using System.Text.RegularExpressions;

namespace IssuePilot.Application;

public static partial class Safety
{
    public const int MaxTitle = 200;
    public const int MaxBody = 12000;

    public static bool HasLikelySecret(string text) =>
        SecretPattern().IsMatch(text) || PrivateKeyPattern().IsMatch(text);

    public static string PlainText(string text) => Regex.Replace(text, @"</?(?:script|style|iframe|div|span|p|br|img|a|svg|object|embed)\b[^>]*>", " ", RegexOptions.IgnoreCase).Trim();

    [GeneratedRegex(@"(?i)(gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|AIza[A-Za-z0-9_-]{30,}|(?:AKIA|ASIA)[A-Z0-9]{16}|xox[baprs]-[A-Za-z0-9-]{20,}|glpat-[A-Za-z0-9_-]{20,}|sk-[A-Za-z0-9_-]{20,}|(?:api[_-]?key|token|password|secret)\s*[:=]\s*['""']?\S{12,})", RegexOptions.Compiled)]
    private static partial Regex SecretPattern();

    [GeneratedRegex(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled)]
    private static partial Regex PrivateKeyPattern();
}
