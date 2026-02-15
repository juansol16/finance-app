using System.Security.Claims;
using Cuintable.Server.DTOs.TaxPayments;
using Cuintable.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/tax-payments")]
[Authorize(Roles = "Owner,Contador,Pareja")]
public class TaxPaymentsController : ControllerBase
{
    private readonly ITaxPaymentService _taxPaymentService;
    private readonly IFileStorageService _fileStorage;

    public TaxPaymentsController(ITaxPaymentService taxPaymentService, IFileStorageService fileStorage)
    {
        _taxPaymentService = taxPaymentService;
        _fileStorage = fileStorage;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirstValue("TenantId")!);

    [HttpGet]
    public async Task<ActionResult<List<TaxPaymentResponse>>> GetAll()
    {
        var payments = await _taxPaymentService.GetAllAsync(GetTenantId());
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxPaymentResponse>> GetById(Guid id)
    {
        var payment = await _taxPaymentService.GetByIdAsync(GetTenantId(), id);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<TaxPaymentResponse>> Create([FromBody] CreateTaxPaymentRequest request)
    {
        var payment = await _taxPaymentService.CreateAsync(GetTenantId(), GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxPaymentResponse>> Update(Guid id, [FromBody] UpdateTaxPaymentRequest request)
    {
        var payment = await _taxPaymentService.UpdateAsync(GetTenantId(), id, request);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    [HttpPost("{id:guid}/determination")]
    public async Task<ActionResult<TaxPaymentResponse>> UploadDetermination(Guid id, [FromForm] IFormFile file)
    {
        var tenantId = GetTenantId();
        var payment = await _taxPaymentService.GetByIdAsync(tenantId, id);
        if (payment is null) return NotFound();

        using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType, $"tax-payments/{id}/determination");

        var updated = await _taxPaymentService.UpdateDeterminationUrlAsync(tenantId, id, url);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/receipt")]
    public async Task<ActionResult<TaxPaymentResponse>> UploadReceipt(Guid id, [FromForm] IFormFile file)
    {
        var tenantId = GetTenantId();
        var payment = await _taxPaymentService.GetByIdAsync(tenantId, id);
        if (payment is null) return NotFound();

        using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType, $"tax-payments/{id}/receipt");

        var updated = await _taxPaymentService.UpdateReceiptUrlAsync(tenantId, id, url);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/mark-paid")]
    public async Task<ActionResult<TaxPaymentResponse>> MarkAsPaid(Guid id, [FromForm] DateOnly paymentDate, [FromForm] IFormFile? receipt)
    {
        var tenantId = GetTenantId();
        var payment = await _taxPaymentService.GetByIdAsync(tenantId, id);
        if (payment is null) return NotFound();

        string? receiptUrl = null;
        if (receipt is not null)
        {
            using var stream = receipt.OpenReadStream();
            receiptUrl = await _fileStorage.UploadAsync(stream, receipt.FileName, receipt.ContentType, $"tax-payments/{id}/receipt");
        }

        var request = new MarkAsPaidRequest { PaymentDate = paymentDate };
        var updated = await _taxPaymentService.MarkAsPaidAsync(tenantId, id, request, receiptUrl);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _taxPaymentService.DeleteAsync(GetTenantId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
