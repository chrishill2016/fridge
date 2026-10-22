# Fridge Chef

A proof of concept for learning **Microsoft Foundry** with CSharp. You photograph your fridge or cupboard, a vision model lists the ingredients, and an agent suggests three recipes. Missing items go onto a shopping list, and you can save favourite recipes.

The app is a Blazor WebAssembly PWA (MudBlazor), hosted on GitHub Pages. Preferences, favourites and the shopping list live in IndexedDB on the device. AI calls go through an Azure Function, so no credential ever reaches the browser.

## Status

| Phase | What | State |
|---|---|---|
| 1 | Scaffold, PWA shell (dark mode, version, online/offline status, update prompt), CI/CD to Pages | ✅ |
| 2 | Local data: `ILocalStore` on IndexedDB; shopping list, favourites, preferences | ✅ |
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

Covered in phases 3–6. The Function will use `DefaultAzureCredential`: `az login` locally, managed identity in Azure. No keys anywhere.
