# Deploys any stored procedures found in /sprocs
. ./_vars.ps1

$ErrorActionPreference = 'Stop'

Install-Module -Name CosmosDB -Confirm   # Thanks @PlagueHO !

$cosmos = 'numberservice'
$cosmosdb = 'NumberService'
$collection = $container

$cosmosKey = ( az cosmosdb keys list -n $cosmos -g $rg --type 'keys' | ConvertFrom-Json ).primaryMasterKey
$primaryKey = ConvertTo-SecureString -String $cosmosKey -AsPlainText -Force
$cosmosDbContext = New-CosmosDbContext -Account $cosmos -Database $cosmosdb -Key $primaryKey

$sprocs = Get-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $collection | % { $_.Id }

Get-ChildItem -Path ../sprocs -Name | ForEach-Object {
    [string] $body = Get-Content -Path $( Join-Path '../sprocs' $_ ) -Raw
    $id = $_.Replace('.js', '')
    if ($sprocs -contains $id) {
        Set-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $collection `
            -Id $id -StoredProcedureBody $body 
    } else {
        New-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $collection `
            -Id $id -StoredProcedureBody $body 
    }
}
