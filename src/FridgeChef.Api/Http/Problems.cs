using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Http;

public static class Problems
{
    public static ObjectResult Create(int status, string detail) =>
        new(new ProblemDetails { Status = status, Detail = detail }) { StatusCode = status };
}
