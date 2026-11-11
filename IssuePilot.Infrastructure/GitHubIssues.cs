using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IssuePilot.Application;
using IssuePilot.Domain;

namespace IssuePilot.Infrastructure;

public sealed class GitHubIssues(HttpClient http) : IGitHubIssues
{
    public async Task<IssueInput?> GetAsync(GitHubIssueRef issue, CancellationToken ct)
    {
        using var response = await http.GetAsync($"repos/{issue.Owner}/{issue.Repository}/issues/{issue.Number}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var root = document!.RootElement;
        if (root.TryGetProperty("pull_request", out _)) return null;
        var title = root.GetProperty("title").GetString() ?? "";
        var body = root.TryGetProperty("body", out var element) ? element.GetString() ?? "" : "";
        return new IssueInput(title, body, $"https://github.com/{issue.Owner}/{issue.Repository}/issues/{issue.Number}");
    }

    public async Task<IReadOnlyList<DuplicateCandidate>> SearchAsync(GitHubIssueRef issue, string title, CancellationToken ct)
    {
        var terms = string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(6));
        var query = Uri.EscapeDataString($"repo:{issue.Owner}/{issue.Repository} is:issue {terms}");
        try
        {
            using var response = await http.GetAsync($"search/issues?q={query}&per_page=6", ct);
            if (!response.IsSuccessStatusCode) return [];
            using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var result = new List<DuplicateCandidate>();
            foreach (var item in document!.RootElement.GetProperty("items").EnumerateArray())
            {
                var number = item.GetProperty("number").GetInt32();
                if (number == issue.Number) continue;
                result.Add(new DuplicateCandidate(number, item.GetProperty("title").GetString() ?? "",
                    $"https://github.com/{issue.Owner}/{issue.Repository}/issues/{number}"));
            }
            return result;
        }
        catch (HttpRequestException) { return []; }
    }
}
