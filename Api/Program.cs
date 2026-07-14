using infrastructure;
using infrastructure.Persistence;
using Serilog;
using MediatR;
using Services.Behaviors;
using Api.Middleware;
using System.Threading.RateLimiting;
using Core.Common;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => string.IsNullOrEmpty(entry.Key) ? "request" : entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Provide a value in the required format."
                        : error.ErrorMessage)
                    .ToArray());

        const int status = StatusCodes.Status400BadRequest;
        return new BadRequestObjectResult(new Shared.ApiResponse<Shared.ApiErrorData>(
            status,
            "Correct the highlighted fields.",
            new Shared.ApiErrorData(errors)));
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Services.Abstractions.IActorContext, HttpActorContext>();
builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(Services.Foundation.CreateFoundationProbeCommand).Assembly));
builder.Services.AddScoped<IValidator<Services.Foundation.CreateFoundationProbeCommand>, Services.Foundation.CreateFoundationProbeCommandValidator>();
// Pipeline registration order is the required execution order: exception, performance, validation, idempotency, cache, handler.
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) => await ExceptionHandlingMiddleware.WriteAsync(context.HttpContext, new Error("rate_limited", "Too many requests. Wait and try again.", ErrorType.RateLimited));
    options.AddFixedWindowLimiter("foundation", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EquipmentRentalDbContext>(name: "postgresql");
builder.Services.AddResponseCompression();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy("Development", policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:3001")
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
