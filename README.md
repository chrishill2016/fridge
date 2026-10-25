# Fridge Chef

A proof of concept for learning **Microsoft Foundry** with CSharp. You photograph your fridge or cupboard, a vision model lists the ingredients, and an agent suggests three recipes. Missing items go onto a shopping list, and you can save favourite recipes.

The app is a Blazor WebAssembly PWA (MudBlazor), hosted on GitHub Pages. Preferences, favourites and the shopping list live in IndexedDB on the device. AI calls go through an Azure Function, so no credential ever reaches the browser.

## Status

| Phase | What | State |
|---|---|---|
| 1 | Scaffold, PWA shell (dark mode, version, online/offline status, update prompt), CI/CD to Pages | ✅ |
| 2 | Local data: `ILocalStore` on IndexedDB; shopping list, favourites, preferences | ✅ |
| 3 | Foundry Bicep (resource, project, model deployment, budget) | ✅ |
| 3 | Azure Function + Foundry vision model: `POST /api/ingredients` | ✅ |
| 3 | Scan page: photo (resized in browser), analyse, editable ingredient list | ✅ |
| 4 | Recipes agent (structured output), shopping-list agent with `AddToShoppingList` tool, model picker | ✅ |
| 5 | MSAL sign-in (single-tenant, personal Microsoft account) + token validation and object-ID allow-list in the Function | ✅ |
| 6 | Bicep infra (UK South, Global Standard deployments) + Function deploy via OIDC | ⏳ |
| 7 | Foundry evaluations: compare prompt versions and models | ⏳ |

## Structure

```
src/FridgeChef/          Blazor WASM PWA
  Features/              Scan, Recipes (favourites), ShoppingList, Settings
  Layout/                Shell, status indicator, update prompt, footer
  Models/                Device-only records
  Services/              Storage (ILocalStore/IndexedDB), browser interop, feature services
src/FridgeChef.Api/      Azure Function (isolated worker), the only caller of Foundry
  Features/Ingredients/  POST /api/ingredients: photo in, ingredient list out
  Features/Recipes/      POST /api/recipes: ingredients + preferences in, 3 recipes out (agent, structured output)
  Features/ShoppingList/ POST /api/shopping-list: agent calls AddToShoppingList per item to buy
  Features/Models/       GET /api/models: deployments the app's model picker may offer
  Foundry/               Options, model allow-list, chat clients for the project
src/FridgeChef.Shared/   DTOs and limits shared by app and API
tests/FridgeChef.Tests/  xUnit v3 + bUnit
tests/FridgeChef.Api.Tests/  xUnit v3, Foundry mocked
infra/                   Bicep for the Azure resources
build/                   CI helper scripts
```

## Run locally

You need the .NET 10 SDK (`global.json` pins it).

```bash
dotnet run --project src/FridgeChef     # http://localhost:5199
dotnet test                             # uses Microsoft.Testing.Platform (opted in via global.json)
```

### API

