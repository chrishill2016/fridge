// Fridge Chef infrastructure, deployed into an existing resource group.
// Phase 3: Foundry + budget. Phase 6 adds the Function App, storage and monitoring.
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
