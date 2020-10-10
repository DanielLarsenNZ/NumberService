# Deploy NumberService resources

$ErrorActionPreference = 'Stop'

$location = 'australiaeast'
$loc = 'aue'
$rg = 'numberservice-rg'
$tags = 'project=NumberService', 'repo=DanielLarsenNZ/NumberService'
$cosmos = 'numberservice'
$cosmosDB = 'NumberService'
$throughput = 400
$container='Numbers'
$pk = '/id'
$insights = 'numberservice-insights'
$functionApp = "numberservice-$loc"
$repo = 'https://github.com/DanielLarsenNZ/NumberService.git'
$storage = "numservfn$loc"


# RESOURCE GROUP
az group create -n $rg --location $location --tags $tags


# COSMOS DB ACCOUNT
az cosmosdb create -n $cosmos -g $rg --default-consistency-level Session `
    --locations regionName=$location failoverPriority=0 isZoneRedundant=True
az cosmosdb sql database create -a $cosmos -g $rg -n $cosmosDB --throughput $throughput
az cosmosdb sql container create -a $cosmos -g $rg -d $cosmosDB -n $container -p $pk
$connString = ( az cosmosdb keys list -n $cosmos -g $rg --type 'connection-strings' | ConvertFrom-Json ).connectionStrings[0].connectionString


# STORAGE ACCOUNT
az storage account create -n $storage -g $rg --tags $tags --location $location --sku 'Standard_LRS'


# APPLICATION INSIGHTS
$instrumentationKey = ( az monitor app-insights component create --app $insights --location $location -g $rg --tags $tags | ConvertFrom-Json ).instrumentationKey


# FUNCTION APP
az functionapp create -n $functionApp -g $rg --consumption-plan-location $location --functions-version 3 `
    --app-insights $insights --app-insights-key $instrumentationKey -s $storage
az functionapp config appsettings set -n $functionApp -g $rg --settings `
    "CosmosDbConnectionString=$connString" `
    "CosmosDbDatabaseId=$cosmosDB" `
    "CosmosDbContainerId=$container"
az functionapp deployment source config -n $functionApp -g $rg --repo-url $repo --branch 'main'

# Tear down
# az group delete -n $rg --yes