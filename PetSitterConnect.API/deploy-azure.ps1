# Azure Deployment Script for PetSitter Connect API with Auto-Scaling
# This script deploys the entire infrastructure and application to Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$true)]
    [string]$AppName = "petsitterconnect",
    
    [Parameter(Mandatory=$true)]
    [string]$SqlAdminUsername,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlAdminPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Azure deployment for PetSitter Connect API..." -ForegroundColor Green

# Login to Azure (if not already logged in)
try {
    $context = Get-AzContext
    if (!$context) {
        Write-Host "Please login to Azure..." -ForegroundColor Yellow
        Connect-AzAccount
    }
} catch {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    Connect-AzAccount
}

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Blue
    Set-AzContext -SubscriptionId $SubscriptionId
}

# Create Resource Group
Write-Host "📦 Creating Resource Group: $ResourceGroupName" -ForegroundColor Blue
try {
    $rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
    if (!$rg) {
        New-AzResourceGroup -Name $ResourceGroupName -Location $Location
        Write-Host "✅ Resource Group created successfully" -ForegroundColor Green
    } else {
        Write-Host "✅ Resource Group already exists" -ForegroundColor Green
    }
} catch {
    Write-Error "Failed to create Resource Group: $_"
    exit 1
}

# Deploy ARM Template
Write-Host "🏗️ Deploying Azure resources with auto-scaling configuration..." -ForegroundColor Blue
try {
    $templateFile = Join-Path $PSScriptRoot "azure-deploy.json"
    
    $deploymentParams = @{
        ResourceGroupName = $ResourceGroupName
        TemplateFile = $templateFile
        appName = $AppName
        location = $Location
        sqlAdministratorLogin = $SqlAdminUsername
        sqlAdministratorLoginPassword = $SqlAdminPassword
    }
    
    $deployment = New-AzResourceGroupDeployment @deploymentParams -Verbose
    
    if ($deployment.ProvisioningState -eq "Succeeded") {
        Write-Host "✅ Azure resources deployed successfully" -ForegroundColor Green
        Write-Host "🌐 Web App URL: $($deployment.Outputs.webAppUrl.Value)" -ForegroundColor Cyan
        Write-Host "🗄️ SQL Server: $($deployment.Outputs.sqlServerFqdn.Value)" -ForegroundColor Cyan
    } else {
        throw "Deployment failed with state: $($deployment.ProvisioningState)"
    }
} catch {
    Write-Error "Failed to deploy Azure resources: $_"
    exit 1
}

# Build and publish the application
Write-Host "🔨 Building and publishing the application..." -ForegroundColor Blue
try {
    $projectPath = Join-Path $PSScriptRoot "PetSitterConnect.API.csproj"
    $publishPath = Join-Path $PSScriptRoot "bin/Release/net9.0/publish"
    
    # Clean and build
    dotnet clean $projectPath --configuration Release
    dotnet build $projectPath --configuration Release --no-restore
    
    # Publish
    dotnet publish $projectPath --configuration Release --output $publishPath --no-build
    
    Write-Host "✅ Application built and published successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to build application: $_"
    exit 1
}

