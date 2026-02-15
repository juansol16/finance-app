using System.Security.Claims;
using Cuintable.Server.DTOs.CreditCards;
using Cuintable.Server.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _service;
    private readonly IValidator<CreateCreditCardRequest> _createValidator;
    private readonly IValidator<UpdateCreditCardRequest> _updateValidator;

    public CreditCardsController(
        ICreditCardService service,
        IValidator<CreateCreditCardRequest> createValidator,
        IValidator<UpdateCreditCardRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<CreditCardResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync(GetUserId()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditCardResponse>> GetById(Guid id)
    {
        var card = await _service.GetByIdAsync(GetUserId(), id);
        if (card is null) return NotFound();
        return Ok(card);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create([FromBody] CreateCreditCardRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var card = await _service.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CreditCardResponse>> Update(Guid id, [FromBody] UpdateCreditCardRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var card = await _service.UpdateAsync(GetUserId(), id, request);
        if (card is null) return NotFound();
        return Ok(card);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(GetUserId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
