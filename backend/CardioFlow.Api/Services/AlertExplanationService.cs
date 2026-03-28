using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AlertExplanationService : IAlertExplanationService
{
    private const string ConfigKey = "Explainer:BaseUrl";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlertExplanationService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public AlertExplanationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AlertExplanationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task TryEnrichAsync(AlertMessage alert, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration[ConfigKey]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var requestUri = $"{baseUrl.TrimEnd('/')}/explain";
        var payload = new AlertExplanationRequestDto
        {
            PatientId = alert.PatientId,
            Annotation = alert.Annotation,
            HeartRate = alert.HeartRate,
            RrInterval = alert.RrIntervalMs.HasValue ? alert.RrIntervalMs.Value / 1000.0 : null,
            Severity = alert.Severity,
            Message = alert.Message
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(requestUri, payload, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Explainer returned {StatusCode} for patientId={PatientId}, message={Message}",
                    (int)response.StatusCode,
                    alert.PatientId,
                    alert.Message);
                return;
            }

            var body = await response.Content
                .ReadFromJsonAsync<ExplanationResponseDto>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (body == null)
            {
                _logger.LogWarning("Explainer returned empty body for patientId={PatientId}", alert.PatientId);
                return;
            }

            alert.ExplanationSummary = body.Summary;
            alert.ExplanationDetails = body.Explanation;
            alert.RecommendedAction = body.RecommendedAction;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explainer call failed for patientId={PatientId}", alert.PatientId);
        }
    }
}
