while ($true) {
    $response = $null
    $response = Invoke-RestMethod -Method Put -Uri 'https://numberservice-aue.azurewebsites.net/api/numbers/free' 
    
    Write-Host $response

    $response = $null
    $response = Invoke-RestMethod -Method Put -Uri 'https://numberservice-ase.azurewebsites.net/api/numbers/free' 
    
    Write-Host $response

    #Start-Sleep -Seconds 5
}