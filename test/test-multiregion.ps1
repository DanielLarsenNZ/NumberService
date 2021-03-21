$numbers = [System.Collections.Concurrent.ConcurrentDictionary[string,object]]::new()

$i = 0
while ($true) {
    $i++
    'https://numberservice-aue.azurewebsites.net/api/numbers/free?diagnostics', 'https://numberservice-ase.azurewebsites.net/api/numbers/free?diagnostics' | ForEach-Object -Parallel {

        $response = $null
        $response = Invoke-RestMethod -Method Put -Uri $_ 
        Write-Host $response.number -ForegroundColor Yellow
        Write-Host $response
        Write-Host ( $response.CosmosDiagnostics.Context | Where-Object -Property Id -eq 'StoreResponseStatistics' )

        $numbers = $using:numbers
        $numbers[$_] = $response.number
    }

    if ($numbers['https://numberservice-aue.azurewebsites.net/api/numbers/free?diagnostics'] -eq $numbers['https://numberservice-ase.azurewebsites.net/api/numbers/free?diagnostics']) {
        Write-Host "COLLISION after $i tests" -ForegroundColor Red
        Write-Host $numbers['https://numberservice-aue.azurewebsites.net/api/numbers/free?diagnostics'] -ForegroundColor Yellow
        Write-Host $numbers['https://numberservice-ase.azurewebsites.net/api/numbers/free?diagnostics'] -ForegroundColor Yellow
        break
    }
}
