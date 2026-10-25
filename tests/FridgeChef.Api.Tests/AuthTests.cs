using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using FridgeChef.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FridgeChef.Api.Tests;

public class AuthTests
{
    private const string Tenant = "c0ffee00-0000-0000-0000-000000000001";
    private const string ClientId = "c0ffee00-0000-0000-0000-000000000002";
    private const string Allowed = "c0ffee00-0000-0000-0000-0000000000aa";
    private const string Stranger = "c0ffee00-0000-0000-0000-0000000000bb";

    private static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };
    private static readonly RsaSecurityKey OtherKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    [Fact]
    public async Task Valid_token_from_allowed_user_passes()
    {
        var result = await Validator().ValidateAsync(Bearer(Token()), TestContext.Current.CancellationToken);

        Assert.True(result.Allowed);
        Assert.Equal(Allowed, result.ObjectId);
    }

    [Fact]
    public async Task V1_token_issuer_and_audience_are_also_accepted()
    {
        var token = Token(issuer: $"https://sts.windows.net/{Tenant}/", audience: $"api://{ClientId}");

        Assert.True((await Validator().ValidateAsync(Bearer(token), TestContext.Current.CancellationToken)).Allowed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic dXNlcjpwYXNz")]
    [InlineData("Bearer not-a-jwt")]
    public async Task Missing_or_malformed_token_is_401(string? header) =>
        Assert.Equal(401, (await Validator().ValidateAsync(header, TestContext.Current.CancellationToken)).Status);

    public static TheoryData<string, string> BadTokens => new()
    {
        { "wrong tenant", Token(issuer: "https://login.microsoftonline.com/someone-else/v2.0") },
        { "wrong audience", Token(audience: "another-app") },
        { "expired", Token(expires: DateTime.UtcNow.AddMinutes(-10)) },
        { "wrong signing key", Token(key: OtherKey) },
        { "unsigned", new JsonWebTokenHandler().CreateToken(Descriptor(Allowed, "access_as_user", $"https://login.microsoftonline.com/{Tenant}/v2.0", ClientId, DateTime.UtcNow.AddHours(1), null)) },
    };

    [Theory]
    [MemberData(nameof(BadTokens))]
    public async Task Invalid_token_is_401(string because, string token)
    {
        var result = await Validator().ValidateAsync(Bearer(token), TestContext.Current.CancellationToken);

        Assert.False(result.Allowed, because);
        Assert.Equal(401, result.Status);
    }

    [Fact]
    public async Task Token_without_api_scope_is_403()
    {
        var result = await Validator().ValidateAsync(Bearer(Token(scope: "User.Read")), TestContext.Current.CancellationToken);

        Assert.Equal(403, result.Status);
        Assert.Contains("access to this API", result.Problem);
    }

    [Fact]
    public async Task Someone_not_on_the_allow_list_is_403()
    {
        var result = await Validator().ValidateAsync(Bearer(Token(objectId: Stranger)), TestContext.Current.CancellationToken);

        Assert.Equal(403, result.Status);
        Assert.Equal(Stranger, result.ObjectId);
    }

    [Fact]
    public async Task Empty_allow_list_lets_nobody_in() =>
        Assert.Equal(403, (await Validator(allowed: []).ValidateAsync(Bearer(Token()), TestContext.Current.CancellationToken)).Status);

    [Fact]
    public async Task Anonymous_switch_only_works_in_development()
    {
        Assert.True((await Validator(allowAnonymous: true, environment: Environments.Development)
            .ValidateAsync(null, TestContext.Current.CancellationToken)).Allowed);

        Assert.Equal(401, (await Validator(allowAnonymous: true, environment: Environments.Production)
            .ValidateAsync(null, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task Middleware_stops_refused_requests_with_problem_json()
    {
        var (context, http) = FunctionContextFor(header: null);
        var nextCalled = false;

        await new AuthMiddleware(Validator()).Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        Assert.Equal(401, http.Response.StatusCode);
        Assert.Equal("Bearer", http.Response.Headers.WWWAuthenticate.ToString());
        http.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(http.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("Sign in", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Middleware_lets_allowed_requests_through()
    {
        var (context, _) = FunctionContextFor(Bearer(Token()));
        var nextCalled = false;

        await new AuthMiddleware(Validator()).Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public void Middleware_applies_to_every_http_function()
    {
        var httpFunctions = typeof(AuthMiddleware).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(FunctionAttribute), false).Length > 0)
            .Where(m => m.GetParameters().Any(p => p.GetCustomAttributes(typeof(HttpTriggerAttribute), false).Length > 0))
            .Select(m => m.DeclaringType!.Name)
            .ToList();

        Assert.Equal(["IngredientsFunction", "ModelsFunction", "RecipesFunction", "ShoppingListFunction"], httpFunctions.Order());
        Assert.True(AuthMiddleware.AppliesTo(FunctionContextFor(null).Context));
    }

    private static AccessTokenValidator Validator(string[]? allowed = null, bool allowAnonymous = false, string environment = "Production") => new(
        Options.Create(new AuthOptions
        {
            TenantId = Tenant,
            ClientId = ClientId,
            AllowedObjectIds = allowed ?? [Allowed],
            AllowAnonymousInDevelopment = allowAnonymous,
        }),
        new StaticConfigurationManager<OpenIdConnectConfiguration>(Signing()),
        new TestHostEnvironment(environment),
        NullLogger<AccessTokenValidator>.Instance);

    private static OpenIdConnectConfiguration Signing()
    {
        var config = new OpenIdConnectConfiguration();
        config.SigningKeys.Add(SigningKey);
        return config;
    }

    private static string Bearer(string token) => "Bearer " + token;

    private static string Token(
        string objectId = Allowed,
        string scope = "access_as_user",
        string issuer = $"https://login.microsoftonline.com/{Tenant}/v2.0",
        string audience = ClientId,
        DateTime? expires = null,
        SecurityKey? key = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddHours(1);
        return new JsonWebTokenHandler().CreateToken(Descriptor(
            objectId, scope, issuer, audience, expiry,
            new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.RsaSha256)));
    }

    private static SecurityTokenDescriptor Descriptor(string objectId, string scope, string issuer, string audience, DateTime expires, SigningCredentials? credentials) => new()
    {
        Issuer = issuer,
        Audience = audience,
        NotBefore = expires.AddHours(-2),
        IssuedAt = expires.AddHours(-2),
        Expires = expires,
        Claims = new Dictionary<string, object> { ["oid"] = objectId, ["scp"] = scope, ["name"] = "Test User" },
        SigningCredentials = credentials,
    };

    private static (FunctionContext Context, HttpContext Http) FunctionContextFor(string? header)
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        if (header is not null)
        {
            http.Request.Headers.Authorization = header;
        }

        return (new FakeFunctionContext(http), http);
    }

    private sealed class TestHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "FridgeChef.Api";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>Just enough FunctionContext for the middleware: an HTTP trigger binding and the HttpContext.</summary>
    private sealed class FakeFunctionContext : FunctionContext
    {
        public FakeFunctionContext(HttpContext http) => Items["HttpRequestContext"] = http;

        public override string InvocationId => "test";
        public override string FunctionId => "test";
        public override TraceContext TraceContext => null!;
        public override BindingContext BindingContext => null!;
        public override RetryContext RetryContext => null!;
        public override IServiceProvider InstanceServices { get; set; } = null!;
        public override FunctionDefinition FunctionDefinition { get; } = new FakeDefinition();
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        public override IInvocationFeatures Features => null!;
    }

    private sealed class FakeDefinition : FunctionDefinition
    {
        public override ImmutableArray<FunctionParameter> Parameters => [];
        public override string PathToAssembly => "";
        public override string EntryPoint => "";
        public override string Id => "test";
        public override string Name => "test";
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } =
            ImmutableDictionary<string, BindingMetadata>.Empty.Add("req", new FakeBinding("httpTrigger"));
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    }

    private sealed class FakeBinding(string type) : BindingMetadata
    {
        public override string Name => "req";
        public override string Type => type;
        public override BindingDirection Direction => BindingDirection.In;
    }
}
