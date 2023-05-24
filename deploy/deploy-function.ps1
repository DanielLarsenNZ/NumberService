[CmdletBinding()]
param (
    # TODO: Add more regions here
    [Parameter(Mandatory = $true)] [ValidateSet('australiaeast', 'australiasoutheast')] $FunctionLocation
)

$ErrorActionPreference = 'Stop'

. ./_vars.ps1

# TODO: Add more regions here
$loc = switch ($FunctionLocation) {
    'australiaeast' { 'aue' }
    'australiasoutheast' { 'ase' }
    default { throw "$FunctionLocation is not supported" }
}

$functionApp = "numberservice-$loc"
$storage = "numservfn$loc"

# STORAGE ACCOUNT
az storage account create -n $storage -g $rg --tags $tags --location $FunctionLocation --sku 'Standard_LRS'

# Get cosmos and application insights keys
$cosmosConnString = ( az cosmosdb keys list -n $cosmos -g $rg --type 'connection-strings' | ConvertFrom-Json ).connectionStrings[0].connectionString
$insightsKey = ( az monitor app-insights component show -a $insights -g $rg | ConvertFrom-Json ).instrumentationKey


# FUNCTION APP
az functionapp create -n $functionApp -g $rg --consumption-plan-location $FunctionLocation --functions-version 4 `
    --app-insights $insights --app-insights-key $insightsKey -s $storage
az functionapp config appsettings set -n $functionApp -g $rg --settings `
    "CosmosDbConnectionString=$cosmosConnString" `
    "CosmosDbDatabaseId=$cosmosDB" `
    "CosmosDbContainerId=$container" `
    "CosmosApplicationPreferredRegions=$FunctionLocation"
