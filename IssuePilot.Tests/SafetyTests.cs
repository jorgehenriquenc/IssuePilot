using IssuePilot.Application;
using IssuePilot.Domain;
using IssuePilot.Infrastructure;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IssuePilot.Tests;

public class SafetyTests
{
    [Theory]
    [InlineData("https://github.com/dotnet/aspnetcore/issues/1", true)]
    [InlineData("http://github.com/dotnet/aspnetcore/issues/1", false)]
    [InlineData("https://github.com.evil.test/o/r/issues/1", false)]
    [InlineData("https://github.com/o/r/issues/1?redirect=evil", false)]
    [InlineData("https://github.com/o/r/pull/1", false)]
    [InlineData("https://github.com/o/r/issues/0", false)]
    public void Url_allowlist_is_strict(string url, bool expected) =>
        Assert.Equal(expected, GitHubIssueRef.TryParse(url, out _));

    [Theory]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz123456", true)]
    [InlineData("-----BEGIN PRIVATE KEY-----", true)]
    [InlineData("AKIAABCDEFGHIJKLMNOP", true)]
    [InlineData("glpat-abcdefghijklmnopqrstuvwxyz", true)]
    [InlineData("Texto comum de uma issue", false)]
    public void Detects_likely_secrets(string text, bool expected) =>
        Assert.Equal(expected, Safety.HasLikelySecret(text));

    [Fact]
    public async Task Secret_is_rejected_before_model_call()
    {
        var model = new FakeModel();
        var analysis = new IssueAnalysis(new FakeGitHub(), model, new FakeHistory());
        await Assert.ThrowsAsync<ArgumentException>(() => analysis.AnalyzeDraftAsync("Bug ao salvar", "token=abcdefghijklmnop", CancellationToken.None));
        Assert.False(model.Called);
    }

    [Fact]
    public void Invalid_model_json_is_rejected()
    {
        Assert.Null(ModelAnalyzer.Parse("{\"summary\":\"x\"}", "gemini"));
    }

