using Cuintable.Server.DTOs.FinancialAdvisor;
using Cuintable.Server.Models;

namespace Cuintable.Server.Services;

public interface IFinancialAdvisorService
{
    Task<CardStatement> CreateStatementAsync(Guid tenantId, Guid userId, Guid statementId, Guid? creditCardId, string pdfUrl);
    Task<CardStatement?> GetEntityAsync(Guid tenantId, Guid id);
    Task<List<StatementSummaryResponse>> GetAllAsync(Guid tenantId, int? year, Guid? creditCardId);
    Task<StatementDetailResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
    Task<StatementTransactionResponse?> ReviewTransactionAsync(Guid tenantId, Guid transactionId, TransactionReviewStatus status);
    Task<AdvisorDashboardResponse> GetDashboardAsync(Guid tenantId, int year, int month);
}
