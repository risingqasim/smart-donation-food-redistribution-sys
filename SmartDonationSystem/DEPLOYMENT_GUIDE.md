# Smart Donation System - Deployment Guide

## 🚀 **Deployment Options**

The Smart Donation System supports multiple deployment scenarios:

1. **Azure App Service** - Cloud deployment on Microsoft Azure
2. **IIS (Internet Information Services)** - On-premises Windows deployment
3. **Docker Containers** - Containerized deployment
4. **Linux with Nginx** - Linux server deployment

## 📋 **Prerequisites**

### **For All Deployments:**
- .NET 9.0 Runtime
- SQL Server (2019 or later)
- SSL Certificate (for HTTPS)
- Google Maps API Key
- JWT Secret Key (32+ characters)

### **For Azure Deployment:**
- Azure Subscription
- Azure CLI or PowerShell
- Azure DevOps (optional)

### **For IIS Deployment:**
- Windows Server with IIS
- ASP.NET Core Hosting Bundle
- SQL Server (local or remote)

### **For Docker Deployment:**
- Docker Engine
- Docker Compose
- SQL Server container or external database

## 🔧 **Configuration Setup**

### **1. Environment Variables**

Create environment variables for sensitive configuration:

```bash
# Database
SQL_SERVER=your-sql-server
SQL_DATABASE=SmartDonationSystemDb
SQL_USER=your-sql-user
SQL_PASSWORD=your-sql-password

# JWT
JWT_SECRET_KEY=YourSuperSecretKeyThatIsAtLeast32CharactersLong!

# Google Maps
GOOGLE_MAPS_API_KEY=your-google-maps-api-key

# Email
SMTP_SERVER=smtp.gmail.com
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
```

### **2. Connection Strings**

#### **SQL Server Connection String:**
```
Server={SQL_SERVER};Database={SQL_DATABASE};User Id={SQL_USER};Password={SQL_PASSWORD};TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;
```

#### **Azure SQL Database:**
```
Server=tcp:{server}.database.windows.net,1433;Initial Catalog={database};Persist Security Info=False;User ID={user};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## 🌐 **Azure App Service Deployment**

### **1. Prerequisites**
- Azure Subscription
- Azure CLI installed
- PowerShell (for deployment scripts)

### **2. Quick Deployment**

```powershell
# Run the Azure deployment script
.\scripts\deploy-azure.ps1 -ResourceGroupName "SmartDonationSystem-RG" -AppServiceName "smart-donation-system" -SqlServerName "smartdonation-sql" -SqlDatabaseName "SmartDonationSystemDb" -SqlAdminUsername "sqladmin" -SqlAdminPassword "YourStrong@Passw0rd" -JwtSecretKey "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" -GoogleMapsApiKey "YOUR_GOOGLE_MAPS_API_KEY"
```

### **3. Manual Azure Setup**

#### **Create Resource Group:**
```bash
az group create --name SmartDonationSystem-RG --location "East US"
```

#### **Create App Service Plan:**
```bash
az appservice plan create --name SmartDonationSystem-Plan --resource-group SmartDonationSystem-RG --sku S1 --is-linux
```

#### **Create App Service:**
```bash
az webapp create --name smart-donation-system --resource-group SmartDonationSystem-RG --plan SmartDonationSystem-Plan --runtime "DOTNET|9.0"
```

#### **Create SQL Server:**
```bash
az sql server create --name smartdonation-sql --resource-group SmartDonationSystem-RG --location "East US" --admin-user sqladmin --admin-password "YourStrong@Passw0rd"
```

#### **Create SQL Database:**
```bash
az sql db create --name SmartDonationSystemDb --resource-group SmartDonationSystem-RG --server smartdonation-sql --service-objective S0
```

#### **Configure App Settings:**
```bash
az webapp config appsettings set --name smart-donation-system --resource-group SmartDonationSystem-RG --settings \
  "ConnectionStrings__DefaultConnection=Server=tcp:smartdonation-sql.database.windows.net,1433;Initial Catalog=SmartDonationSystemDb;Persist Security Info=False;User ID=sqladmin;Password=YourStrong@Passw0rd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
  "JwtSettings__SecretKey=YourSuperSecretKeyThatIsAtLeast32CharactersLong!" \
  "GoogleMaps__ApiKey=YOUR_GOOGLE_MAPS_API_KEY" \
  "ASPNETCORE_ENVIRONMENT=Production"