    [Fact]
    public async Task Sqlite_history_stores_only_anonymous_metadata()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"history-test-{Guid.NewGuid():N}.sqlite");
        var history = new SqliteAnalysisHistory(path);
        await history.SaveAsync("draft", 70, CancellationToken.None);
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT mode, quality_score FROM analyses";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("draft", reader.GetString(0));
        Assert.Equal(70, reader.GetInt32(1));
    }

    [Fact]
    public void Baseline_does_not_claim_unobserved_priority()
    {
        var report = BaselineAnalyzer.Analyze(new IssueInput("Bug ao salvar", "Erro ao salvar"));
        Assert.Equal("A verificar", report.Priority);
        Assert.InRange(report.QualityScore, 0, 100);
    }

    [Fact]
    public void Baseline_reads_behavior_after_markdown_headings()
    {
        var issue = new IssueInput("Rows disappear", "### Expected behavior\nEarlier rows appear.\n### Actual behavior\nThe viewport stays blank.");
        var report = BaselineAnalyzer.Analyze(issue);
        Assert.Equal("Earlier rows appear.", report.Expected);
        Assert.Equal("The viewport stays blank.", report.Observed);
    }

    [Fact]
    public void Baseline_uses_title_when_draft_has_only_steps()
    {
        var report = BaselineAnalyzer.Analyze(new IssueInput("Falha ao salvar perfil", "### Passos para reproduzir\n1. Abra o perfil.\n2. Clique em salvar."));
        Assert.Equal("Falha ao salvar perfil", report.Summary);
        Assert.Contains("## Resumo\nFalha ao salvar perfil", report.ImprovedMarkdown);
    }

    [Fact]
    public void Baseline_uses_bug_section_and_only_reproduction_steps()
    {
        var issue = new IssueInput("Rows disappear", "### Is there an existing issue for this?\n- [x] I searched.\n### Describe the bug\nThe viewport becomes blank.\n### Reproduction\n1. Open the page.\n2. Scroll upward.\n### Actual behavior\nBlank viewport.\n### Environment\n- OS: Windows");
        var report = BaselineAnalyzer.Analyze(issue);
        Assert.Equal("The viewport becomes blank.", report.Summary);
        Assert.Equal(["Open the page.", "Scroll upward."], report.ReproductionSteps);
        Assert.Contains("OS: Windows", report.ImprovedMarkdown);
        Assert.DoesNotContain("I searched", report.ImprovedMarkdown);
    }

    [Fact]
    public async Task Incomplete_model_markdown_is_rebuilt_from_issue()
    {
        var input = new IssueInput("Rows disappear", "### Describe the bug\nThe list becomes blank.\n### Expected behavior\nRows appear.\n### Actual behavior\nBlank list.");
        var modeled = BaselineAnalyzer.Analyze(input) with
        {
            AnalysisMode = "gemini-3.5-flash-lite",
            Summary = "The list loses rows while scrolling.",
            ImprovedMarkdown = "Não informado",
            ReviewWarnings = []
        };
        var analysis = new IssueAnalysis(new FakeGitHub(), new FakeModelWithReport(modeled), new FakeHistory());

        var result = await analysis.AnalyzeDraftAsync(input.Title, input.Body, CancellationToken.None);

        Assert.Equal("gemini-3.5-flash-lite", result.AnalysisMode);
        Assert.Equal("The list loses rows while scrolling.", result.Summary);
        Assert.StartsWith("# Rows disappear", result.ImprovedMarkdown);
        Assert.Contains("## Passos para reproduzir", result.ImprovedMarkdown);
        Assert.Contains(result.ReviewWarnings, warning => warning.Contains("organizada localmente", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Duplicate_agent_filters_unrelated_search_hits()
    {
        var github = new FakeGitHub([new DuplicateCandidate(8, "Components AI built-in UIs", "https://github.com/o/r/issues/8")]);
        var candidates = await DuplicateAgent.SearchAsync(github, new GitHubIssueRef("o", "r", 1), "Virtualize stops loading earlier variable-height items", CancellationToken.None);
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Gemini_http_failure_has_safe_diagnostic()
    {
        var analyzer = new ModelAnalyzer(new HttpClient(new StatusHandler()), new FakeSettings());
        var result = await analyzer.AnalyzeAsync(new IssueInput("Bug", "Steps to reproduce"), CancellationToken.None);
        Assert.Null(result);
        Assert.Equal("modelo respondeu HTTP 429", analyzer.FailureReason);
    }

    [Fact]
    public async Task Gemini_flash_lite_sends_structured_json_and_accepts_valid_response()
    {
        var handler = new SuccessfulGeminiHandler();
        var analyzer = new ModelAnalyzer(new HttpClient(handler), new FakeSettings());
        var report = await analyzer.AnalyzeAsync(new IssueInput("Falha ao salvar", "Ao salvar, aparece um erro."), CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal("gemini-3.5-flash-lite", report.AnalysisMode);
        Assert.Equal(80, report.QualityScore);
        Assert.Equal("gemini-3.5-flash-lite", handler.Model);
        Assert.Equal("minimal", handler.ThinkingLevel);
        Assert.True(handler.HasResponseSchema);
        Assert.Null(analyzer.FailureReason);
    }

    [Fact]
    public async Task Gemini_timeout_has_safe_diagnostic()
    {
        var analyzer = new ModelAnalyzer(new HttpClient(new HangingHandler()) { Timeout = TimeSpan.FromMilliseconds(30) }, new FakeSettings());
        var report = await analyzer.AnalyzeAsync(new IssueInput("Falha", "Passos"), CancellationToken.None);
        Assert.Null(report);
        Assert.Equal("tempo limite do modelo (60 s)", analyzer.FailureReason);
    }

    private sealed class FakeModel : IModelAnalyzer
    {
        public bool Called { get; private set; }
        public Task<IssueReport?> AnalyzeAsync(IssueInput input, CancellationToken ct)
        {
            Called = true;
            return Task.FromResult<IssueReport?>(null);
        }
    }

    private sealed class FakeModelWithReport(IssueReport report) : IModelAnalyzer
    {
        public Task<IssueReport?> AnalyzeAsync(IssueInput input, CancellationToken ct) => Task.FromResult<IssueReport?>(report);
    }

    private sealed class FakeGitHub(IReadOnlyList<DuplicateCandidate>? candidates = null) : IGitHubIssues
    {
        public Task<IssueInput?> GetAsync(GitHubIssueRef issue, CancellationToken ct) => Task.FromResult<IssueInput?>(null);
        public Task<IReadOnlyList<DuplicateCandidate>> SearchAsync(GitHubIssueRef issue, string title, CancellationToken ct) => Task.FromResult<IReadOnlyList<DuplicateCandidate>>(candidates ?? []);
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        }
    }

    private sealed class SuccessfulGeminiHandler : HttpMessageHandler
    {
        public string? Model { get; private set; }
        public string? ThinkingLevel { get; private set; }
        public bool HasResponseSchema { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Model = request.RequestUri?.Segments.Last().Split(':')[0];
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            ThinkingLevel = body.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString();
            HasResponseSchema = body.RootElement.GetProperty("generationConfig").TryGetProperty("responseSchema", out _);
            var report = new
            {
                summary = "Falha ao salvar", category = "Bug", priority = "Média", impact = "Usuário não salva alterações",
                confidence = 0.8, qualityScore = 80, present = new[] { "Descrição" }, missing = Array.Empty<string>(),
                reproductionSteps = new[] { "Abrir perfil", "Salvar" }, expected = "Alterações salvas", observed = "Erro",
                hypotheses = new[] { "Hipótese: falha de rede" }, labels = new[] { "bug" },
                nextActions = new[] { "Conferir logs" }, improvedMarkdown = "# Falha ao salvar", reviewWarnings = Array.Empty<string>()
            };
            var response = JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = JsonSerializer.Serialize(report) } } } } } });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FakeSettings : IConfigurationLike
    {
        public string Provider => "gemini";
        public string? GeminiKey => "test-key";
        public string GeminiModel => "gemini-3.5-flash-lite";
        public string OllamaModel => "unused";
    }

    private sealed class FakeHistory : IAnalysisHistory
    {
        public Task SaveAsync(string mode, int qualityScore, CancellationToken ct) => Task.CompletedTask;
    }
}
