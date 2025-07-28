# PetSitter Connect - Azure Auto-Scale Deployment Guide

## 🎯 **Overview**

This guide will help you deploy your PetSitter Connect application to Azure with auto-scaling capabilities. The architecture is designed to handle varying user loads automatically.

## 🏗️ **Architecture**

```
Mobile Apps (iOS/Android)
    ↓
Azure Front Door (Global Load Balancer)
    ↓
Azure App Service (Auto-Scale: 1-20 instances)
    ↓
Azure SQL Database (Serverless Auto-Scale)
    ↓
Azure Redis Cache (Standard Tier)
    ↓
Azure SignalR Service (Auto-Scale)
    ↓
Azure Blob Storage + CDN
    ↓
Application Insights (Monitoring)
```

## 💰 **Cost Breakdown**

### **Auto-Scale Pricing (Pay-as-you-use)**
- **Idle (0-100 users)**: ~$25/month
- **Light Load (100-1K users)**: ~$75/month
- **Medium Load (1K-5K users)**: ~$200/month
- **High Load (5K-20K users)**: ~$600/month
- **Peak Load (20K+ users)**: ~$1500/month

## 🚀 **Deployment Steps**

### **Prerequisites**

1. **Azure Account**: [Create free account](https://azure.microsoft.com/free/)
2. **Azure CLI**: [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
3. **PowerShell** (for Windows) or **Azure Cloud Shell**
4. **Visual Studio 2022** or **VS Code**
5. **.NET 9 SDK**

### **Step 1: Prepare Your Environment**

```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "your-subscription-id"

# Install required Azure CLI extensions
az extension add --name webapp
az extension add --name sql
```

### **Step 2: Clone and Setup the API Project**

The API project has already been created in `/PetSitterConnect.API/` with:
- ✅ Auto-scaling ready architecture
- ✅ Distributed caching (Redis)
- ✅ Health checks
- ✅ Application Insights integration
- ✅ SignalR for real-time features
- ✅ JWT authentication
- ✅ Swagger documentation

### **Step 3: Deploy Using PowerShell Script**

```powershell
# Navigate to the API directory
cd PetSitterConnect.API

# Run the deployment script
./deploy-azure.ps1 -ResourceGroupName "PetSitterConnect-RG" -Location "East US" -AppName "petsitterconnect" -SqlAdminUsername "petadmin" -SqlAdminPassword (ConvertTo-SecureString "YourSecurePassword123!" -AsPlainText -Force)
```

### **Step 4: Alternative - Manual Azure Portal Deployment**

If you prefer using the Azure Portal:

1. **Create Resource Group**
2. **Deploy ARM Template** (`azure-deploy.json`)
3. **Configure App Service** with auto-scaling
4. **Set up databases and caches**
5. **Deploy application code**

### **Step 5: Configure CI/CD Pipeline**

The GitHub Actions workflow (`.github/workflows/azure-deploy.yml`) provides:
- ✅ Automated builds on code changes
- ✅ Automated testing
- ✅ Blue-green deployments
- ✅ Auto-scaling configuration
- ✅ Health checks
- ✅ Performance testing

**Required GitHub Secrets:**
```
AZURE_CREDENTIALS: {"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}
AZURE_RESOURCE_GROUP: PetSitterConnect-RG
AZURE_SUBSCRIPTION_ID: your-subscription-id
AZURE_APP_SERVICE_PLAN: petsitterconnect-plan
DATABASE_CONNECTION_STRING: your-database-connection-string
```

## ⚡ **Auto-Scaling Configuration**

### **App Service Auto-Scale Rules**

1. **CPU Scale-Out**: When CPU > 70% for 5 minutes → Add 1 instance
2. **CPU Scale-In**: When CPU < 30% for 10 minutes → Remove 1 instance
3. **Memory Scale-Out**: When Memory > 80% for 5 minutes → Add 1 instance
4. **Instance Limits**: Min: 1, Max: 20

### **Database Auto-Scale**
- **Azure SQL Serverless**: 0.5-16 vCores
- **Auto-pause**: After 60 minutes of inactivity
- **Storage**: Auto-grow up to 1TB

### **Redis Cache**
- **Standard Tier**: Automatic scaling based on memory usage
- **Connection pooling**: Optimized for high concurrency

## 📊 **Monitoring & Alerts**

### **Application Insights Metrics**
- Request rate and response times
- Dependency calls (database, cache, external APIs)
- Exception tracking
- Custom telemetry

### **Auto-Scale Alerts**
- Scale-out events
- Scale-in events
- Resource limit warnings
- Performance degradation alerts

### **Key Dashboards**
1. **Performance Dashboard**: Response times, throughput
2. **Auto-Scale Dashboard**: Instance count, scaling events
3. **Error Dashboard**: Exception rates, failed requests
4. **User Analytics**: Active users, feature usage

## 🔧 **Configuration**

### **Environment Variables**
```json
{
  "ConnectionStrings__DefaultConnection": "Azure SQL connection string",
  "ConnectionStrings__Redis": "Azure Redis connection string",
  "ConnectionStrings__AzureSignalR": "Azure SignalR connection string",
  "ApplicationInsights__ConnectionString": "App Insights connection string",
  "AzureStorage__ConnectionString": "Storage account connection string"
}
```

### **Auto-Scale Settings**
```json
{
  "profiles": [
    {
      "name": "Default",
      "capacity": {
        "minimum": "1",
        "maximum": "20",
        "default": "1"
      },
      "rules": [
        {
          "metricTrigger": {
            "metricName": "CpuPercentage",
            "threshold": 70,
            "operator": "GreaterThan",
            "timeWindow": "PT5M"
          },
          "scaleAction": {
            "direction": "Increase",
            "type": "ChangeCount",
            "value": "1",
            "cooldown": "PT5M"
          }
        }
      ]
    }
  ]
}
```

## 🔒 **Security Configuration**

### **Authentication**
- JWT tokens with Azure AD B2C integration
- API key authentication for mobile apps
- Role-based access control (RBAC)

### **Network Security**
- HTTPS enforcement
- CORS configuration for mobile apps
- API rate limiting
- SQL injection protection

### **Data Protection**
- Encryption at rest (Azure SQL, Storage)
- Encryption in transit (TLS 1.2+)
- Key management with Azure Key Vault

## 📱 **Mobile App Integration**

### **Update MAUI App Configuration**

1. **Update API Base URL**:
```csharp
public static class ApiConfig
{
    public const string BaseUrl = "https://petsitterconnect-api.azurewebsites.net/api/v1/";
    public const string SignalRUrl = "https://petsitterconnect-api.azurewebsites.net/chathub";
}
```

2. **Update Authentication**:
```csharp
services.AddHttpClient("PetSitterAPI", client =>
{
    client.BaseAddress = new Uri(ApiConfig.BaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
```

3. **Update SignalR Connection**:
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl(ApiConfig.SignalRUrl)
    .WithAutomaticReconnect()
    .Build();
```

## 🧪 **Testing Auto-Scale**

### **Load Testing**
```bash
# Install Apache Bench
sudo apt-get install apache2-utils

# Generate load to trigger auto-scaling
ab -n 1000 -c 50 https://petsitterconnect-api.azurewebsites.net/api/v1/petcarerequests

# Monitor scaling in Azure Portal
```

### **Monitoring Scaling Events**
1. Go to Azure Portal → App Service → Monitoring → Metrics
2. Add metrics: Instance Count, CPU Percentage, Memory Percentage
3. Set time range to last 1 hour
4. Watch for scaling events during load tests

## 🚨 **Troubleshooting**

### **Common Issues**

1. **Slow Cold Starts**
   - Solution: Enable "Always On" in App Service settings
   - Use Application Initialization module

2. **Database Connection Issues**
   - Check connection string format
   - Verify firewall rules allow Azure services
   - Monitor connection pool usage

3. **Auto-Scale Not Triggering**
   - Verify metrics are being collected
   - Check scale rule thresholds
   - Ensure sufficient load duration

4. **High Costs**
   - Review auto-scale rules (too aggressive?)
   - Check database usage (serverless pause settings)
   - Monitor storage and bandwidth usage

## 📈 **Performance Optimization**

### **Caching Strategy**
- Redis for session data and frequently accessed data
- CDN for static assets
- Application-level caching for expensive operations

### **Database Optimization**
- Use connection pooling
- Implement read replicas for heavy read workloads
- Optimize queries with proper indexing

### **API Optimization**
- Implement pagination for large datasets
- Use compression for API responses
- Implement API versioning for backward compatibility

## 🎉 **Go Live Checklist**

- [ ] All Azure resources deployed and configured
- [ ] Auto-scaling rules tested and verified
- [ ] Database migrations completed
- [ ] SSL certificate configured
- [ ] Custom domain configured (optional)
- [ ] Monitoring and alerts set up
- [ ] Mobile app updated with production API endpoints
- [ ] Load testing completed
- [ ] Backup and disaster recovery plan in place
- [ ] Security review completed
- [ ] Performance baseline established

## 📞 **Support & Maintenance**

### **Monitoring**
- Set up Azure Monitor alerts
- Configure Application Insights dashboards
- Monitor costs and usage patterns

### **Maintenance Tasks**
- Regular security updates
- Database maintenance and optimization
- Performance monitoring and tuning
- Cost optimization reviews

---

## 🚀 **Ready to Deploy?**

Your PetSitter Connect application is now ready for Azure deployment with auto-scaling! 

**Next Steps:**
1. Run the PowerShell deployment script
2. Update your mobile app configuration
3. Test the auto-scaling behavior
4. Set up monitoring and alerts
5. Go live! 🎉

**Need Help?**
- Check Azure documentation
- Review Application Insights logs
- Monitor auto-scaling metrics
- Test with gradual user load increases