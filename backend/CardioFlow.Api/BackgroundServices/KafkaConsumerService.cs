using System.Text.Json;
using CardioFlow.Api.Hubs;
using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace CardioFlow.Api.BackgroundServices;

/// <summary>
/// Background service that consumes ECG telemetry messages from Kafka
/// and stores them in the telemetry buffer service.
/// </summary>
public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITelemetryBufferService _bufferService;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly IAlertService _alertService;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private IConsumer<string, string>? _consumer;
    private readonly string _bootstrapServers;
    private readonly string _consumerGroupId;
    private readonly string _topic;
    private int _messagesConsumed;
    private int _messagesFailed;

    /// <summary>
    /// Initializes a new instance of the KafkaConsumerService.
    /// </summary>
    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IConfiguration configuration,
        ITelemetryBufferService bufferService,
        IAnomalyDetectionService anomalyDetectionService,
        IAlertService alertService,
        IHubContext<TelemetryHub> hubContext)
    {
        _logger = logger;
        _configuration = configuration;
        _bufferService = bufferService;
        _anomalyDetectionService = anomalyDetectionService;
        _alertService = alertService;
        _hubContext = hubContext;

        // Read Kafka configuration from appsettings.json or environment variables
        _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? configuration["Kafka:BootstrapServers"]
            ?? "localhost:9092";

        _consumerGroupId = configuration["Kafka:ConsumerGroupId"] ?? "cardioflow-api-consumer";
        _topic = configuration["Kafka:TelemetryTopic"] ?? "ecg.telemetry";

        _messagesConsumed = 0;
        _messagesFailed = 0;

        _logger.LogInformation(
            "KafkaConsumerService initialized: BootstrapServers={BootstrapServers}, GroupId={GroupId}, Topic={Topic}",
            _bootstrapServers,
            _consumerGroupId,
            _topic);
    }

    /// <summary>
    /// Starts the Kafka consumer service.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Consumer Service starting...");

        try
        {
            await InitializeConsumerAsync();
            _logger.LogInformation("Kafka Consumer Service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Kafka Consumer Service");
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the main consumer loop.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumer loop...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_consumer == null)
                {
                    _logger.LogWarning("Consumer is null, attempting to reinitialize...");
                    await InitializeConsumerAsync();
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                    continue;
                }

                // Consume message with timeout
                var result = _consumer.Consume(TimeSpan.FromSeconds(1));

                if (result != null && !result.IsPartitionEOF)
                {
                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                }
            }
            catch (ConsumeException ex)
            {
                _messagesFailed++;
                _logger.LogError(ex, "Error consuming message from Kafka: {Error}", ex.Error.Reason);

                // If it's a fatal error, try to reinitialize
                if (ex.Error.IsFatal)
                {
                    _logger.LogWarning("Fatal error detected, attempting to reinitialize consumer...");
                    await ReinitializeConsumerAsync(stoppingToken);
                }
            }
            catch (KafkaException ex)
            {
                _messagesFailed++;
                _logger.LogError(ex, "Kafka exception occurred: {Message}", ex.Message);
                await ReinitializeConsumerAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _messagesFailed++;
                _logger.LogError(ex, "Unexpected error in consumer loop: {Message}", ex.Message);
                await Task.Delay(5000, stoppingToken); // Wait before continuing
            }
        }

        _logger.LogInformation("Kafka consumer loop stopped");
    }

    /// <summary>
    /// Stops the Kafka consumer service gracefully.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Consumer Service stopping...");

        if (_consumer != null)
        {
            try
            {
                _consumer.Close();
                _consumer.Dispose();
                _logger.LogInformation("Kafka consumer closed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Kafka consumer");
            }
        }

        _logger.LogInformation(
            "Kafka Consumer Service stopped. Total messages consumed: {Consumed}, Failed: {Failed}",
            _messagesConsumed,
            _messagesFailed);

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Initializes the Kafka consumer.
    /// </summary>
    private async Task InitializeConsumerAsync()
    {
        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _consumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest, // Start from earliest for development
                EnableAutoCommit = true,
                AutoCommitIntervalMs = 5000,
                EnablePartitionEof = true,
                SessionTimeoutMs = 30000,
                MaxPollIntervalMs = 300000
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError("Kafka consumer error: {Reason}", e.Reason);
                })
                .SetLogHandler((_, message) =>
                {
                    _logger.LogDebug("Kafka log: {Message}", message.Message);
                })
                .Build();

            _consumer.Subscribe(_topic);

            _logger.LogInformation(
                "Kafka consumer initialized and subscribed to topic: {Topic}",
                _topic);

            // Small delay to ensure subscription is complete
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Kafka consumer");
            throw;
        }
    }

    /// <summary>
    /// Reinitializes the consumer after an error.
    /// </summary>
    private async Task ReinitializeConsumerAsync(CancellationToken stoppingToken)
    {
        if (_consumer != null)
        {
            try
            {
                _consumer.Close();
                _consumer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing consumer during reinitialization");
            }
        }

        _consumer = null;

        // Exponential backoff: wait 1s, 2s, 4s, etc. (max 30s)
        var waitTime = Math.Min(30000, (int)Math.Pow(2, _messagesFailed / 10) * 1000);
        _logger.LogInformation("Waiting {WaitTime}ms before reinitializing consumer...", waitTime);

        await Task.Delay(waitTime, stoppingToken);

        try
        {
            await InitializeConsumerAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reinitialize consumer");
        }
    }

    /// <summary>
    /// Processes a single Kafka message.
    /// </summary>
    private async Task ProcessMessageAsync(string jsonMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Deserialize JSON message
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var telemetryMessage = JsonSerializer.Deserialize<TelemetryMessage>(jsonMessage, options);

            if (telemetryMessage == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", jsonMessage);
                _messagesFailed++;
                return;
            }

            // Add to buffer
            _bufferService.Add(telemetryMessage);
            await TryBroadcastAsync("ReceiveTelemetry", telemetryMessage, cancellationToken);

            // Detect and store alerts based on telemetry payload.
            var alerts = _anomalyDetectionService.DetectAlerts(telemetryMessage);
            foreach (var alert in alerts)
            {
                _alertService.AddAlert(alert);
                await TryBroadcastAsync("ReceiveAlert", alert, cancellationToken);
                _logger.LogInformation(
                    "Alert generated: patientId={PatientId}, sampleIndex={SampleIndex}, severity={Severity}, message={Message}",
                    alert.PatientId,
                    alert.SampleIndex,
                    alert.Severity,
                    alert.Message);
            }

            _messagesConsumed++;

            // Log every 100 messages
            if (_messagesConsumed % 100 == 0)
            {
                _logger.LogInformation(
                    "Consumed {Count} messages. Latest: sampleIndex={SampleIndex}, lead1={Lead1}, annotation={Annotation}, bufferSize={BufferSize}",
                    _messagesConsumed,
                    telemetryMessage.SampleIndex,
                    telemetryMessage.Lead1,
                    telemetryMessage.Annotation,
                    _bufferService.GetCount());

                await TryBroadcastAsync("ReceiveSystemStatus", BuildSystemStatus(), cancellationToken);
            }
            else
            {
                _logger.LogDebug(
                    "Consumed message: sampleIndex={SampleIndex}, lead1={Lead1}, annotation={Annotation}",
                    telemetryMessage.SampleIndex,
                    telemetryMessage.Lead1,
                    telemetryMessage.Annotation);
            }
        }
        catch (JsonException ex)
        {
            _messagesFailed++;
            _logger.LogError(ex, "JSON deserialization error: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _messagesFailed++;
            _logger.LogError(ex, "Error processing message: {Message}", ex.Message);
        }

        await Task.CompletedTask;
    }

    private SystemStatusDto BuildSystemStatus()
    {
        var latestTelemetry = _bufferService.GetLatest(1).FirstOrDefault();
        var bufferCount = _bufferService.GetCount();
        var lastMessageAt = _bufferService.GetLastMessageAt();
        var now = DateTime.UtcNow;

        var streamStatus = "stopped";
        if (bufferCount > 0)
        {
            streamStatus = lastMessageAt.HasValue && now - lastMessageAt.Value <= TimeSpan.FromSeconds(30)
                ? "running"
                : "idle";
        }

        return new SystemStatusDto
        {
            StreamStatus = streamStatus,
            SamplingRate = _configuration.GetValue<int>("Telemetry:SamplingRate", 360),
            Topic = _configuration["Kafka:TelemetryTopic"] ?? "ecg.telemetry",
            ActivePatient = latestTelemetry?.PatientId,
            LastAlert = _alertService.GetLastAlert()?.Message,
            BufferCount = bufferCount,
            LastMessageAt = lastMessageAt
        };
    }

    private async Task TryBroadcastAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for event {EventName}", eventName);
        }
    }
}