```

### **4. Deploy Application**

#### **Using Azure DevOps:**
1. Create Azure DevOps project
2. Add `azure-pipelines.yml` to repository
3. Configure service connections
4. Run pipeline

#### **Using Visual Studio:**
1. Right-click project → Publish
2. Select Azure App Service
3. Configure connection
4. Publish

#### **Using Azure CLI:**
```bash
# Build and deploy
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip --name smart-donation-system --resource-group SmartDonationSystem-RG --src ./publish.zip
```

## 🖥️ **IIS Deployment**

### **1. Prerequisites**
- Windows Server with IIS
- ASP.NET Core Hosting Bundle
- SQL Server (local or remote)

### **2. IIS Setup**

#### **Install ASP.NET Core Hosting Bundle:**
1. Download from Microsoft website
2. Install on target server
3. Restart IIS

#### **Configure IIS:**
```powershell
# Run the IIS deployment script
.\scripts\deploy-iis.ps1 -SiteName "SmartDonationSystem" -PhysicalPath "C:\inetpub\wwwroot\SmartDonationSystem" -SqlServerName "localhost" -SqlDatabaseName "SmartDonationSystemDb" -SqlUsername "sa" -SqlPassword "YourStrong@Passw0rd" -JwtSecretKey "YourSuperSecretKeyThatIsAtLeast32CharactersLong!" -GoogleMapsApiKey "YOUR_GOOGLE_MAPS_API_KEY"
```

### **3. Manual IIS Configuration**

#### **Create Application Pool:**
1. Open IIS Manager
2. Right-click Application Pools → Add Application Pool
3. Name: SmartDonationSystem
4. .NET CLR Version: No Managed Code
5. Managed Pipeline Mode: Integrated

#### **Create Website:**
1. Right-click Sites → Add Website
2. Site name: SmartDonationSystem
3. Application pool: SmartDonationSystem
4. Physical path: C:\inetpub\wwwroot\SmartDonationSystem
5. Port: 80

#### **Configure SSL:**
1. Install SSL certificate
2. Add HTTPS binding (port 443)
3. Configure URL Rewrite for HTTPS redirect

### **4. Deploy Application**

#### **Using Visual Studio:**
1. Right-click project → Publish
2. Select IIS
3. Configure connection
4. Publish

#### **Using Command Line:**
```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Copy to IIS directory
xcopy ./publish C:\inetpub\wwwroot\SmartDonationSystem /E /I /Y
```

## 🐳 **Docker Deployment**

### **1. Prerequisites**
- Docker Engine
- Docker Compose
- SQL Server (container or external)

### **2. Build Docker Image**

```bash
# Build the image
docker build -t smart-donation-system .

# Run with docker-compose
docker-compose up -d
```

### **3. Docker Compose Configuration**

The `docker-compose.yml` includes:
- Smart Donation System application
- SQL Server 2022
- Redis (for caching)
- Volume mounts for data persistence

### **4. Environment Configuration**

Create `.env` file:
```bash
# Copy from env.example
cp env.example .env

# Edit with your values
nano .env
```

### **5. Deploy with Docker Compose**

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## 🐧 **Linux with Nginx Deployment**

### **1. Prerequisites**
- Ubuntu 20.04+ or CentOS 8+
- .NET 9.0 Runtime
- Nginx
- SQL Server (local or remote)

### **2. Install Dependencies**

```bash
# Install .NET 9.0 Runtime
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-runtime-9.0

# Install Nginx
sudo apt-get install -y nginx

# Install SQL Server client
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
sudo apt-get update
sudo apt-get install -y mssql-tools unixodbc-dev
```

### **3. Configure Nginx**

Create `/etc/nginx/sites-available/smart-donation-system`:
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### **4. Configure Systemd Service**

Create `/etc/systemd/system/smart-donation-system.service`:
```ini
[Unit]
Description=Smart Donation System
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/smart-donation-system/SmartDonationSystem.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

