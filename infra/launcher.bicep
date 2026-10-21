// The demo launcher (see the README section "Azure demo deployment"): an Azure Function that starts the
// one shared demo when the portfolio page opens and deletes it once nobody has opened the page for
// lifetimeMinutes, plus the narrow permissions and Azure Policy guardrails around it. Deploy it once,
// as the subscription's Owner, and again whenever it changes:
//   az deployment sub create -l eastus -f infra/launcher.bicep -p ...
targetScope = 'subscription'

@description('Web origins allowed to call the launcher from a browser, e.g. the portfolio site.')
param allowedOrigins array

@description('GitHub repository (owner/name) whose latest release holds the app and the demo template.')
param githubRepository string

@description('Name of the persistent Key Vault that holds the EventTicketing--* secrets.')
param keyVaultName string

@description('Public key for the demo VM\'s admin account. Azure requires one, but SSH stays closed.')
param adminSshPublicKey string

param location string = 'eastus'
param sharedResourceGroupName string = 'rg-eventticketing-shared'
param demosResourceGroupName string = 'rg-eventticketing-demos'
param demoIdentityName string = 'id-eventticketing-demo'
param dnsLabel string = 'eventticketing-demo'
param lifetimeMinutes int = 30

var demoVmSize = 'Standard_B2s'

resource sharedGroup 'Microsoft.Resources/resourceGroups@2024-03-01' existing = {
  name: sharedResourceGroupName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
  scope: sharedGroup
}

// The only place the launcher may create anything.
resource demosGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: demosResourceGroupName
  location: location
  tags: { app: 'event-ticketing' }
}

// Exactly what infra/demo-stack.bicep creates. No Microsoft.Authorization writes, so the launcher can't
// grant itself more or lift the policies on the demos group.
resource deployerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: guid(subscription().id, 'event-ticketing-demo-deployer')
  properties: {
    roleName: 'Event Ticketing demo deployer'
    description: 'Creates and deletes the Event Ticketing demo stack in its resource group.'
    type: 'CustomRole'
    assignableScopes: [demosGroup.id]
    permissions: [
      {
        actions: [
          'Microsoft.Resources/subscriptions/resourceGroups/read'
          'Microsoft.Resources/deploymentStacks/*'
          'Microsoft.Resources/deployments/*'
          'Microsoft.Compute/virtualMachines/*'
          'Microsoft.Compute/disks/*'
          'Microsoft.Network/networkSecurityGroups/*'
          'Microsoft.Network/virtualNetworks/*'
          'Microsoft.Network/publicIPAddresses/*'
          'Microsoft.Network/networkInterfaces/*'
          'Microsoft.Storage/storageAccounts/*'
        ]
        notActions: []
      }
    ]
  }
}

module app 'launcher/app.bicep' = {
  name: 'demo-launcher-app'
  scope: sharedGroup
  params: {
    location: location
    allowedOrigins: allowedOrigins
    demoIdentityName: demoIdentityName
    demoSettings: {
      SubscriptionId: subscription().subscriptionId
      ResourceGroup: demosGroup.name
      StackName: 'event-ticketing-demo'
      Location: location
      DnsLabel: dnsLabel
      TemplateUrl: 'https://github.com/${githubRepository}/releases/latest/download/demo-stack.json'
      ReleaseUrl: 'https://github.com/${githubRepository}/releases/latest/download/eventticketing-linux-x64.tar.gz'
      KeyVaultUri: keyVault.properties.vaultUri
      IdentityName: demoIdentityName
      IdentityResourceGroup: sharedResourceGroupName
      AdminSshPublicKey: adminSshPublicKey
      LifetimeMinutes: string(lifetimeMinutes)
    }
  }
}

module demos 'launcher/demos-group.bicep' = {
  name: 'demo-launcher-demos-group'
  scope: demosGroup
  params: {
    launcherPrincipalId: app.outputs.principalId
    deployerRoleId: deployerRole.id
    location: location
    vmSize: demoVmSize
  }
}

@description('The address the portfolio page calls: POST to start or extend the demo, GET for its status.')
output launcherUrl string = app.outputs.demoEndpoint

output functionAppName string = app.outputs.name
