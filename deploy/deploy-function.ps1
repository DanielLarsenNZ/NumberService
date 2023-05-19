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
$repo = 'https://github.com/DanielLarsenNZ/NumberService.git'
$storage = "numservfn$loc"

# STORAGE ACCOUNT
az storage account create -n $storage -g $rg --tags $tags --location $FunctionLocation --sku 'Standard_LRS'


# FUNCTION APP
az functionapp create -n $functionApp -g $rg --consumption-plan-location $FunctionLocation --functions-version 4 `
    --app-insights $insights --app-insights-key $env:NUMBERS_APP_INSIGHTS_IKEY -s $storage
az functionapp config appsettings set -n $functionApp -g $rg --settings `
    "CosmosDbConnectionString=$env:NUMBERS_COSMOS_CONNSTRING" `
    "CosmosDbDatabaseId=$cosmosDB" `
    "CosmosDbContainerId=$container" `
    "CosmosApplicationPreferredRegions=$FunctionLocation"

#TODO: SCM deployment with basic auth has been deprecated. Enhance to use GH Actions instead.
    #az functionapp deployment source config -n $functionApp -g $rg --repo-url $repo --branch 'main'
