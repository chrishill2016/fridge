// Fridge Chef infrastructure, deployed into an existing resource group.
// Foundry (models + project), the API Function App with its storage and monitoring, and a budget.
targetScope = 'resourceGroup'

import { deploymentConfig, principalConfig } from 'modules/foundry.bicep'

param location string = resourceGroup().location

@description('Short lowercase prefix for resource names.')
@minLength(3)
@maxLength(12)
param namePrefix string = 'fridgechef'

param deployments deploymentConfig[]

@description('Object ID of the developer running the app locally (az ad signed-in-user show --query id -o tsv).')
@minLength(36)
param developerPrincipalId string

@description('Client ID of the Entra app registration (infra/register-app.sh). Empty skips the Function App.')
param authClientId string = ''

@description('Origins allowed to call the API from a browser (the GitHub Pages site).')
param corsOrigins string[] = []

param budgetAmount int = 10
@minLength(1)
param budgetContactEmails string[]

@description('Leave as the default; budgets need the first day of a month.')
param budgetStartDate string = '${utcNow('yyyy-MM')}-01'

var suffix = uniqueString(resourceGroup().id)
var tags = {
  app: 'fridge-chef'
}

var developer principalConfig = {
  principalId: developerPrincipalId
  principalType: 'User'
}

module foundry 'modules/foundry.bicep' = {
  name: 'foundry'
  params: {
    location: location
    accountName: '${namePrefix}-ai-${suffix}'
    projectName: '${namePrefix}-project'
    deployments: deployments
    aiUsers: [developer]
    tags: tags
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    name: namePrefix
    tags: tags
  }
}

module api 'modules/function.bicep' = if (!empty(authClientId)) {
  name: 'api'
  params: {
    location: location
    name: '${namePrefix}-api-${suffix}'
    // Storage names: 3-24 lowercase letters and digits.
    storageAccountName: take(toLower(replace('${namePrefix}st${suffix}', '-', '')), 24)
    tags: tags
    foundryAccountName: foundry.outputs.accountName
    foundryProjectEndpoint: foundry.outputs.projectEndpoint
    ingredientsModel: deployments[0].name
    recipesModel: deployments[0].name
    extraModels: [for (d, i) in deployments: d.name]
    authClientId: authClientId
    // The developer is the one person allowed to use the API.
    allowedObjectIds: [developerPrincipalId]
    corsOrigins: corsOrigins
    appInsightsConnectionString: monitoring.outputs.connectionString
  }
}

module budget 'modules/budget.bicep' = {
  name: 'budget'
  params: {
    name: '${namePrefix}-monthly'
    amount: budgetAmount
    contactEmails: budgetContactEmails
    startDate: budgetStartDate
  }
}

output foundryAccountName string = foundry.outputs.accountName
output foundryProjectEndpoint string = foundry.outputs.projectEndpoint
output modelDeployments string[] = foundry.outputs.deploymentNames
output functionAppName string = api.?outputs.name ?? ''
output apiBaseUrl string = api.?outputs.baseUrl ?? ''
