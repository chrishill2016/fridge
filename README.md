# Fridge Chef

A proof of concept for learning **Microsoft Foundry** with CSharp. You photograph your fridge or cupboard, a vision model lists the ingredients, and an agent suggests three recipes. Missing items go onto a shopping list, and you can save favourite recipes.

The app is a Blazor WebAssembly PWA (MudBlazor), hosted on GitHub Pages. Preferences, favourites and the shopping list live in IndexedDB on the device. AI calls go through an Azure Function, so no credential ever reaches the browser.

## Status

| Phase | What | State |
|---|---|---|
| 1 | Scaffold, PWA shell (dark mode, version, online/offline status, update prompt), CI/CD to Pages | ✅ |
| 2 | Local data: `ILocalStore` on IndexedDB; shopping list, favourites, preferences | ✅ |
| 3 | Foundry Bicep (resource, project, model deployment, budget) | ✅ |
| 3 | Azure Function + Foundry vision model: `POST /api/ingredients` | ⏳ |
| 4 | Recipes prompt agent with `AddToShoppingList` tool, in-app model picker | ⏳ |
| 5 | MSAL sign-in (personal Microsoft account) + object-ID allow-list in the Function | ⏳ |
| 6 | Bicep infra (UK West, Global Standard deployments) + Function deploy via OIDC | ⏳ |
| 7 | Foundry evaluations: compare prompt versions and models | ⏳ |

## Structure

```
src/FridgeChef/          Blazor WASM PWA
  Features/              Scan, Recipes (favourites), ShoppingList, Settings
  Layout/                Shell, status indicator, update prompt, footer
  Models/                Device-only records
  Services/              Storage (ILocalStore/IndexedDB), browser interop, feature services
src/FridgeChef.Shared/   DTOs shared with the API
tests/FridgeChef.Tests/  xUnit v3 + bUnit
infra/                   Bicep for the Azure resources
build/                   CI helper scripts
```

## Run locally

You need the .NET 10 SDK (`global.json` pins it).

```bash
dotnet run --project src/FridgeChef     # http://localhost:5199
dotnet test                             # uses Microsoft.Testing.Platform (opted in via global.json)
```

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

`infra/main.bicep` deploys into a resource group (default `rg-fridge-chef`, UK West):

| Resource | Notes |
|---|---|
| Foundry resource (`AIServices` account) | Key auth disabled, so only Entra ID callers |
| Foundry project | `fridgechef-project`, for agents, evaluations and tracing |
| Model deployments | Listed in `infra/main.bicepparam`. Starts with `gpt-4.1-mini` on Global Standard. |
| Role assignment | **Azure AI User** for you, so local dev works via `az login` |
| Budget | £10/month with emails at 50%, 90% and forecast 100% (`budgetAmount` to change) |

Prerequisites:
- Azure CLI, logged in with `az login`.
- Owner on the subscription, or Contributor + User Access Administrator. The deployment creates a role assignment.
- Check the model is offered as Global Standard in UK West: Foundry portal → Models → filter by region.

```bash
infra/deploy.sh you@example.com              # optional: [resource-group] [location]
```

The output includes `foundryProjectEndpoint`, which the Function uses in the next step.

**Adding a model:** add an entry to `deployments` in `infra/main.bicepparam` and run the script again. Re-runs only change what differs.

**Stopping all cost:** `az group delete -n rg-fridge-chef`. The Foundry resource is then soft-deleted; purge it with `az cognitiveservices account purge` if you want to reuse the name straight away.

### Function, auth and pipeline (phases 4–6)

To follow. The Function will use `DefaultAzureCredential`: `az login` locally, managed identity in Azure. No keys anywhere.
