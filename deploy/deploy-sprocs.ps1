Install-Module -Name CosmosDB   # Thanks @PlagueHO !

$cosmos = 'numberservice'
$cosmosdb = 'NumberService'
$collection = 'Numbers'

$primaryKey = ConvertTo-SecureString -String $env:COSMOSPRIMARYMASTERKEY -AsPlainText -Force
$cosmosDbContext = New-CosmosDbContext -Account $cosmos -Database $cosmosdb -Key $primaryKey

$sprocs = Get-CosmosDbStoredProcedure -Context $cosmosDbContext -CollectionId $collection | % { $_.Id }

Get-ChildItem -Path ./cosmos/sprocs -Name | ForEach-Object {
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
