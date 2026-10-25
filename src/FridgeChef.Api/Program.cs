using FridgeChef.Api;
using FridgeChef.Api.Auth;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseWhen<AuthMiddleware>(AuthMiddleware.AppliesTo);
builder.Services.AddFridgeChefApi(builder.Configuration);

builder.Build().Run();
