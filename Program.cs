using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure.Persistence;
using OrderApi.Application.Interfaces;
using OrderApi.Application.Services;
using OrderApi.Infrastructure.Repositories;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OrderApi.Application.Interfaces.Application.Interfaces;
using Serilog;
using Serilog.Events;
using OrderApi.Infrastructure.Observability;
using OrderApi.Infrastructure.External;

var builder = WebApplication.CreateBuilder(args);

// ===== Serilog (logs estruturados JSON no console) =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("service", "order-api")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ===== OpenTelemetry =====
var serviceName = "order-api";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                o.EnrichWithHttpRequest = (activity, request) =>
                {
                    if (request.Headers.TryGetValue("X-Correlation-Id", out var cid))
                        activity.SetTag("correlation_id", cid.ToString());

                    if (request.Headers.TryGetValue("Idempotency-Key", out var idem))
                        activity.SetTag("idempotency_key", idem.ToString());
                };
            })
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
            .AddOtlpExporter();//OTLP_ENDPOINT (Datadog/Collector)
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISessionService, MockSessionService>();
builder.Services.AddScoped<IAntiFraudClient, MockAntiFraudClient>();
builder.Services.AddScoped<IBalanceClient, MockBalanceClient>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseSerilogRequestLogging(opts =>
{
    // trace_id/span_id automaticamente via Activity
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("trace_id", System.Diagnostics.Activity.Current?.TraceId.ToString());
        diag.Set("span_id", System.Diagnostics.Activity.Current?.SpanId.ToString());
        diag.Set("path", http.Request.Path.Value);
        diag.Set("method", http.Request.Method);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();
app.Run();