### **5. Deploy Application**

```bash
# Create application directory
sudo mkdir -p /var/www/smart-donation-system

# Copy application files
sudo cp -r ./publish/* /var/www/smart-donation-system/

# Set permissions
sudo chown -R www-data:www-data /var/www/smart-donation-system
sudo chmod -R 755 /var/www/smart-donation-system

# Enable and start service
sudo systemctl enable smart-donation-system
sudo systemctl start smart-donation-system

# Enable and start Nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

## 🗄️ **Database Setup**

### **1. Run Migrations**

After deployment, run database migrations:

```bash
# Update database schema
dotnet ef database update

# Or using connection string
dotnet ef database update --connection "Server=your-server;Database=SmartDonationSystemDb;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
```

### **2. Seed Initial Data**

The application automatically seeds:
- Default roles (Admin, Donor, NGO)
- Admin user
- Sample NGO
- Sample donations

### **3. Database Configuration**

#### **SQL Server Settings:**
- Enable TCP/IP protocol
- Configure firewall rules
- Set up backup strategy
- Configure monitoring

#### **Azure SQL Database:**
- Configure firewall rules
- Set up geo-replication
- Configure backup retention
- Set up monitoring alerts

## 🔒 **Security Configuration**

### **1. SSL/TLS Setup**

#### **For Azure App Service:**
- Use built-in SSL certificates
- Configure custom domain
- Enable HTTPS redirect

#### **For IIS:**
- Install SSL certificate
- Configure HTTPS binding
- Enable HTTPS redirect

#### **For Docker/Linux:**
- Use Let's Encrypt certificates
- Configure Nginx SSL
- Enable HTTPS redirect

### **2. Security Headers**

Configure security headers in `Program.cs`:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});
```

### **3. CORS Configuration**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

## 📊 **Monitoring and Logging**

### **1. Application Insights (Azure)**

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

### **2. Logging Configuration**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### **3. Health Checks**

```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddSqlServer(connectionString);

// Map health check endpoint
app.MapHealthChecks("/health");
```

## 🚀 **Deployment Checklist**

### **Pre-Deployment:**
- [ ] Update connection strings
- [ ] Configure JWT secret key
- [ ] Set up Google Maps API key
- [ ] Configure email settings
- [ ] Set up SSL certificates
- [ ] Configure CORS settings
- [ ] Set up monitoring

### **Post-Deployment:**
- [ ] Run database migrations
- [ ] Verify application startup
- [ ] Test all endpoints
- [ ] Configure monitoring
- [ ] Set up backups
- [ ] Configure alerts
- [ ] Test SSL/HTTPS
- [ ] Verify email functionality
- [ ] Test Google Maps integration

### **Production Readiness:**
- [ ] Performance testing
- [ ] Security scanning
- [ ] Load testing
- [ ] Backup verification
- [ ] Disaster recovery plan
- [ ] Documentation update
- [ ] Team training

## 🔧 **Troubleshooting**

### **Common Issues:**

1. **Database Connection Issues:**
   - Verify connection string
   - Check firewall rules
   - Ensure SQL Server is running

2. **SSL Certificate Issues:**
   - Verify certificate installation
   - Check certificate validity
   - Configure HTTPS redirect

3. **Google Maps API Issues:**
   - Verify API key
   - Check API quotas
   - Configure allowed domains

4. **Email Configuration:**
   - Verify SMTP settings
   - Check authentication
   - Test email delivery

### **Log Locations:**

- **Azure App Service:** Application Insights
- **IIS:** C:\inetpub\logs\LogFiles\
- **Docker:** Container logs
- **Linux:** /var/log/nginx/ and systemd logs

## 📞 **Support**

For deployment issues:
1. Check application logs
2. Verify configuration settings
3. Test connectivity
4. Review security settings
5. Contact system administrator

The Smart Donation System is now ready for production deployment with comprehensive monitoring, security, and scalability features!
