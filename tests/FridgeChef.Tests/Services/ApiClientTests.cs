using System.Net;
using System.Text;
using FridgeChef.Services.Api;
using FridgeChef.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Services;

public class ApiClientTests
{
    private static readonly PreparedPhoto Photo = new([0xFF, 0xD8, 0xFF, 9], "image/jpeg");

    [Fact]
    public async Task Posts_photo_and_model_as_multipart_and_reads_result()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            {"ingredients":[{"name":"eggs","quantity":"6","confidence":0.9}],
             "run":{"model":"gpt-4.1-mini","promptVersion":"ingredients-v1","inputTokens":10,"outputTokens":2}}
            """);
        var sut = Client(handler);

        var result = await sut.AnalysePhotoAsync(Photo, "phi-4", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("http://localhost:7071/api/ingredients", handler.Request.RequestUri!.ToString());
        Assert.StartsWith("multipart/form-data", handler.ContentType);
        Assert.Contains("name=image; filename=photo.jpg", handler.Body);
        Assert.Contains("Content-Type: image/jpeg", handler.Body);
        Assert.Contains("name=model", handler.Body);
        Assert.Contains("phi-4", handler.Body);

        Assert.Equal("eggs", Assert.Single(result.Ingredients).Name);
        Assert.Equal(10, result.Run.InputTokens);
    }

    [Fact]
    public async Task Model_field_is_omitted_when_not_chosen()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"ingredients":[],"run":{"model":"m","promptVersion":"v"}}""");

        await Client(handler).AnalysePhotoAsync(Photo, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        Assert.DoesNotContain("name=model", handler.Body);
    }

    [Fact]
    public async Task Problem_detail_becomes_the_error_message()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"status":400,"detail":"The photo must be a JPEG, PNG or WebP image."}""", "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Equal("The photo must be a JPEG, PNG or WebP image.", ex.Message);
    }

    [Fact]
    public async Task Non_json_error_gets_generic_message_with_status()
    {
        var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, "<html>down</html>", "text/html");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Contains("503", ex.Message);
    }

    [Fact]
    public async Task Network_failure_becomes_friendly_error()
    {
        var handler = new StubHandler(new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Contains("Couldn't reach", ex.Message);
    }

    [Fact]
    public async Task Demo_api_returns_sample_data_after_a_short_delay()
    {
        var time = new ManualTimeProvider();
        var sut = new DemoFridgeChefApi(time);

        var task = sut.AnalysePhotoAsync(Photo, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        Assert.False(task.IsCompleted);
        time.Advance(DemoFridgeChefApi.Delay);
        var result = await task;

        Assert.True(sut.IsDemo);
        Assert.NotEmpty(result.Ingredients);
        Assert.Equal("demo", result.Run.Model);
    }

    [Theory]
    [InlineData(null, typeof(DemoFridgeChefApi))]
    [InlineData("", typeof(DemoFridgeChefApi))]
    [InlineData("http://localhost:7071/api/", typeof(HttpFridgeChefApi))]
    public void Api_implementation_follows_configuration(string? baseUrl, Type expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = baseUrl })
            .Build();
        var services = new ServiceCollection().AddFridgeChef(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType(expected, provider.GetRequiredService<IFridgeChefApi>());
    }

    private static HttpFridgeChefApi Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7071/api/") });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body = "";
        private readonly string _mediaType = "application/json";
        private readonly Exception? _throw;

        public StubHandler(HttpStatusCode status, string body, string mediaType = "application/json") =>
            (_status, _body, _mediaType) = (status, body, mediaType);

        public StubHandler(Exception toThrow) => _throw = toThrow;

        public HttpRequestMessage? Request { get; private set; }
        public string? ContentType { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            ContentType = request.Content?.Headers.ContentType?.ToString();
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            if (_throw is not null)
            {
                throw _throw;
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, _mediaType) };
        }
    }
}
