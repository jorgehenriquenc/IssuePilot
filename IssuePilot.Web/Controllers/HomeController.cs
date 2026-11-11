using IssuePilot.Application;
using IssuePilot.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IssuePilot.Web.Controllers;

public sealed class HomeController(IssueAnalysis analysis) : Controller
{
    public IActionResult Index() => View(new HomeViewModel());

    public IActionResult Privacy() => View();

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> Analyze(HomeViewModel form, CancellationToken ct)
    {
        if (form.Mode is not ("url" or "draft")) ModelState.AddModelError("", "Escolha um modo de análise válido.");
        if (!ModelState.IsValid) return View("Index", form);
        try
        {
            form.Report = form.Mode == "url"
                ? await analysis.AnalyzeUrlAsync(form.Url ?? "", ct)
                : await analysis.AnalyzeDraftAsync(form.Title ?? "", form.Body ?? "", ct);
        }
        catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
        catch (KeyNotFoundException ex) { ModelState.AddModelError("", ex.Message); }
        catch (HttpRequestException) { ModelState.AddModelError("", "Não foi possível consultar o GitHub agora."); }
        catch (TaskCanceledException) { ModelState.AddModelError("", "A análise excedeu o tempo limite."); }
        return View("Index", form);
    }

    public IActionResult Error() => View();
}

public sealed class HomeViewModel
{
    public string Mode { get; set; } = "url";
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public IssueReport? Report { get; set; }
}
