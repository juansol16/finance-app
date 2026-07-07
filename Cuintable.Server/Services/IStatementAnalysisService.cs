namespace Cuintable.Server.Services;

public interface IStatementAnalysisService
{
    /// <summary>
    /// Runs the full pipeline (extract → analyze → advise) on an uploaded statement.
    /// Updates Status/ErrorMessage on the statement; never throws for processing failures.
    /// </summary>
    Task ProcessAsync(Guid tenantId, Guid statementId);
}
