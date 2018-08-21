param(
  [Parameter(Mandatory = $False)]
  [String]
  $EnvironmentName = "AzureCloud",

  [Parameter(Mandatory = $False)]
  [String]
  $SubscriptionName = "Visual Studio Enterprise",

  [Parameter(Mandatory = $False)]
  [String]
  $ResourceLocation = "eastus",

  [Parameter(Mandatory = $False)]
  [Switch]
  $CodeOnly
)

function Save-ShellAppearance {
  [Environment]::SetEnvironmentVariable("FOREGROUND_COLOR", [console]::ForegroundColor, "User")
  [Environment]::SetEnvironmentVariable("BACKGROUND_COLOR", [console]::BackgroundColor, "User")
}

function Reset-ShellAppearance {
  [console]::ForegroundColor = [Environment]::GetEnvironmentVariable("FOREGROUND_COLOR", "User")
  [console]::BackgroundColor = [Environment]::GetEnvironmentVariable("BACKGROUND_COLOR", "User")
}

function Invoke-ErrorReport {
  Param ([string] $Message)
  Reset-ShellAppearance
  Write-Error "$Message"
  Pop-Location
  Exit
}

function Connect-AzSubscription {
  Param ([string] $EnvironmentName, [string] $SubscriptionName)
  $ACCOUNT_INFO = (az account show | ConvertFrom-Json)
  Reset-ShellAppearance
  if ($ACCOUNT_INFO.environmentName -ne "$EnvironmentName" -or $ACCOUNT_INFO.name -ne "$SubscriptionName") {
    az cloud set --name "$EnvironmentName"
    az login
    az account set --subscription "$SubscriptionName"
    if ($LASTEXITCODE -ne 0) { Invoke-ErrorReport "Could not connect to subscription '$SubscriptionName'" }
    $ACCOUNT_INFO = (az account show | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0 -or $ACCOUNT_INFO.environmentName -ne "$EnvironmentName" -or $ACCOUNT_INFO.name -ne "$SubscriptionName") { Invoke-ErrorReport "Could not connect to subscription '$SubscriptionName'" }
  }
}

function New-AzResourceDeployment {
  Param ([string] $ResourceGroupName, [string] $ResourceLocation, [string] $TemplateFile)
  $TIMESTAMP = (Get-Date).ToString("yyyyMMdd-HHmm")
  $DEPLOYMENT_NAME = "LoggingResearch$TIMESTAMP"
  az group create --name $ResourceGroupName --location $ResourceLocation --tag purpose=research
  if ($LASTEXITCODE -ne 0) { Invoke-ErrorReport "Could not create resource group" }
  $DEPLOYMENT_OUTPUTS = ((az group deployment create `
      --verbose `
      --name $DEPLOYMENT_NAME `
      --resource-group $ResourceGroupName `
      --template-file $TemplateFile | ConvertFrom-Json).properties.outputs)
  if ($LASTEXITCODE -ne 0) { Invoke-ErrorReport "ARM template deployment error: $DEPLOYMENT_OUTPUTS" }

  # Wait for app to be ready for deployment
  $TIMEOUT = new-timespan -Minutes 5
  $STOP_WATCH = [diagnostics.stopwatch]::StartNew()
  $APP_READY = $False
  while (!$APP_READY -and $STOP_WATCH.elapsed -lt $TIMEOUT) {
    $WEB_APP = (az webapp show -g $ResourceGroupName -n $DEPLOYMENT_OUTPUTS.webAppName.value | ConvertFrom-Json)
    Write-Output "Current web app status: { state: '$($WEB_APP.state)', availabilityState: '$($WEB_APP.availabilityState)' }"
    if ($WEB_APP.state -eq "Running" -and $WEB_APP.availabilityState -eq "Normal") {
      $APP_READY = $True
    }

    Start-Sleep -seconds 5
  }

  Return $DEPLOYMENT_OUTPUTS
}

Push-Location
$PROJECT_ROOT = "$PSScriptRoot\.."
Set-Location $PROJECT_ROOT
$RESOURCE_GROUP_NAME = "logging-research-rg"
$WEB_APP_FOLDER = "Serilog.Core.2.0"
Save-ShellAppearance

Connect-AzSubscription -EnvironmentName "$EnvironmentName" -SubscriptionName "$SubscriptionName"
if ($CodeOnly) {
  $WEB_APP_OBJECT = (az webapp show -g logging-research-rg -n ats-logging-research | ConvertFrom-Json)
  $WEB_APP_NAME = $WEB_APP_OBJECT.name
  $SITE_URL = "https://$($WEB_APP_OBJECT.hostNames[0])"
}
else {
  $DEPLOYMENT_OUTPUTS = New-AzResourceDeployment -ResourceGroupName $RESOURCE_GROUP_NAME -ResourceLocation $ResourceLocation -TemplateFile ./scripts/autodeploy.json
  $WEB_APP_NAME = $DEPLOYMENT_OUTPUTS.webAppName.value
  $SITE_URL = "https://$($DEPLOYMENT_OUTPUTS.webAppUri.value)"
}

# Compile Azure web app and package for deployment
Push-Location
Set-Location "$PROJECT_ROOT\$WEB_APP_FOLDER"
$artifactFolder = "$($PWD.Path)\dist"
$publishOut = "$artifactFolder\artifact"
If (Test-path $artifactFolder) { Remove-Item $artifactFolder -Force -Recurse }
dotnet publish $WEB_APP_FOLDER.csproj --configuration Debug --output $publishOut
if ($LASTEXITCODE -ne 0) { Invoke-ErrorReport "Could not compile the web app" }
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($publishOut, "$artifactFolder\webapp.zip")
Remove-Item $publishOut -Force -Recurse
Pop-Location

# Deploy web app to Azure
az webapp deployment source config-zip `
  --resource-group $RESOURCE_GROUP_NAME `
  --name $WEB_APP_NAME `
  --src ".\$WEB_APP_FOLDER\dist\webapp.zip"
if ($LASTEXITCODE -ne 0) { Invoke-ErrorReport "Could not deploy the Azure Web App" }

Reset-ShellAppearance

Write-Output "Site is ready at $SITE_URL/api/values"

Pop-Location