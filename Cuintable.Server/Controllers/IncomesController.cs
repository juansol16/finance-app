using System.Security.Claims;
using Cuintable.Server.DTOs.Incomes;
using Cuintable.Server.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class IncomesController : ControllerBase
{
    private readonly IIncomeService _incomeService;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateIncomeRequest> _createValidator;
    private readonly IValidator<UpdateIncomeRequest> _updateValidator;

    public IncomesController(
        IIncomeService incomeService,
        IFileStorageService fileStorage,
        IValidator<CreateIncomeRequest> createValidator,
        IValidator<UpdateIncomeRequest> updateValidator)
    {
        _incomeService = incomeService;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirstValue("TenantId")!);

    [HttpGet]
    public async Task<ActionResult<List<IncomeResponse>>> GetAll()
    {
        var incomes = await _incomeService.GetAllAsync(GetTenantId());
        return Ok(incomes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncomeResponse>> GetById(Guid id)
    {
        var income = await _incomeService.GetByIdAsync(GetTenantId(), id);
        if (income is null) return NotFound();
        return Ok(income);
    }

    [HttpPost]
    public async Task<ActionResult<IncomeResponse>> Create([FromBody] CreateIncomeRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var income = await _incomeService.CreateAsync(GetTenantId(), GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = income.Id }, income);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IncomeResponse>> Update(Guid id, [FromBody] UpdateIncomeRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var income = await _incomeService.UpdateAsync(GetTenantId(), id, request);
        if (income is null) return NotFound();
        return Ok(income);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _incomeService.DeleteAsync(GetTenantId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/upload")]
    public async Task<ActionResult<IncomeResponse>> UploadFiles(Guid id, [FromForm] IFormFile? pdf, [FromForm] IFormFile? xml)
    {
        var tenantId = GetTenantId();
        var existing = await _incomeService.GetByIdAsync(tenantId, id);
        if (existing is null) return NotFound();

        string? pdfUrl = null;
        string? xmlUrl = null;
        string? xmlMetadata = null;

        if (pdf is not null)
        {
            using var stream = pdf.OpenReadStream();
            pdfUrl = await _fileStorage.UploadAsync(stream, pdf.FileName, pdf.ContentType, $"incomes/{id}");
        }

        if (xml is not null)
        {
            using var stream = xml.OpenReadStream();
            xmlUrl = await _fileStorage.UploadAsync(stream, xml.FileName, xml.ContentType, $"incomes/{id}");

            // Parse CFDI XML metadata
            stream.Position = 0;
            xmlMetadata = CfdiParser.Parse(stream);
        }

        var updated = await _incomeService.UpdateFileUrlsAsync(tenantId, id, pdfUrl, xmlUrl, xmlMetadata);
        return Ok(updated);
    }
}
