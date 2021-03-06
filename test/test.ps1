while ($true) {
    $response = $null
    $response = Invoke-RestMethod -Method Put -Uri 'https://numberservice-aue.azurewebsites.net/api/numbers/free' 
    
    if ($null -ne $response) {
        if ($response.Number -ne $lastNumber + 1) {
            Write-Host $response -ForegroundColor Yellow
            Write-Error $response
        } else {
            Write-Host $response -ForegroundColor White
        }
        $lastNumber = $response.Number
    }

    $response = $null
    $response = Invoke-RestMethod -Method Get -Uri 'https://numberservice-ase.azurewebsites.net/api/numbers/free' 
    
    if ($null -ne $response) {
        if ($response.Number -ne $lastNumber) {
            Write-Host $response -ForegroundColor Yellow
            Write-Error $response
        } else {
            Write-Host $response -ForegroundColor White
        }
        $lastNumber = $response.Number
        Write-Host
    }

    $response = $null
    $response = Invoke-RestMethod -Method Put -Uri 'https://numberservice-ase.azurewebsites.net/api/numbers/free' 
    
    if ($null -ne $response) {
        if ($response.Number -ne $lastNumber + 1) {
            Write-Host $response -ForegroundColor Yellow
            Write-Error $response
        } else {
            Write-Host $response -ForegroundColor White
        }
        $lastNumber = $response.Number
    }

    $response = $null
    $response = Invoke-RestMethod -Method Get -Uri 'https://numberservice-aue.azurewebsites.net/api/numbers/free' 
    
    if ($null -ne $response) {
        if ($response.Number -ne $lastNumber) {
            Write-Host $response -ForegroundColor Yellow
            Write-Error $response
        } else {
            Write-Host $response -ForegroundColor White
        }
        $lastNumber = $response.Number
        Write-Host
    }

    #Start-Sleep -Seconds 5
}