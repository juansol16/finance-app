using System.Security.Claims;
using Cuintable.Server.DTOs.TaxableExpenses;
using Cuintable.Server.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/taxable-expenses")]
[Authorize(Roles = "Owner,Contador,Pareja")]
public class TaxableExpensesController : ControllerBase
{
    private readonly ITaxableExpenseService _service;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateTaxableExpenseRequest> _createValidator;
    private readonly IValidator<UpdateTaxableExpenseRequest> _updateValidator;

    public TaxableExpensesController(
        ITaxableExpenseService service,
        IFileStorageService fileStorage,
        IValidator<CreateTaxableExpenseRequest> createValidator,
        IValidator<UpdateTaxableExpenseRequest> updateValidator)
    {
        _service = service;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirstValue("TenantId")!);

    [HttpGet]
    public async Task<ActionResult<List<TaxableExpenseResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync(GetTenantId()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxableExpenseResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(GetTenantId(), id);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaxableExpenseResponse>> Create([FromBody] CreateTaxableExpenseRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var item = await _service.CreateAsync(GetTenantId(), GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxableExpenseResponse>> Update(Guid id, [FromBody] UpdateTaxableExpenseRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var item = await _service.UpdateAsync(GetTenantId(), id, request);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(GetTenantId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/upload")]
    public async Task<ActionResult<TaxableExpenseResponse>> UploadFiles(Guid id, [FromForm] IFormFile? pdf, [FromForm] IFormFile? xml)
    {
        var tenantId = GetTenantId();
        var existing = await _service.GetByIdAsync(tenantId, id);
        if (existing is null) return NotFound();

        string? pdfUrl = null;
        string? xmlUrl = null;
        string? xmlMetadata = null;

        if (pdf is not null)
        {
            using var stream = pdf.OpenReadStream();
            pdfUrl = await _fileStorage.UploadAsync(stream, pdf.FileName, pdf.ContentType, $"taxable-expenses/{id}");
        }

        if (xml is not null)
        {
            using var stream = xml.OpenReadStream();
            xmlUrl = await _fileStorage.UploadAsync(stream, xml.FileName, xml.ContentType, $"taxable-expenses/{id}");

            stream.Position = 0;
            xmlMetadata = CfdiParser.Parse(stream);
        }

        var updated = await _service.UpdateFileUrlsAsync(tenantId, id, pdfUrl, xmlUrl, xmlMetadata);
        return Ok(updated);
    }
}
