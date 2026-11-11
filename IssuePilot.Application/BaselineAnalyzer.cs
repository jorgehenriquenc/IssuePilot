using System.Text.RegularExpressions;
using IssuePilot.Domain;

namespace IssuePilot.Application;

public static class BaselineAnalyzer
{
    public static IssueReport Analyze(IssueInput input)
    {
        var body = Safety.PlainText(input.Body);
        var lines = body.Replace("\r", "").Split('\n', StringSplitOptions.TrimEntries);
        var summary = FindSummary(lines, input.Title);
        var steps = FindSteps(lines);
        var expected = ExtractBehavior(lines, "esperado", "expected");
        var observed = ExtractBehavior(lines, "observado", "actual");
        var environment = FindSection(lines, "environment", "ambiente").Select(CleanBullet).Take(8).ToArray();
        var hasEnvironment = environment.Length > 0 || Contains(body, "versão", "version", "browser", "os:");
        var hasEvidence = Contains(body, "screenshot", "captura", "stack trace", "logs:", "log:") ||
            Regex.IsMatch(body, @"https://github\.com/\S*repro", RegexOptions.IgnoreCase);
        var fields = new (string Name, bool Found)[] {
            ("Passos de reprodução", steps.Length > 0),
            ("Comportamento esperado", expected != "Não informado"),
            ("Comportamento observado", observed != "Não informado"),
            ("Ambiente ou versão", hasEnvironment),
            ("Evidência ou logs", hasEvidence)
        };
        var present = fields.Where(field => field.Found).Select(field => field.Name).ToArray();
        var missing = fields.Where(field => !field.Found).Select(field => field.Name).ToArray();
        var category = Contains(input.Title + " " + body, "crash", "erro", "error", "bug", "falha") ? "Bug" : "A verificar";
        var score = Math.Clamp(20 + present.Length * 15 + (input.Title.Length >= 12 ? 5 : 0), 0, 100);
        var stepMarkdown = steps.Length == 0 ? "1. [Adicione passos verificáveis]" : string.Join("\n", steps.Select((step, index) => $"{index + 1}. {step}"));
        var environmentMarkdown = environment.Length == 0 ? "[Informe versão, sistema e contexto]" : string.Join("\n", environment.Select(item => "- " + item));
        var improved = $"# {input.Title.Trim()}\n\n## Resumo\n{summary}\n\n## Passos para reproduzir\n{stepMarkdown}\n\n## Esperado\n{expected}\n\n## Observado\n{observed}\n\n## Ambiente\n{environmentMarkdown}\n";
        var actions = missing.Select(field => $"Informar: {field}.").ToList();
        if (actions.Count == 0) actions.Add("Confirmar os passos e o impacto com um mantenedor.");
        return new IssueReport(summary, category, "A verificar", "A verificar", .35,
            score, present, missing, steps, expected, observed, [],
            category == "Bug" ? ["bug", "needs-triage"] : ["needs-triage"], [],
            actions, improved,
            ["Análise local baseada em regras; classificação e prioridade exigem revisão humana."], "Regras locais");
    }

    private static string FindSummary(string[] lines, string title)
    {
        var section = FindSection(lines, "describe the bug", "descrição do problema", "description");
        var candidate = section.FirstOrDefault(line => IsContent(line));
        candidate ??= title;
        var cleaned = CleanInline(candidate);
        return cleaned.Length <= 500 ? cleaned : cleaned[..500].TrimEnd() + "…";
    }

    private static string[] FindSteps(string[] lines)
    {
        var section = FindSection(lines, "reproduction", "to reproduce", "steps to reproduce", "passos para reproduzir", "passos de reprodução");
        var source = section.Length > 0 ? section : lines;
        var steps = source.Select(line => Regex.Match(line, @"^\s*\d+[.)]\s+(.+)$"))
            .Where(match => match.Success)
            .Select(match => CleanInline(match.Groups[1].Value))
            .Take(10).ToArray();
        if (steps.Length > 0) return steps;
        return Regex.Matches(string.Join(' ', lines), @"(?:^|\s)\d+[.)]\s+(.+?)(?=\s+\d+[.)]\s+|$)")
            .Select(match => CleanInline(match.Groups[1].Value)).Take(8).ToArray();
    }

    private static string ExtractBehavior(string[] lines, params string[] markers)
    {
        var section = FindSection(lines, markers);
        var value = section.FirstOrDefault(IsContent);
        if (value is not null) return CleanInline(value);
        foreach (var line in lines)
        {
            if (!markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))) continue;
            var colon = line.IndexOf(':');
            if (colon >= 0 && colon + 1 < line.Length) return CleanInline(line[(colon + 1)..]);
        }
        return "Não informado";
    }

    private static string[] FindSection(string[] lines, params string[] markers)
    {
        var start = Array.FindIndex(lines, line => IsHeading(line) && markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase)));
        if (start < 0) return [];
        var end = Array.FindIndex(lines, start + 1, IsHeading);
        if (end < 0) end = lines.Length;
        return lines[(start + 1)..end].Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    private static bool IsHeading(string line) => Regex.IsMatch(line, @"^#{1,6}\s+");
    private static bool IsContent(string line) => !string.IsNullOrWhiteSpace(line) && !IsHeading(line) && !Regex.IsMatch(line, @"^\s*-\s*\[[ xX]\]");
    private static string CleanBullet(string line) => CleanInline(Regex.Replace(line, @"^\s*[-*]\s+", ""));
    private static string CleanInline(string line) => line.Trim().Replace("**", "").Replace("`", "");
    private static bool Contains(string text, params string[] terms) => terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
}
