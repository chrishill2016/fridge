using FridgeChef.Layout;
using FridgeChef.Services.Api;
using FridgeChef.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Components;

public class AuthUiTests : AppTestContext
{
    [Fact]
    public void Login_display_is_hidden_when_sign_in_is_not_configured()
    {
        var cut = Render<LoginDisplay>();

        Assert.Empty(cut.FindAll("[data-testid=sign-in]"));
        Assert.Empty(cut.FindAll("[data-testid=account-menu]"));
    }

    [Fact]
    public void Signed_out_user_sees_sign_in_which_starts_login()
    {
        EnableAuth();

        var cut = Render<LoginDisplay>();
        cut.Find("[data-testid=sign-in]").Click();

        Assert.Contains("authentication/login", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Signed_in_user_sees_account_menu()
    {
        EnableAuth();
        Auth.SetAuthorized("Chris");

        var cut = Render<LoginDisplay>();

        cut.Find("[data-testid=account-menu]");
        Assert.Empty(cut.FindAll("[data-testid=sign-in]"));
    }

    [Fact]
    public void Error_needing_sign_in_offers_sign_in_button()
    {
        var cut = Render<ApiErrorAlert>(p => p.Add(c => c.Error, new ApiException("Sign in to use the AI features.", requiresSignIn: true)));

        cut.Find("[data-testid=error-sign-in]").Click();

        Assert.Contains("authentication/login", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Other_errors_have_no_sign_in_button()
    {
        var cut = Render<ApiErrorAlert>(p => p.Add(c => c.Error, new ApiException("Try again.")));

        Assert.Empty(cut.FindAll("[data-testid=error-sign-in]"));
    }

    private void EnableAuth() => AuthConfig = new AuthSettings(true, "api://client/access_as_user");
}
