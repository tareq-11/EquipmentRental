using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace infrastructure.Identity;

/// <summary>Prevents production startup with plaintext or development email delivery.</summary>
public sealed class MailOptionsValidator(IHostEnvironment environment) : IValidateOptions<MailOptions>
{
    public ValidateOptionsResult Validate(string? name, MailOptions options)
    {
        var production = environment.IsProduction();
        if (options.Mode.Equals("DevelopmentMailbox", StringComparison.OrdinalIgnoreCase))
        {
            if (production) return ValidateOptionsResult.Fail("Production requires Mail:Mode=Smtp.");
            return string.IsNullOrWhiteSpace(options.DevelopmentMailboxPath)
                ? ValidateOptionsResult.Fail("Development mailbox mode requires Mail:DevelopmentMailboxPath.")
                : ValidateOptionsResult.Success;
        }

        if (!options.Mode.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail("Mail:Mode must be DevelopmentMailbox or Smtp.");
        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.FromAddress))
            return ValidateOptionsResult.Fail("SMTP mode requires Mail:Host and Mail:FromAddress.");
        if (!options.TlsMode.Equals("StartTls", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail("SMTP mode requires Mail:TlsMode=StartTls.");
        if (production && (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Password)))
            return ValidateOptionsResult.Fail("Production SMTP requires Mail:UserName and Mail:Password.");
        return ValidateOptionsResult.Success;
    }
}
