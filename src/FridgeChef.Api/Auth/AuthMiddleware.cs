using FridgeChef.Api.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace FridgeChef.Api.Auth;

/// <summary>Runs before every HTTP function, so no endpoint can forget to check the caller.</summary>
public sealed class AuthMiddleware(IAccessTokenValidator validator) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext()
            ?? throw new InvalidOperationException("Auth middleware only applies to HTTP functions.");

        var result = await validator.ValidateAsync(http.Request.Headers.Authorization.ToString(), http.RequestAborted);
        if (!result.Allowed)
        {
            var problem = Problems.Create(result.Status, result.Problem!);
            http.Response.StatusCode = result.Status;
            if (result.Status == StatusCodes.Status401Unauthorized)
            {
                http.Response.Headers.WWWAuthenticate = "Bearer";
            }

            await http.Response.WriteAsJsonAsync(problem.Value, http.RequestAborted);
            return;
        }

        await next(context);
    }

    public static bool AppliesTo(FunctionContext context) =>
        context.FunctionDefinition.InputBindings.Values.Any(b => b.Type.Equals("httpTrigger", StringComparison.OrdinalIgnoreCase));
}
