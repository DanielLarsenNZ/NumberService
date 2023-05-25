$DeployCosmos = $true

# Deploy NumberService resources
$ErrorActionPreference = 'Stop'
. ./_vars.ps1

$throughput = 400
$pk = '/id'
$primaryCosmosLocation = 'australiaeast'
$secondaryCosmosLocation = 'australiasoutheast'


# RESOURCE GROUP
$rgId = ( az group create -n $rg --location $location --tags $tags | ConvertFrom-Json ).id
$rgId


Write-Host "Copy the RBAC JSON below and paste into a GitHub Action Repository secret named AZURE_RBAC_CREDENTIALS."
# Note that this service principal is scoped to the resource group with Contributor access
az ad sp create-for-rbac --name "$rg-sp" --role contributor --scopes $rgId --sdk-auth


if ($DeployCosmos) {
    # COSMOS DB ACCOUNT
    az cosmosdb create -n $cosmos -g $rg --default-consistency-level Session `
        --locations regionName=$primaryCosmosLocation failoverPriority=0 isZoneRedundant=True `
        --locations regionName=$secondaryCosmosLocation failoverPriority=1 isZoneRedundant=False `
        --enable-automatic-failover $true `
        --enable-multiple-write-locations $true
    az cosmosdb sql database create -a $cosmos -g $rg -n $cosmosDB --throughput $throughput
    az cosmosdb sql container create -a $cosmos -g $rg -d $cosmosDB -n $container -p $pk  --conflict-resolution-policy @conflict-policy.json
}
