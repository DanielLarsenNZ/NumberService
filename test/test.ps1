while ($true) {
    Invoke-RestMethod -Method Put -Uri 'https://numberservice-aue.azurewebsites.net/api/numbers/free' 
    Start-Sleep -Seconds 30
}