You need [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local), [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (or a storage connection string), and the Foundry resources from **Azure setup** below.

```bash
cp src/FridgeChef.Api/local.settings.example.json src/FridgeChef.Api/local.settings.json
# set Foundry__ProjectEndpoint to the foundryProjectEndpoint output of infra/deploy.sh
az login                                    # DefaultAzureCredential uses this
cd src/FridgeChef.Api && func start         # http://localhost:7071/api/ingredients
```

Try it with a photo:

```bash
curl -F image=@fridge.jpg http://localhost:7071/api/ingredients
curl -F image=@fridge.jpg -F model=gpt-4.1-mini http://localhost:7071/api/ingredients
```

- **Limits:** photos must be JPEG, PNG or WebP up to 4 MB. The type is checked from the file's bytes, not its name.
- **Models:** `model` is optional and must be on the allow-list: `Foundry__IngredientsModel`, `Foundry__RecipesModel` and `Foundry__AllowedModels__0..n`.
- **Storage:** requests go to the Foundry project's Responses API with `store: false`, so photos aren't kept by the service.

**How the agents work.** Both are code-defined Microsoft Agent Framework `ChatClientAgent`s over the project's Responses API, so the model can change per request.
- **Recipes agent:** returns structured output, three recipes.
- **Shopping-list agent:** has one function tool, `AddToShoppingList`. The framework runs the loop: the model asks for the tool, the C# method runs, the result goes back, and this repeats until the model is done. The shopping list lives on the device, so the Function collects the calls and returns them, and the app saves them.
- **Fallback:** when offline, or if the agent fails, "Add missing" adds the items as listed.

### App

The app reads the API address from `wwwroot/appsettings*.json` (`Api:BaseUrl`):
- **Running locally:** `appsettings.Development.json` points at the local Function (`http://localhost:7071/api/`).
- **Empty address (GitHub Pages for now):** the app runs in **demo mode** and returns sample ingredients, so the flow can be tried before the API is deployed.

The dev service worker does no caching. To try offline mode and the update prompt, publish the app and serve `publish/wwwroot` from any static server.

## Deploy

`.github/workflows/ci-cd.yml`:
- **Pull request to `main`:** restore, build, test.
- **Push to `main`:** build and test, then publish and deploy to GitHub Pages.
  - The version is stamped as `1.0.<run number>` and shown in the footer.
  - `<base href>` is set to `/<repo-name>/`.
  - `index.html` is copied to `404.html`.
  - `.nojekyll` is added.

One-off setup: in **Settings → Pages**, set **Source** to **GitHub Actions**.

## Azure setup

### Foundry (phase 3)

`infra/main.bicep` deploys into a resource group (default `rg-fridge-chef`, UK South). UK West was the first choice, but it only offers text-only `gpt-oss` models, and the app needs vision.

| Resource | Notes |
|---|---|
| Foundry resource (`AIServices` account) | Key auth disabled, so only Entra ID callers |
| Foundry project | `fridgechef-project`, for agents, evaluations and tracing |
| Model deployments | Listed in `infra/main.bicepparam`: `gpt-4.1-mini` (default) and `gpt-5.4-mini` (offered in the model picker), both Global Standard. |
| Role assignment | **Azure AI User** for you, so local dev works via `az login` |
| Budget | £10/month with emails at 50%, 90% and forecast 100% (`budgetAmount` to change) |

Prerequisites:
- Azure CLI, logged in with `az login`.
- Owner on the subscription, or Contributor + User Access Administrator. The deployment creates a role assignment.
- Check the models are offered as Global Standard in the region: `az cognitiveservices model list --location uksouth -o table`.

```bash
infra/deploy.sh you@example.com              # optional: [resource-group] [location]
```

The output includes `foundryProjectEndpoint`, which the Function uses in the next step.

**Adding a model:** add an entry to `deployments` in `infra/main.bicepparam` and run the script again. Re-runs only change what differs.

**Stopping all cost:** `az group delete -n rg-fridge-chef`. The Foundry resource is then soft-deleted; purge it with `az cognitiveservices account purge` if you want to reuse the name straight away.

### Sign-in (phase 5)

How the pieces fit:
- **App:** signs in with MSAL against a **single-tenant** app registration in your directory, using your personal Microsoft account, which is a member of that directory.
- **API calls:** each carries an access token for the `access_as_user` scope.
- **Function:** middleware checks the token on every HTTP function before it runs:
  - signed by your tenant
  - audience is this app
  - carries the scope
  - `oid` is on `Auth__AllowedObjectIds`

  Anything else gets 401 or 403.

Create the app registration once, from Cloud Shell (Bash). Re-running it is safe:

```bash
infra/register-app.sh            # optional: [pages-url] [display-name]
```

It creates or updates the registration and prints the client ID and the Function settings:
- **Redirect URIs** for SPA sign-in: Pages, `localhost:5199`, `localhost:7290`.
- **API identifier:** `api://<client-id>`, with the `access_as_user` scope.
- **v2 tokens.**
- **No consent prompt:** the app is pre-authorised for its own scope.

Then:
- **App:** set `AzureAd:ClientId` in `src/FridgeChef/wwwroot/appsettings.json`. While it's empty, sign-in is off.
- **Function:** set `Auth__TenantId`, `Auth__ClientId` and `Auth__AllowedObjectIds__0` (your object ID) in `local.settings.json`.
- **Local testing before the registration exists:** `Auth__AllowAnonymousInDevelopment=true` skips the check. It only takes effect when `func start` runs in Development; in Azure it's ignored.

### Function and pipeline (phase 6)

To follow. The Function will use `DefaultAzureCredential`: `az login` locally, managed identity in Azure. No keys anywhere.
