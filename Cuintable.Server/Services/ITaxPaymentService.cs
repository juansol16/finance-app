using Cuintable.Server.DTOs.TaxPayments;

namespace Cuintable.Server.Services;

public interface ITaxPaymentService
{
    Task<List<TaxPaymentResponse>> GetAllAsync(Guid userId);
    Task<TaxPaymentResponse?> GetByIdAsync(Guid userId, Guid id);
    Task<TaxPaymentResponse> CreateAsync(Guid userId, CreateTaxPaymentRequest request);
    Task<TaxPaymentResponse?> UpdateAsync(Guid userId, Guid id, UpdateTaxPaymentRequest request);
    Task<TaxPaymentResponse?> UpdateDeterminationUrlAsync(Guid userId, Guid id, string determinationUrl);
    Task<TaxPaymentResponse?> UpdateReceiptUrlAsync(Guid userId, Guid id, string receiptUrl);
    Task<TaxPaymentResponse?> MarkAsPaidAsync(Guid userId, Guid id, MarkAsPaidRequest request, string? receiptUrl);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
