using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FridgeChef.Api.Tests.Fakes;

public static class JsonRequests
{
    public static HttpRequest Post(object body) => Post(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    public static HttpRequest Post(string json, string contentType = "application/json")
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = contentType;
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
        return context.Request;
    }
}
