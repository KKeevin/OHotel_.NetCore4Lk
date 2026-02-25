using System.Text.RegularExpressions;

namespace OHotel.NETCoreMVC.Helper;

/// <summary>密碼強度驗證</summary>
public static class PasswordPolicy
{
    /// <summary>最小長度</summary>
    public const int MinLength = 8;

    /// <summary>驗證密碼是否符合政策（至少 8 字元，含英文與數字）</summary>
    public static (bool IsValid, string? ErrorMessage) Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "密碼不可為空");
        if (password.Length < MinLength)
            return (false, $"密碼至少需 {MinLength} 個字元");
        if (!Regex.IsMatch(password, @"[a-zA-Z]"))
            return (false, "密碼需包含至少一個英文字母");
        if (!Regex.IsMatch(password, @"[0-9]"))
            return (false, "密碼需包含至少一個數字");
        return (true, null);
    }
}
