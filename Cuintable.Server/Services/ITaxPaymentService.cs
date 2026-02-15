using Cuintable.Server.DTOs.TaxPayments;

namespace Cuintable.Server.Services;

public interface ITaxPaymentService
{
    Task<List<TaxPaymentResponse>> GetAllAsync(Guid tenantId);
    Task<TaxPaymentResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<TaxPaymentResponse> CreateAsync(Guid tenantId, Guid userId, CreateTaxPaymentRequest request);
    Task<TaxPaymentResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateTaxPaymentRequest request);
    Task<TaxPaymentResponse?> UpdateDeterminationUrlAsync(Guid tenantId, Guid id, string determinationUrl);
    Task<TaxPaymentResponse?> UpdateReceiptUrlAsync(Guid tenantId, Guid id, string receiptUrl);
    Task<TaxPaymentResponse?> MarkAsPaidAsync(Guid tenantId, Guid id, MarkAsPaidRequest request, string? receiptUrl);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
}
