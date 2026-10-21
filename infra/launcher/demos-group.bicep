// The demos resource group's rules: the launcher may build the demo here and nowhere else, and Azure
// Policy refuses anything the demo doesn't need, whoever asks.

param launcherPrincipalId string
param deployerRoleId string
param location string
param vmSize string

resource deployer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, launcherPrincipalId, deployerRoleId)
  properties: {
    roleDefinitionId: deployerRoleId
    principalId: launcherPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Built-in policy definitions; each denies by default.
var guardrails = [
  {
    name: 'allowed-vm-sizes'
    displayName: 'Demo VMs: ${vmSize} only'
    definition: 'cccc23c7-8427-4f53-ad12-b6a63eb452b3'
    parameters: { listOfAllowedSKUs: { value: [vmSize] } }
  }
  {
    name: 'allowed-locations'
    displayName: 'Demos: ${location} only'
    definition: 'e56962a6-4747-49cd-b67b-bf8b01975c4c'
    parameters: { listOfAllowedLocations: { value: [location] } }
  }
  {
    name: 'allowed-resource-types'
    displayName: 'Demos: only what infra/demo-stack.bicep creates'
    definition: 'a08ec900-254a-4555-9bf5-e42af04b5c5c'
    parameters: {
      listOfResourceTypesAllowed: {
        value: [
          'Microsoft.Resources/deploymentStacks'
          'Microsoft.Compute/virtualMachines'
          'Microsoft.Compute/disks'
          'Microsoft.Network/networkSecurityGroups'
          'Microsoft.Network/virtualNetworks'
          'Microsoft.Network/publicIPAddresses'
          'Microsoft.Network/networkInterfaces'
          'Microsoft.Storage/storageAccounts'
        ]
      }
    }
  }
  {
    name: 'allowed-storage-skus'
    displayName: 'Demo storage: Standard_LRS only'
    definition: '7433c107-6db4-4ad1-b57a-a76dce0154a1'
    parameters: { listOfAllowedSKUs: { value: ['Standard_LRS'] } }
  }
]

resource policies 'Microsoft.Authorization/policyAssignments@2024-04-01' = [
  for guardrail in guardrails: {
    name: guardrail.name
    properties: {
      displayName: guardrail.displayName
      policyDefinitionId: tenantResourceId('Microsoft.Authorization/policyDefinitions', guardrail.definition)
      parameters: guardrail.parameters
    }
  }
]
