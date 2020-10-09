# Deploy NumberService resources

$location = 'australiaeast'
$loc = 'aue'
$rg = 'numberservice-rg'
$tags = 'project=NumberService', 'repo=DanielLarsenNZ/NumberService'
$frontDoor = 'numberservice'
$cosmosAccount = 'numberservice'
$cosmosDB = 'NumberService'
$throughput = 400
$container='Numbers'
$pk = '/id'

# Create resource group
az group create -n $rg --location $location --tags $tags

<#
# FRONT DOOR
az network front-door create -n $frontDoor -g $rg --tags $tags `
    --backend-address "$app1.azurewebsites.net" `
    --accepted-protocols Http Https `
    --protocol Http
#>

# COSMOS DB ACCOUNT
az cosmosdb create -n $cosmosAccount -g $rg --default-consistency-level Session `
    --locations regionName=$location failoverPriority=0 isZoneRedundant=True

# COSMOS DB
az cosmosdb sql database create -a $cosmosAccount -g $rg -n $cosmosDB --throughput $throughput

# COSMOS CONTAINER
az cosmosdb sql container create -a $cosmosAccount -g $rg -d $cosmosDB -n $container -p $pk

# Tear down
# az group delete -n $rg --yes