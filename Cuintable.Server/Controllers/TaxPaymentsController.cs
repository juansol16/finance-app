using System.Security.Claims;
using Cuintable.Server.DTOs.TaxPayments;
using Cuintable.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/tax-payments")]
[Authorize]
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

    [HttpGet]
    public async Task<ActionResult<List<TaxPaymentResponse>>> GetAll()
    {
        var payments = await _taxPaymentService.GetAllAsync(GetUserId());
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxPaymentResponse>> GetById(Guid id)
    {
        var payment = await _taxPaymentService.GetByIdAsync(GetUserId(), id);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<TaxPaymentResponse>> Create([FromBody] CreateTaxPaymentRequest request)
    {
        var payment = await _taxPaymentService.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxPaymentResponse>> Update(Guid id, [FromBody] UpdateTaxPaymentRequest request)
    {
        var payment = await _taxPaymentService.UpdateAsync(GetUserId(), id, request);
        if (payment is null) return NotFound();
        return Ok(payment);
    }

    [HttpPost("{id:guid}/determination")]
    public async Task<ActionResult<TaxPaymentResponse>> UploadDetermination(Guid id, [FromForm] IFormFile file)
    {
        var userId = GetUserId();
        var payment = await _taxPaymentService.GetByIdAsync(userId, id);
        if (payment is null) return NotFound();

        using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType, $"tax-payments/{id}/determination");

        var updated = await _taxPaymentService.UpdateDeterminationUrlAsync(userId, id, url);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/receipt")]
    public async Task<ActionResult<TaxPaymentResponse>> UploadReceipt(Guid id, [FromForm] IFormFile file)
    {
        var userId = GetUserId();
        var payment = await _taxPaymentService.GetByIdAsync(userId, id);
        if (payment is null) return NotFound();

        using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType, $"tax-payments/{id}/receipt");

        var updated = await _taxPaymentService.UpdateReceiptUrlAsync(userId, id, url);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/mark-paid")]
    public async Task<ActionResult<TaxPaymentResponse>> MarkAsPaid(Guid id, [FromForm] DateOnly paymentDate, [FromForm] IFormFile? receipt)
    {
        var userId = GetUserId();
        var payment = await _taxPaymentService.GetByIdAsync(userId, id);
        if (payment is null) return NotFound();

        string? receiptUrl = null;
        if (receipt is not null)
        {
            using var stream = receipt.OpenReadStream();
            receiptUrl = await _fileStorage.UploadAsync(stream, receipt.FileName, receipt.ContentType, $"tax-payments/{id}/receipt");
        }

        var request = new MarkAsPaidRequest { PaymentDate = paymentDate };
        var updated = await _taxPaymentService.MarkAsPaidAsync(userId, id, request, receiptUrl);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _taxPaymentService.DeleteAsync(GetUserId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
