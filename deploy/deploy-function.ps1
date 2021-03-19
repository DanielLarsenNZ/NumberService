[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)] [ValidateSet('australiaeast', 'australiasoutheast')] $FunctionLocation
)

. ./_vars.ps1

$loc = switch ($FunctionLocation) {
    'australiaeast' { 'aue' }
    'australiasoutheast' { 'ase' }
    default { throw "$FunctionLocation is not supported" }
}

$functionApp = "numberservice-$loc"
$repo = 'https://github.com/DanielLarsenNZ/NumberService.git'
$storage = "numservfn$loc"

# STORAGE ACCOUNT
az storage account create -n $storage -g $rg --tags $tags --location $FunctionLocation --sku 'Standard_LRS'


# FUNCTION APP
az functionapp create -n $functionApp -g $rg --consumption-plan-location $FunctionLocation --functions-version 3 `
    --app-insights $insights --app-insights-key $env:NUMBERS_APP_INSIGHTS_IKEY -s $storage
az functionapp config appsettings set -n $functionApp -g $rg --settings `
    "CosmosDbConnectionString=$env:NUMBERS_COSMOS_CONNSTRING" `
    "CosmosDbDatabaseId=$cosmosDB" `
    "CosmosDbContainerId=$container"
az functionapp deployment source config -n $functionApp -g $rg --repo-url $repo --branch 'main'
