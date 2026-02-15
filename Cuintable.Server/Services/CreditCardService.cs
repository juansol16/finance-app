using Cuintable.Server.Data;
using Cuintable.Server.DTOs.CreditCards;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class CreditCardService : ICreditCardService
{
    private readonly AppDbContext _db;

    public CreditCardService(AppDbContext db) => _db = db;

    public async Task<List<CreditCardResponse>> GetAllAsync(Guid tenantId)
    {
        return await _db.CreditCards
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Bank).ThenBy(c => c.Nickname)
            .Select(c => MapToResponse(c))
            .ToListAsync();
    }

    public async Task<CreditCardResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var card = await _db.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        return card is null ? null : MapToResponse(card);
    }

    public async Task<CreditCardResponse> CreateAsync(Guid tenantId, Guid userId, CreateCreditCardRequest request)
    {
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Bank = request.Bank,
            Nickname = request.Nickname,
            LastFourDigits = request.LastFourDigits
        };

        _db.CreditCards.Add(card);
        await _db.SaveChangesAsync();
        return MapToResponse(card);
    }

    public async Task<CreditCardResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateCreditCardRequest request)
    {
        var card = await _db.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (card is null) return null;

        card.Bank = request.Bank;
        card.Nickname = request.Nickname;
        card.LastFourDigits = request.LastFourDigits;
        card.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return MapToResponse(card);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var card = await _db.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (card is null) return false;

        _db.CreditCards.Remove(card);
        await _db.SaveChangesAsync();
        return true;
    }

    private static CreditCardResponse MapToResponse(CreditCard card) => new()
    {
        Id = card.Id,
        Bank = card.Bank,
        Nickname = card.Nickname,
        LastFourDigits = card.LastFourDigits,
        IsActive = card.IsActive,
        CreatedAt = card.CreatedAt
    };
}
