# Deploy NumberService resources
$ErrorActionPreference = 'Stop'
. ./_vars.ps1

$throughput = 400
$pk = '/id'
$primaryCosmosLocation = 'australiaeast'
$secondaryCosmosLocation = 'australiasoutheast'


# RESOURCE GROUP
az group create -n $rg --location $location --tags $tags


# COSMOS DB ACCOUNT
az cosmosdb create -n $cosmos -g $rg --default-consistency-level Session `
    --locations regionName=$primaryCosmosLocation failoverPriority=0 isZoneRedundant=True `
    --locations regionName=$secondaryCosmosLocation failoverPriority=1 isZoneRedundant=False `
    --enable-automatic-failover $true `
    --enable-multiple-write-locations $true
az cosmosdb sql database create -a $cosmos -g $rg -n $cosmosDB --throughput $throughput
az cosmosdb sql container create -a $cosmos -g $rg -d $cosmosDB -n $container -p $pk --conflict-resolution-policy @conflict-policy.json
