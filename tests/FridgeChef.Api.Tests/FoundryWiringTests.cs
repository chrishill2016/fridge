using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Azure.AI.Projects;
using Azure.Core;
using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Foundry;
using FridgeChef.Api.Tests.Fakes;

namespace FridgeChef.Api.Tests;

/// <summary>
/// Runs a detection through the real Foundry SDK into a fake HTTP handler, to check what goes
/// over the wire (endpoint, model, stored output off, image, JSON schema) without calling Azure.
/// </summary>
public class FoundryWiringTests
{
    [Fact]
    public async Task Detection_sends_expected_responses_request()
    {
        var handler = new CapturingHandler();
        var project = new AIProjectClient(
            new Uri("https://test-ai.services.ai.azure.com/api/projects/test-project"),
            new FakeCredential(),
            new AIProjectClientOptions { Transport = new HttpClientPipelineTransport(new HttpClient(handler)) });
        using var provider = new FoundryChatClientProvider(project);
        var detector = new IngredientDetector(provider);

        var result = await detector.DetectAsync(new ValidatedImage(TestImages.Jpeg(), "image/jpeg"), "gpt-4.1-mini", TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Request);
        Assert.Equal("test-ai.services.ai.azure.com", handler.Request.RequestUri!.Host);
        Assert.Contains("/responses", handler.Request.RequestUri.AbsolutePath);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);

        var body = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("gpt-4.1-mini", (string?)body["model"]);
        Assert.False((bool?)body["store"]);
        Assert.Equal("json_schema", (string?)body["text"]?["format"]?["type"]);
        Assert.Contains("data:image/jpeg;base64,", handler.Body);

        Assert.Equal(["eggs"], result.Ingredients.Select(i => i.Name));
        Assert.Equal(42, result.Run.InputTokens);
    }

    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("fake-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            const string output = """{\"ingredients\":[{\"name\":\"eggs\",\"quantity\":\"6\",\"confidence\":0.9}]}""";
            var json = $$"""
                {
                  "id": "resp_test",
                  "object": "response",
                  "created_at": 1760000000,
                  "status": "completed",
                  "model": "gpt-4.1-mini",
                  "output": [{
                    "type": "message",
                    "id": "msg_test",
                    "status": "completed",
                    "role": "assistant",
                    "content": [{ "type": "output_text", "text": "{{output}}", "annotations": [] }]
                  }],
                  "usage": { "input_tokens": 42, "output_tokens": 7, "total_tokens": 49 }
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
