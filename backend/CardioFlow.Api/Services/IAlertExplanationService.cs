using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public interface IAlertExplanationService
{
    /// <summary>
    /// Calls the explainer service and copies results onto the alert. No-op if explainer is not configured or the call fails.
    /// </summary>
    Task TryEnrichAsync(AlertMessage alert, CancellationToken cancellationToken = default);
}
