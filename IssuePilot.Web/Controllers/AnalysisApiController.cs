using IssuePilot.Application;
using IssuePilot.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IssuePilot.Web.Controllers;

[ApiController]
[Route("api/analysis")]
[RequestSizeLimit(65536)]
public sealed class AnalysisApiController(IssueAnalysis analysis) : ControllerBase
{
    [HttpPost("draft")]
    [ProducesResponseType(typeof(IssueReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IssueReport>> Draft([FromBody] DraftRequest request, CancellationToken ct)
    {
        try { return Ok(await analysis.AnalyzeDraftAsync(request.Title, request.Body, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("url")]
    [ProducesResponseType(typeof(IssueReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IssueReport>> AnalyzeUrl([FromBody] UrlRequest request, CancellationToken ct)
    {
        try { return Ok(await analysis.AnalyzeUrlAsync(request.Url, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Não foi possível consultar o GitHub agora." }); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "A consulta excedeu o tempo limite." }); }
    }
}

public sealed record DraftRequest(string Title, string Body);
public sealed record UrlRequest(string Url);
