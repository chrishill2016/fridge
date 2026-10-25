using System.Net;
using System.Text;
using FridgeChef.Services.Api;
using FridgeChef.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
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

        var result = await sut.AnalysePhotoAsync(Photo, "phi-4", TestContext.Current.CancellationToken);

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

        await Client(handler).AnalysePhotoAsync(Photo, cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain("name=model", handler.Body);
    }

    [Fact]
    public async Task Problem_detail_becomes_the_error_message()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"status":400,"detail":"The photo must be a JPEG, PNG or WebP image."}""", "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("The photo must be a JPEG, PNG or WebP image.", ex.Message);
    }

    [Fact]
    public async Task Non_json_error_gets_generic_message_with_status()
    {
        var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, "<html>down</html>", "text/html");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("503", ex.Message);
    }

    [Fact]
    public async Task Network_failure_becomes_friendly_error()
    {
        var handler = new StubHandler(new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).AnalysePhotoAsync(Photo, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Couldn't reach", ex.Message);
    }

    [Fact]
    public async Task Recipes_shopping_and_models_use_json_endpoints()
    {
        var recipes = new StubHandler(HttpStatusCode.OK, """{"recipes":[{"title":"Omelette","timeMinutes":10,"ingredients":["eggs"],"missing":[],"steps":["Cook"]}],"run":{"model":"m","promptVersion":"v"}}""");
        var result = await Client(recipes).SuggestRecipesAsync(new(["eggs"], new(["olives"], true, 3), "phi-4"), TestContext.Current.CancellationToken);
        Assert.Equal("http://localhost:7071/api/recipes", recipes.Request!.RequestUri!.ToString());
        Assert.StartsWith("application/json", recipes.ContentType);
        Assert.Contains("\"dislikes\":[\"olives\"]", recipes.Body);
        Assert.Contains("\"quickWeeknight\":true", recipes.Body);
        Assert.Equal("Omelette", Assert.Single(result.Recipes).Title);

        var shopping = new StubHandler(HttpStatusCode.OK, """{"items":[{"name":"cream","quantity":"300ml"}],"run":{"model":"m","promptVersion":"v"}}""");
        var plan = await Client(shopping).PlanShoppingAsync(new("Pasta", ["cream"], ["milk"]), TestContext.Current.CancellationToken);
        Assert.Equal("http://localhost:7071/api/shopping-list", shopping.Request!.RequestUri!.ToString());
        Assert.Contains("\"alreadyOnList\":[\"milk\"]", shopping.Body);
        Assert.Equal("300ml", Assert.Single(plan.Items).Quantity);

        var models = new StubHandler(HttpStatusCode.OK, """{"models":["a","b"],"ingredientsDefault":"a","recipesDefault":"b"}""");
        var list = await Client(models).GetModelsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpMethod.Get, models.Request!.Method);
        Assert.Equal(["a", "b"], list.Models);
    }

    [Fact]
    public async Task Demo_api_recipes_and_shopping_list()
    {
        var time = new ManualTimeProvider();
        var sut = new DemoFridgeChefApi(time);

        var recipesTask = sut.SuggestRecipesAsync(new(["eggs"], new([], QuickWeeknight: true)), TestContext.Current.CancellationToken);
        time.Advance(DemoFridgeChefApi.Delay);
        var recipes = await recipesTask;
        Assert.Equal(3, recipes.Recipes.Count);
        Assert.All(recipes.Recipes, r => Assert.True(r.TimeMinutes <= 30));

        var planTask = sut.PlanShoppingAsync(new("Pasta", ["pasta", "garlic"], ["Garlic"]), TestContext.Current.CancellationToken);
        time.Advance(DemoFridgeChefApi.Delay);
        Assert.Equal(["pasta"], (await planTask).Items.Select(i => i.Name));

        Assert.Equal(["demo"], (await sut.GetModelsAsync(TestContext.Current.CancellationToken)).Models);
    }

    [Fact]
    public async Task Unauthorised_response_asks_for_sign_in()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, """{"status":401,"detail":"Sign in to use Fridge Chef's AI features."}""", "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).GetModelsAsync(TestContext.Current.CancellationToken));

        Assert.True(ex.RequiresSignIn);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public async Task Forbidden_response_does_not_ask_for_sign_in()
    {
        var handler = new StubHandler(HttpStatusCode.Forbidden, """{"status":403,"detail":"This account isn't allowed to use Fridge Chef."}""", "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(handler).GetModelsAsync(TestContext.Current.CancellationToken));

        Assert.False(ex.RequiresSignIn);
        Assert.Contains("isn't allowed", ex.Message);
    }

    [Fact]
    public async Task Missing_token_asks_for_sign_in()
    {
        var noToken = new AccessTokenNotAvailableException(
            new TestNavigationManager(),
            new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null!, "authentication/login", null),
            ["api://client/access_as_user"]);

        var ex = await Assert.ThrowsAsync<ApiException>(() => Client(new StubHandler(noToken)).GetModelsAsync(TestContext.Current.CancellationToken));

        Assert.True(ex.RequiresSignIn);
    }

    [Theory]
    [InlineData(null, null, false, null)]
    [InlineData("", "", false, null)]
    [InlineData("abc", null, true, "api://abc/access_as_user")]
    [InlineData("abc", "api://custom/scope", true, "api://custom/scope")]
    public void Auth_settings_follow_configuration(string? clientId, string? scope, bool enabled, string? expectedScope)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AzureAd:ClientId"] = clientId, ["Api:Scope"] = scope })
            .Build();

        var settings = AuthSettings.From(configuration);

        Assert.Equal(enabled, settings.Enabled);
        Assert.Equal(expectedScope, settings.ApiScope);
    }

    [Fact]
    public void Without_client_id_the_app_runs_anonymously()
    {
        var services = new ServiceCollection().AddFridgeChef(new ConfigurationBuilder().Build());

        Assert.Contains(services, d => d.ServiceType == typeof(AuthenticationStateProvider) && d.ImplementationType == typeof(AnonymousAuthenticationStateProvider));
    }

    [Fact]
    public void With_client_id_msal_sign_in_is_registered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Authority"] = "https://login.microsoftonline.com/tenant",
                ["AzureAd:ClientId"] = "abc",
                ["Api:BaseUrl"] = "https://api.example/api/",
            })
            .Build();

        var services = new ServiceCollection().AddFridgeChef(configuration);

        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AnonymousAuthenticationStateProvider));
        Assert.Contains(services, d => d.ServiceType == typeof(IAccessTokenProvider));
        Assert.Equal("api://abc/access_as_user", Assert.Single(services, d => d.ServiceType == typeof(AuthSettings)).ImplementationInstance is AuthSettings a ? a.ApiScope : null);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://app.example/", "https://app.example/");
    }

    [Fact]
    public async Task Demo_api_returns_sample_data_after_a_short_delay()
    {
        var time = new ManualTimeProvider();
        var sut = new DemoFridgeChefApi(time);

        var task = sut.AnalysePhotoAsync(Photo, cancellationToken: TestContext.Current.CancellationToken);
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
