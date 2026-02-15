using Cuintable.Server.DTOs.CreditCards;

namespace Cuintable.Server.Services;

public interface ICreditCardService
{
    Task<List<CreditCardResponse>> GetAllAsync(Guid userId);
    Task<CreditCardResponse?> GetByIdAsync(Guid userId, Guid id);
    Task<CreditCardResponse> CreateAsync(Guid userId, CreateCreditCardRequest request);
    Task<CreditCardResponse?> UpdateAsync(Guid userId, Guid id, UpdateCreditCardRequest request);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
