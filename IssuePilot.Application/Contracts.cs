using IssuePilot.Domain;

namespace IssuePilot.Application;

public interface IGitHubIssues
{
    Task<IssueInput?> GetAsync(GitHubIssueRef issue, CancellationToken cancellationToken);
    Task<IReadOnlyList<DuplicateCandidate>> SearchAsync(GitHubIssueRef issue, string title, CancellationToken cancellationToken);
}

public interface IModelAnalyzer
{
    Task<IssueReport?> AnalyzeAsync(IssueInput input, CancellationToken cancellationToken);
}

public interface IModelDiagnostics
{
    string? FailureReason { get; }
}

public interface IAnalysisHistory
{
    Task SaveAsync(string mode, int qualityScore, CancellationToken cancellationToken);
}

public sealed record GitHubIssueRef(string Owner, string Repository, int Number)
{
    public static bool TryParse(string? value, out GitHubIssueRef? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 300 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com" ||
            uri.Port != 443 || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts[2] != "issues" ||
            !System.Text.RegularExpressions.Regex.IsMatch(parts[0], "^[A-Za-z0-9-]{1,39}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(parts[1], "^[A-Za-z0-9_.-]{1,100}$") ||
            !int.TryParse(parts[3], out var number) || number < 1) return false;
        result = new GitHubIssueRef(parts[0], parts[1], number);
        return true;
    }
}
