# Deploys any stored procedures found in /sprocs
. ./_vars.ps1

$ErrorActionPreference = 'Stop'

Install-Module -Name CosmosDB -Confirm   # Thanks @PlagueHO !

$cosmosKey = ( az cosmosdb keys list -n $cosmos -g $rg --type 'keys' | ConvertFrom-Json ).primaryMasterKey
$primaryKey = ConvertTo-SecureString -String $cosmosKey -AsPlainText -Force
$cosmosDbContext = New-CosmosDbContext -Account $cosmos -Database $cosmosdb -Key $primaryKey

# Get a list of stored procedures in the Cosmos DB container
$sprocs = Get-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $container | % { $_.Id }

# For each sproc found in /sprocs folder
Get-ChildItem -Path ../sprocs -Name | ForEach-Object {
    [string] $body = Get-Content -Path $( Join-Path '../sprocs' $_ ) -Raw
    
    # Derive id from filename
    $id = $_.Replace('.js', '')
    
    # If sproc exists, update it, otherwise create it
    if ($sprocs -contains $id) {
        Set-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $container `
            -Id $id -StoredProcedureBody $body 
    } else {
        New-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $container `
            -Id $id -StoredProcedureBody $body 
    }
}
