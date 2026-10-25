using 'main.bicep'

param location = 'uksouth'

// UK South: UK West only offers text-only gpt-oss models, so no vision model can be deployed there.
// Global Standard: resource and data at rest in UK South; inference may run in any Azure region.
// Check what a region offers: az cognitiveservices model list --location uksouth -o table
param deployments = [
  {
    name: 'gpt-4.1-mini'
    format: 'OpenAI'
    model: 'gpt-4.1-mini'
    version: '2025-04-14'
    sku: 'GlobalStandard'
    capacity: 50
  }
  {
    // Newer reasoning model, offered in the app's model picker for comparison.
    name: 'gpt-5.4-mini'
    format: 'OpenAI'
    model: 'gpt-5.4-mini'
    version: '2026-03-17'
    sku: 'GlobalStandard'
    capacity: 20
  }
]

// Read from the environment so personal IDs and email stay out of the repo (deploy.sh sets them).
param developerPrincipalId = readEnvironmentVariable('DEVELOPER_PRINCIPAL_ID')
param budgetContactEmails = [readEnvironmentVariable('BUDGET_EMAIL')]
