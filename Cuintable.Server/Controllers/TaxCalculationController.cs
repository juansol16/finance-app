using System.Security.Claims;
using Cuintable.Server.DTOs.Tax;
using Cuintable.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cuintable.Server.Controllers;

[ApiController]
[Route("api/tax")]
[Authorize(Roles = "Owner,Pareja")]
public class TaxCalculationController : ControllerBase
{
    private readonly IResicoTaxService _taxService;

    public TaxCalculationController(IResicoTaxService taxService)
    {
        _taxService = taxService;
    }

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirstValue("TenantId")!);

    [HttpGet("summary")]
    public async Task<ActionResult<TaxSummaryResponse>> GetMonthlySummary([FromQuery] int month, [FromQuery] int year)
    {
        if (month < 1 || month > 12)
            return BadRequest("Month must be between 1 and 12.");

        if (year < 2000 || year > 2100)
            return BadRequest("Invalid year.");

        var summary = await _taxService.GetMonthlySummaryAsync(GetTenantId(), month, year);
        return Ok(summary);
    }

    [HttpGet("annual-summary")]
    public async Task<ActionResult<AnnualTaxSummaryResponse>> GetAnnualSummary([FromQuery] int year)
    {
        if (year < 2000 || year > 2100)
            return BadRequest("Invalid year.");

        var summary = await _taxService.GetAnnualSummaryAsync(GetTenantId(), year);
        return Ok(summary);
    }
}
