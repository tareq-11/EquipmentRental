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
using infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddInfrastructure(builder.Configuration);
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30) };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue("sst");
            if (!Guid.TryParse(userId, out var id) || string.IsNullOrWhiteSpace(stamp)) { context.Fail("Invalid session."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<EquipmentRentalDbContext>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
            if (user is null || user.AccountStatus == Core.Identity.AccountStatus.Disabled || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stamp), Encoding.UTF8.GetBytes(user.SecurityStamp))) context.Fail("Session revoked.");
        },
        OnChallenge = async context => { context.HandleResponse(); await ExceptionHandlingMiddleware.WriteAsync(context.HttpContext, new Error("authentication_required", "Authentication is required.", ErrorType.Unauthorized)); },
        OnForbidden = context => ExceptionHandlingMiddleware.WriteAsync(context.HttpContext, new Error("access_denied", "You do not have permission for this action.", ErrorType.Forbidden))
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("Operations", policy => policy.RequireRole("OperationsEmployee", "Admin")));
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
builder.Services.AddValidatorsFromAssembly(typeof(Services.Foundation.CreateFoundationProbeCommand).Assembly);
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
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("otp", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true }));
});
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EquipmentRentalDbContext>(name: "postgresql");
builder.Services.AddResponseCompression();

if (builder.Environment.IsDevelopment())
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3001"];
    builder.Services.AddCors(options => options.AddPolicy("Development", policy => policy
         .WithOrigins(allowedOrigins)
          .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms (CorrelationId: {CorrelationId})";
    options.EnrichDiagnosticContext = (diagnosticContext, context) =>
        diagnosticContext.Set("CorrelationId", context.Items["CorrelationId"]?.ToString() ?? "unknown");
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseResponseCompression();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
