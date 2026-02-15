using System.Security.Claims;
using Cuintable.Server.DTOs.Expenses;
using Cuintable.Server.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;
    private readonly IValidator<CreateExpenseRequest> _createValidator;
    private readonly IValidator<UpdateExpenseRequest> _updateValidator;

    public ExpensesController(
        IExpenseService service,
        IValidator<CreateExpenseRequest> createValidator,
        IValidator<UpdateExpenseRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<ExpenseResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync(GetUserId()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(Guid id)
    {
        var expense = await _service.GetByIdAsync(GetUserId(), id);
        if (expense is null) return NotFound();
        return Ok(expense);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create([FromBody] CreateExpenseRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var expense = await _service.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> Update(Guid id, [FromBody] UpdateExpenseRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var expense = await _service.UpdateAsync(GetUserId(), id, request);
        if (expense is null) return NotFound();
        return Ok(expense);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(GetUserId(), id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
