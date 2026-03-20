using CardioFlow.Api.BackgroundServices;
using CardioFlow.Api.Hubs;
using CardioFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendClients", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var origins = (configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : ["http://localhost:5173"]);

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Register TelemetryBufferService as Singleton
builder.Services.AddSingleton<ITelemetryBufferService, TelemetryBufferService>();
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddSingleton<IAlertRule, AnnotationAlertRule>();
builder.Services.AddSingleton<IAlertRule, HeartRateThresholdRule>();
builder.Services.AddSingleton<IAlertRule, RrIntervalRule>();
builder.Services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddSingleton<IStatusAggregationService, StatusAggregationService>();

// Register Kafka Consumer Background Service
builder.Services.AddHostedService<KafkaConsumerService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure JSON options for controllers
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendClients");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();
app.MapHub<TelemetryHub>("/hubs/telemetry");

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("CardioFlow API starting...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Configured URL (launch profile): http://localhost:5050");
logger.LogInformation("Kafka Bootstrap Servers: {BootstrapServers}",
    builder.Configuration["Kafka:BootstrapServers"] ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092");
logger.LogInformation("Kafka Telemetry Topic: {TelemetryTopic}",
    builder.Configuration["Kafka:TelemetryTopic"] ?? "ecg.telemetry");

app.Run();
