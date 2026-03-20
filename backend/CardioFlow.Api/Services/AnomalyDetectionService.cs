using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly ILogger<AnomalyDetectionService> _logger;
    private readonly IReadOnlyList<IAlertRule> _rules;
    private readonly DetectionContext _detectionContext;

    public AnomalyDetectionService(
        IConfiguration configuration,
        IEnumerable<IAlertRule> rules,
        ILogger<AnomalyDetectionService> logger)
    {
        _logger = logger;
        _rules = rules.ToList();
        _detectionContext = new DetectionContext
        {
            HrHighThreshold = configuration.GetValue<int>("DetectionRules:HrHighThreshold", 120),
            HrLowThreshold = configuration.GetValue<int>("DetectionRules:HrLowThreshold", 45),
            RrLowMs = configuration.GetValue<int>("DetectionRules:RrLowMs", 400),
            RrHighMs = configuration.GetValue<int>("DetectionRules:RrHighMs", 1200),
            EnableRrRule = configuration.GetValue<bool>("DetectionRules:EnableRrRule", true)
        };
    }

    public IReadOnlyList<AlertMessage> DetectAlerts(TelemetryMessage telemetryMessage)
    {
        if (telemetryMessage == null)
        {
            return Array.Empty<AlertMessage>();
        }

        // Rule execution order is deterministic (DI registration order),
        // but merge strategy always keeps one winner by highest severity:
        // critical > warning > normal.
        var candidates = new List<(IAlertRule Rule, AlertCandidate Candidate)>();
        foreach (var rule in _rules)
        {
            try
            {
                var result = rule.Evaluate(telemetryMessage, _detectionContext);
                if (result != null)
                {
                    candidates.Add((rule, result));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Alert rule evaluation failed. ruleName={RuleName}, sampleIndex={SampleIndex}",
                    rule.Name,
                    telemetryMessage.SampleIndex);
            }
        }

        if (candidates.Count == 0)
        {
            return Array.Empty<AlertMessage>();
        }

        var winner = candidates
            .OrderByDescending(item => SeverityRank(item.Candidate.Severity))
            .First();
        var matchedRuleNames = candidates.Select(item => item.Rule.Name).Distinct(StringComparer.Ordinal).ToArray();

        _logger.LogInformation(
            "Alert selected. ruleName={RuleName}, sampleIndex={SampleIndex}, severity={Severity}, message={Message}",
            winner.Rule.Name,
            telemetryMessage.SampleIndex,
            winner.Candidate.Severity,
            winner.Candidate.Message);

        return new[]
        {
            BuildAlert(telemetryMessage, winner.Candidate, matchedRuleNames)
        };
    }

    private static AlertMessage BuildAlert(
        TelemetryMessage telemetryMessage,
        AlertCandidate candidate,
        IReadOnlyCollection<string> matchedRuleNames)
    {
        return new AlertMessage
        {
            PatientId = telemetryMessage.PatientId,
            RecordId = telemetryMessage.RecordId,
            DeviceId = telemetryMessage.DeviceId,
            Timestamp = telemetryMessage.Timestamp,
            ReceivedAt = telemetryMessage.ReceivedAt,
            SampleIndex = telemetryMessage.SampleIndex,
            Annotation = telemetryMessage.Annotation,
            Severity = NormalizeSeverity(candidate.Severity),
            Message = candidate.Message,
            SourceRule = candidate.SourceRule,
            HeartRate = telemetryMessage.HeartRate,
            RrIntervalMs = telemetryMessage.RrIntervalMs,
            Metadata = new Dictionary<string, string>
            {
                ["matchedRules"] = string.Join(",", matchedRuleNames),
                ["matchedCount"] = matchedRuleNames.Count.ToString()
            }
        };
    }

    private static string NormalizeSeverity(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "critical" => "critical",
            "warning" => "warning",
            "normal" => "normal",
            _ => "warning"
        };
    }

    private static int SeverityRank(string? value)
    {
        return NormalizeSeverity(value) switch
        {
            "critical" => 3,
            "warning" => 2,
            "normal" => 1,
            _ => 0
        };
    }
}
