using System.Text;

namespace Core.Identity;

/// <summary>Shared password requirements for all account-creation and credential-reset paths.</summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumUtf8Bytes = 72;

    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
            return "Use at least 12 characters.";
        if (Encoding.UTF8.GetByteCount(password) > MaximumUtf8Bytes)
            return "Use a password no longer than 72 UTF-8 bytes.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
            return "Use uppercase, lowercase, number, and symbol characters.";
        return null;
    }
}
