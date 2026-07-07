using System.Security.Claims;
using Cuintable.Server.DTOs.FinancialAdvisor;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/financial-advisor")]
[Authorize(Roles = "Owner,Pareja")]
public class FinancialAdvisorController : ControllerBase
{
    private const long MaxPdfBytes = 10 * 1024 * 1024;

    private readonly IFinancialAdvisorService _service;
    private readonly IStatementAnalysisService _analysis;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<ReviewTransactionRequest> _reviewValidator;

    public FinancialAdvisorController(
        IFinancialAdvisorService service,
        IStatementAnalysisService analysis,
        IFileStorageService fileStorage,
        IValidator<ReviewTransactionRequest> reviewValidator)
    {
        _service = service;
        _analysis = analysis;
        _fileStorage = fileStorage;
        _reviewValidator = reviewValidator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirstValue("TenantId")!);

    [HttpPost("statements")]
    [RequestSizeLimit(MaxPdfBytes + 1024 * 1024)]
    public async Task<ActionResult<StatementDetailResponse>> Upload([FromForm] IFormFile pdf, [FromForm] Guid? creditCardId)
    {
        if (pdf is null || pdf.Length == 0)
            return BadRequest("A PDF file is required.");
        if (pdf.Length > MaxPdfBytes)
            return BadRequest("The PDF must be 10 MB or smaller.");
        if (!string.Equals(pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !pdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are accepted.");

        var tenantId = GetTenantId();
        var statementId = Guid.NewGuid();

        string pdfUrl;
        using (var stream = pdf.OpenReadStream())
        {
            pdfUrl = await _fileStorage.UploadAsync(stream, pdf.FileName, "application/pdf", $"statements/{statementId}");
        }

        await _service.CreateStatementAsync(tenantId, GetUserId(), statementId, creditCardId, pdfUrl);

        // Synchronous by design: the pipeline is NOT tied to the request's cancellation,
        // so a client/proxy timeout doesn't abort processing — the result shows up on refresh.
        await _analysis.ProcessAsync(tenantId, statementId);

        var detail = await _service.GetByIdAsync(tenantId, statementId);
        return CreatedAtAction(nameof(GetById), new { id = statementId }, detail);
    }

    [HttpGet("statements")]
    public async Task<ActionResult<List<StatementSummaryResponse>>> GetAll([FromQuery] int? year, [FromQuery] Guid? creditCardId)
    {
        return Ok(await _service.GetAllAsync(GetTenantId(), year, creditCardId));
    }

    [HttpGet("statements/{id:guid}")]
    public async Task<ActionResult<StatementDetailResponse>> GetById(Guid id)
    {
        var statement = await _service.GetByIdAsync(GetTenantId(), id);
        if (statement is null) return NotFound();
        return Ok(statement);
    }

    [HttpPost("statements/{id:guid}/reprocess")]
    public async Task<ActionResult<StatementDetailResponse>> Reprocess(Guid id)
    {
        var tenantId = GetTenantId();
        var statement = await _service.GetEntityAsync(tenantId, id);
        if (statement is null) return NotFound();
        if (statement.Status == StatementStatus.Processing)
            return Conflict("The statement is already being processed.");

        await _analysis.ProcessAsync(tenantId, id);
        return Ok(await _service.GetByIdAsync(tenantId, id));
    }

    [HttpDelete("statements/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(GetTenantId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("statements/{id:guid}/file")]
    public async Task<IActionResult> GetFile(Guid id, [FromQuery] bool download = false)
    {
        var statement = await _service.GetEntityAsync(GetTenantId(), id);
        if (statement is null) return NotFound();
        if (string.IsNullOrEmpty(statement.PdfUrl)) return NotFound("No file stored for this statement.");

        var result = await _fileStorage.GetStreamAsync(statement.PdfUrl);
        if (result is null) return NotFound("File not found in storage.");

        var (stream, contentType) = result.Value;
        var fileName = $"estado-de-cuenta_{statement.BankName ?? "banco"}_{statement.PeriodYear}-{statement.PeriodMonth:00}.pdf";
        var disposition = download ? "attachment" : "inline";

        Response.Headers.ContentDisposition = $"{disposition}; filename=\"{fileName}\"";
        return File(stream, contentType);
    }

    [HttpPut("transactions/{id:guid}/review")]
    public async Task<ActionResult<StatementTransactionResponse>> Review(Guid id, [FromBody] ReviewTransactionRequest request)
    {
        var validation = await _reviewValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var status = Enum.Parse<TransactionReviewStatus>(request.Status);
        var transaction = await _service.ReviewTransactionAsync(GetTenantId(), id, status);
        if (transaction is null) return NotFound();
        return Ok(transaction);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdvisorDashboardResponse>> GetDashboard([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest("Invalid period.");

        return Ok(await _service.GetDashboardAsync(GetTenantId(), year, month));
    }
}
