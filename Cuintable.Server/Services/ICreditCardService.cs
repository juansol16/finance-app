using Cuintable.Server.DTOs.CreditCards;

namespace Cuintable.Server.Services;

public interface ICreditCardService
{
    Task<List<CreditCardResponse>> GetAllAsync(Guid tenantId);
    Task<CreditCardResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<CreditCardResponse> CreateAsync(Guid tenantId, Guid userId, CreateCreditCardRequest request);
    Task<CreditCardResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateCreditCardRequest request);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
}
