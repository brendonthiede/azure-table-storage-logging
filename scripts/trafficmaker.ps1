$CEILING = 1000000
for ($i = 0; $i -lt $CEILING; $i++) {
  Invoke-RestMethod -Uri "https://ats-logging-research.azurewebsites.net/api/values"
  Write-Output "$($CEILING - $i) iterations to go"
}
