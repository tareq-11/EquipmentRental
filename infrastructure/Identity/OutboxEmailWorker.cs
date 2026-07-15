using System.Text;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace infrastructure.Identity;

/// <summary>Delivers protected account email messages from the durable outbox.</summary>
public sealed class OutboxEmailWorker(IServiceScopeFactory scopeFactory, IOptions<MailOptions> mailOptions, IDataProtectionProvider protectionProvider, TimeProvider clock, ILogger<OutboxEmailWorker> logger) : BackgroundService
{
    private readonly MailOptions _mail = mailOptions.Value;
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("account-email-v1");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Account outbox delivery batch failed"); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        for (var count = 0; count < 10; count++)
        {
            var message = await ClaimNextAsync(ct);
            if (message is null) return;

            try
            {
                var parts = _protector.Unprotect(message.Payload).Split('\n', 3);
                if (parts.Length != 3) throw new InvalidOperationException("Invalid protected account mail payload.");
                await SendAsync(parts[0], parts[1], parts[2], ct);
                await CompleteAsync(message.Id, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await DeferAsync(message.Id, ct);
                logger.LogWarning(exception, "Account email delivery deferred for outbox message {OutboxMessageId}", message.Id);
            }
        }
    }

    private async Task<OutboxMessage?> ClaimNextAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EquipmentRentalDbContext>();
        var now = clock.GetUtcNow();
        var staleLease = now.AddMinutes(-2);
        var message = await db.OutboxMessages
            .Where(x => x.Type == "account.email" && x.ProcessedAt == null && x.AttemptCount < 10 &&
                (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                (x.ProcessingStartedAt == null || x.ProcessingStartedAt < staleLease))
            .OrderBy(x => x.OccurredAt)
            .FirstOrDefaultAsync(ct);
        if (message is null) return null;

        // The persisted lease prevents two worker instances from sending the same pending message.
        message.ProcessingStartedAt = now;
        message.AttemptCount++;
        try
        {
            await db.SaveChangesAsync(ct);
            return message;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
    }

    private async Task CompleteAsync(Guid messageId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EquipmentRentalDbContext>();
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == messageId, ct);
        if (message.ProcessedAt is not null) return;
        var now = clock.GetUtcNow();
        message.ProcessedAt = now;
        message.ProcessingStartedAt = null;
        var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == message.EventId, ct);
        if (notification is not null) { notification.Status = Core.Identity.NotificationStatus.Delivered; notification.DeliveredAt = now; }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Account email delivered for outbox message {OutboxMessageId}", messageId);
    }

    private async Task DeferAsync(Guid messageId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EquipmentRentalDbContext>();
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == messageId, ct);
        message.ProcessingStartedAt = null;
        message.NextAttemptAt = clock.GetUtcNow().AddSeconds(Math.Min(300, Math.Pow(2, message.AttemptCount) * 5));
        if (message.AttemptCount >= 10)
        {
            var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == message.EventId, ct);
            if (notification is not null) notification.Status = Core.Identity.NotificationStatus.Failed;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        if (_mail.Mode.Equals("DevelopmentMailbox", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_mail.DevelopmentMailboxPath)) throw new InvalidOperationException("Set Mail:DevelopmentMailboxPath or configure Mail:Host for account email delivery.");
            Directory.CreateDirectory(_mail.DevelopmentMailboxPath);
            var file = Path.Combine(_mail.DevelopmentMailboxPath, $"{Guid.NewGuid():N}.eml");
            await File.WriteAllTextAsync(file, $"To: {recipient}\nSubject: {subject}\n\n{body}", Encoding.UTF8, ct);
            logger.LogInformation("Development account email written to the configured mailbox.");
            return;
        }
        var email = new MimeMessage(); email.From.Add(new MailboxAddress(_mail.FromName, _mail.FromAddress)); email.To.Add(MailboxAddress.Parse(recipient)); email.Subject = subject; email.Body = new TextPart("plain") { Text = body };
        using var client = new SmtpClient(); await client.ConnectAsync(_mail.Host!, _mail.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrWhiteSpace(_mail.UserName) && !string.IsNullOrWhiteSpace(_mail.Password)) await client.AuthenticateAsync(_mail.UserName, _mail.Password, ct);
        await client.SendAsync(email, ct); await client.DisconnectAsync(true, ct);
    }
}
