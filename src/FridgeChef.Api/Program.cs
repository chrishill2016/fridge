using FridgeChef.Api;
using FridgeChef.Api.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseWhen<AuthMiddleware>(AuthMiddleware.AppliesTo);
builder.Services.AddFridgeChefApi(builder.Configuration);

// Sends the worker's logs and dependency calls (Foundry, token metadata) to Application Insights.
// Does nothing locally unless APPLICATIONINSIGHTS_CONNECTION_STRING is set.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// The Application Insights provider adds a Warning-only filter by default. Remove it so the
// Information logs (model, prompt version, token counts per call) reach Application Insights.
builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    var defaultRule = options.Rules.FirstOrDefault(rule =>
        rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }
});

builder.Build().Run();