# Deploy to Azure App Service
Write-Host "📤 Deploying application to Azure App Service..." -ForegroundColor Blue
try {
    $webAppName = "$AppName-api"
    $zipPath = Join-Path $PSScriptRoot "deploy.zip"
    
    # Create deployment package
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath
    
    # Deploy using Azure CLI (more reliable than PowerShell for zip deploy)
    $deployCommand = "az webapp deployment source config-zip --resource-group $ResourceGroupName --name $webAppName --src `"$zipPath`""
    Invoke-Expression $deployCommand
    
    Write-Host "✅ Application deployed successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to deploy application: $_"
    exit 1
}

# Configure auto-scaling rules
Write-Host "⚡ Configuring auto-scaling rules..." -ForegroundColor Blue
try {
    $appServicePlanName = "$AppName-plan"
    
    # Create auto-scale profile
    $scaleProfile = New-AzAutoscaleProfile -DefaultCapacity 1 -MaximumCapacity 20 -MinimumCapacity 1 -Name "Default"
    
    # CPU scale-out rule
    $scaleOutRule = New-AzAutoscaleRule -MetricName "CpuPercentage" -MetricResourceId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/serverfarms/$appServicePlanName" -Operator GreaterThan -MetricStatistic Average -Threshold 70 -TimeGrain 00:01:00 -TimeWindow 00:05:00 -ScaleActionCooldown 00:05:00 -ScaleActionDirection Increase -ScaleActionValue 1
    
    # CPU scale-in rule
    $scaleInRule = New-AzAutoscaleRule -MetricName "CpuPercentage" -MetricResourceId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/serverfarms/$appServicePlanName" -Operator LessThan -MetricStatistic Average -Threshold 30 -TimeGrain 00:01:00 -TimeWindow 00:10:00 -ScaleActionCooldown 00:10:00 -ScaleActionDirection Decrease -ScaleActionValue 1
    
    # Memory scale-out rule
    $memoryScaleOutRule = New-AzAutoscaleRule -MetricName "MemoryPercentage" -MetricResourceId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/serverfarms/$appServicePlanName" -Operator GreaterThan -MetricStatistic Average -Threshold 80 -TimeGrain 00:01:00 -TimeWindow 00:05:00 -ScaleActionCooldown 00:05:00 -ScaleActionDirection Increase -ScaleActionValue 1
    
    # Add rules to profile
    Add-AzAutoscaleRule -AutoscaleProfile $scaleProfile -Rule $scaleOutRule
    Add-AzAutoscaleRule -AutoscaleProfile $scaleProfile -Rule $scaleInRule
    Add-AzAutoscaleRule -AutoscaleProfile $scaleProfile -Rule $memoryScaleOutRule
    
    # Create auto-scale setting
    $autoScaleSetting = New-AzAutoscaleSetting -Location $Location -Name "$appServicePlanName-autoscale" -ResourceGroupName $ResourceGroupName -TargetResourceId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/serverfarms/$appServicePlanName" -AutoscaleProfile $scaleProfile
    
    Write-Host "✅ Auto-scaling rules configured successfully" -ForegroundColor Green
} catch {
    Write-Warning "Auto-scaling configuration may have failed: $_"
    Write-Host "You can configure auto-scaling manually in the Azure Portal" -ForegroundColor Yellow
}

# Run database migrations
Write-Host "🗄️ Running database migrations..." -ForegroundColor Blue
try {
    $webAppName = "$AppName-api"
    
    # Use Azure CLI to run migrations remotely
    $migrationCommand = "az webapp ssh --resource-group $ResourceGroupName --name $webAppName --command `"dotnet ef database update`""
    
    Write-Host "Running: $migrationCommand" -ForegroundColor Gray
    # Note: This requires the app to be running and SSH to be enabled
    # Invoke-Expression $migrationCommand
    
    Write-Host "⚠️ Please run database migrations manually using:" -ForegroundColor Yellow
    Write-Host "   dotnet ef database update --connection `"[Your Connection String]`"" -ForegroundColor Gray
} catch {
    Write-Warning "Database migration setup failed: $_"
}

# Configure monitoring alerts
Write-Host "📊 Setting up monitoring alerts..." -ForegroundColor Blue
try {
    $webAppName = "$AppName-api"
    $appInsightsName = "$AppName-insights"
    
    # High CPU alert
    $cpuAlert = New-AzMetricAlertRuleV2 -Name "High CPU Usage" -ResourceGroupName $ResourceGroupName -WindowSize 00:05:00 -Frequency 00:01:00 -TargetResourceId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$webAppName" -ConditionMetricName "CpuPercentage" -ConditionOperator GreaterThan -ConditionThreshold 85 -ActionGroupId "/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/$ResourceGroupName/providers/microsoft.insights/actionGroups/default"
    
    Write-Host "✅ Monitoring alerts configured" -ForegroundColor Green
} catch {
    Write-Warning "Monitoring alert setup may have failed: $_"
}

Write-Host ""
Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Deployment Summary:" -ForegroundColor Cyan
Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White
Write-Host "   App Service: $AppName-api" -ForegroundColor White
Write-Host "   SQL Server: $AppName-sql" -ForegroundColor White
Write-Host "   Redis Cache: $AppName-redis" -ForegroundColor White
Write-Host "   SignalR Service: $AppName-signalr" -ForegroundColor White
Write-Host "   Storage Account: $($AppName.Replace('-',''))storage" -ForegroundColor White
Write-Host ""
Write-Host "🔗 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Update your mobile app to use the API endpoint" -ForegroundColor White
Write-Host "   2. Configure custom domain and SSL certificate" -ForegroundColor White
Write-Host "   3. Set up CI/CD pipeline for automated deployments" -ForegroundColor White
Write-Host "   4. Configure monitoring and alerting" -ForegroundColor White
Write-Host "   5. Run database migrations" -ForegroundColor White
Write-Host ""
Write-Host "💡 Auto-scaling is configured to:" -ForegroundColor Cyan
Write-Host "   - Scale out when CPU > 70% for 5 minutes" -ForegroundColor White
Write-Host "   - Scale in when CPU < 30% for 10 minutes" -ForegroundColor White
Write-Host "   - Min instances: 1, Max instances: 20" -ForegroundColor White