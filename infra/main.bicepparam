using 'main.bicep'

param location = 'ukwest'

// Global Standard: resource and data at rest in UK West; inference may run in any Azure region.
param deployments = [
  {
    name: 'gpt-4.1-mini'
    format: 'OpenAI'
    model: 'gpt-4.1-mini'
    version: '2025-04-14'
    sku: 'GlobalStandard'
    capacity: 50
  }
]

// Read from the environment so personal IDs and email stay out of the repo (deploy.sh sets them).
param developerPrincipalId = readEnvironmentVariable('DEVELOPER_PRINCIPAL_ID')
param budgetContactEmails = [readEnvironmentVariable('BUDGET_EMAIL')]
