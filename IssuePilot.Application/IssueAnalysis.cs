using IssuePilot.Domain;

namespace IssuePilot.Application;

public sealed class IssueAnalysis(IGitHubIssues gitHub, IModelAnalyzer model, IAnalysisHistory history)
{
    public async Task<IssueReport> AnalyzeDraftAsync(string title, string body, CancellationToken ct)
    {
        Validate(title, body);
        return await AnalyzeAsync(new IssueInput(title.Trim(), body.Trim()), null, ct);
    }

    public async Task<IssueReport> AnalyzeUrlAsync(string url, CancellationToken ct)
    {
        if (!GitHubIssueRef.TryParse(url, out var issue)) throw new ArgumentException("Use uma URL HTTPS de issue pública do GitHub.");
        var input = await gitHub.GetAsync(issue!, ct) ?? throw new KeyNotFoundException("Issue pública não encontrada.");
        Validate(input.Title, input.Body);
        return await AnalyzeAsync(input, issue, ct);
    }

    private async Task<IssueReport> AnalyzeAsync(IssueInput input, GitHubIssueRef? issue, CancellationToken ct)
    {
        // Secret detection precedes every model call, including local providers.
        if (Safety.HasLikelySecret(input.Title + "\n" + input.Body))
            throw new ArgumentException("O texto parece conter um segredo. Remova-o antes de analisar.");
        var report = await TriageAgent.AnalyzeAsync(model, input, ct);
        report = QualityCoachAgent.Coach(report);
        report = DraftCoachAgent.EnsureUsefulMarkdown(report, input);
        var duplicates = issue is null ? [] : await DuplicateAgent.SearchAsync(gitHub, issue, input.Title, ct);
        report = report with { Duplicates = duplicates };
        report = ReviewAgent.Review(report, input);
        await history.SaveAsync(issue is null ? "draft" : "url", report.QualityScore, ct);
        return report;
    }

    private static void Validate(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > Safety.MaxTitle ||
            string.IsNullOrWhiteSpace(body) || body.Length > Safety.MaxBody)
            throw new ArgumentException("Informe título (até 200 caracteres) e descrição (até 12.000 caracteres).");
    }
}

public static class DraftCoachAgent
{
    public static IssueReport EnsureUsefulMarkdown(IssueReport report, IssueInput input)
    {
        var markdown = report.ImprovedMarkdown.Trim();
        if (markdown.StartsWith("# ", StringComparison.Ordinal) && markdown.Contains("\n## ", StringComparison.Ordinal))
            return report;

        var baseline = BaselineAnalyzer.Analyze(input);
        var warnings = report.ReviewWarnings.Append("O modelo não produziu um rascunho completo; a versão em Markdown foi organizada localmente a partir da issue.").ToArray();
        return report with { ImprovedMarkdown = baseline.ImprovedMarkdown, ReviewWarnings = warnings };
    }
}

public static class TriageAgent
{
    public static async Task<IssueReport> AnalyzeAsync(IModelAnalyzer model, IssueInput input, CancellationToken ct)
    {
        var modeled = await model.AnalyzeAsync(input, ct);
        if (modeled is not null) return modeled;
        var baseline = BaselineAnalyzer.Analyze(input);
        if (model is IModelDiagnostics { FailureReason: { } reason })
            baseline = baseline with { AnalysisMode = $"Regras locais — {reason}" };
        return baseline;
    }
}

public static class QualityCoachAgent
{
    public static IssueReport Coach(IssueReport report)
    {
        var missing = report.Missing.ToList();
        if (report.Expected.Equals("Não informado", StringComparison.OrdinalIgnoreCase) && !missing.Contains("Comportamento esperado"))
            missing.Add("Comportamento esperado");
        if (report.Observed.Equals("Não informado", StringComparison.OrdinalIgnoreCase) && !missing.Contains("Comportamento observado"))
            missing.Add("Comportamento observado");
        var actions = report.NextActions.ToList();
        foreach (var field in missing)
        {
            var action = $"Informar: {field}.";
            if (!actions.Contains(action)) actions.Add(action);
        }
        return report with { Missing = missing, NextActions = actions };
    }
}

public static class DuplicateAgent
{
    public static async Task<IReadOnlyList<DuplicateCandidate>> SearchAsync(IGitHubIssues gitHub, GitHubIssueRef issue, string title, CancellationToken ct)
    {
        var candidates = await gitHub.SearchAsync(issue, title, ct);
        var sourceTerms = Terms(title);
        return candidates.Where(candidate =>
        {
            var candidateTerms = Terms(candidate.Title);
            var overlap = sourceTerms.Intersect(candidateTerms).Count();
            return overlap >= 2 && overlap >= Math.Ceiling(Math.Min(sourceTerms.Count, candidateTerms.Count) * .35);
        }).Take(5).ToArray();
    }

    private static HashSet<string> Terms(string title) =>
        System.Text.RegularExpressions.Regex.Matches(title.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}")
            .Select(match => match.Value)
            .Where(term => term is not ("the" or "and" or "for" or "with" or "from" or "after" or "when" or "this" or "that" or "uma" or "para" or "com" or "por" or "dos" or "das"))
            .ToHashSet(StringComparer.Ordinal);
}

public static class ReviewAgent
{
    public static IssueReport Review(IssueReport report, IssueInput input)
    {
        var warnings = report.ReviewWarnings.ToList();
        if (report.Confidence < .6) warnings.Add("Confiança baixa: confirme os detalhes antes de agir.");
        if (string.IsNullOrWhiteSpace(report.Expected) || string.IsNullOrWhiteSpace(report.Observed))
            warnings.Add("Comportamento esperado ou observado insuficiente.");
        if (report.Expected != "Não informado" && report.Expected.Equals(report.Observed, StringComparison.OrdinalIgnoreCase))
            warnings.Add("Esperado e observado são idênticos; confirme se o problema foi descrito corretamente.");
        if (report.Hypotheses.Any(h => !h.StartsWith("Hipótese:", StringComparison.OrdinalIgnoreCase)))
            report = report with { Hypotheses = report.Hypotheses.Select(h => "Hipótese: " + h).ToArray() };
        return report with { QualityScore = Math.Clamp(report.QualityScore, 0, 100), Confidence = Math.Clamp(report.Confidence, 0, 1), ReviewWarnings = warnings };
    }
}
