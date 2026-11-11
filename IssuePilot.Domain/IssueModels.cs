namespace IssuePilot.Domain;

public sealed record IssueInput(string Title, string Body, string? SourceUrl = null);
public sealed record DuplicateCandidate(int Number, string Title, string Url);
public sealed record IssueReport(
    string Summary, string Category, string Priority, string Impact, double Confidence,
    int QualityScore, IReadOnlyList<string> Present, IReadOnlyList<string> Missing,
    IReadOnlyList<string> ReproductionSteps, string Expected, string Observed,
    IReadOnlyList<string> Hypotheses, IReadOnlyList<string> Labels,
    IReadOnlyList<DuplicateCandidate> Duplicates, IReadOnlyList<string> NextActions,
    string ImprovedMarkdown, IReadOnlyList<string> ReviewWarnings, string AnalysisMode);
