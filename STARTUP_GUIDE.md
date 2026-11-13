# E-Commerce API Startup Guide

## Quick Start (Windows)

### Option 1: Direct dotnet run
```bash
cd D:\Projects\API\API
dotnet run --configuration Development
```

### Option 2: Using PowerShell
```powershell
cd D:\Projects\API\API
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-build
```

## Access Points
- **Swagger UI**: http://localhost:5103/swagger/index.html
- **API Base**: http://localhost:5103

## Available Endpoints

### Account (Authentication)
- `POST /api/account/register` - Register new user
- `POST /api/account/login` - Login user
- `GET /api/account/currentuser` - Get current authenticated user

### Products
- `GET /api/products` - Get all products with filtering/pagination
- `GET /api/products/{id}` - Get product by ID

### Basket
- `GET /api/basket/{id}` - Get basket by ID
- `POST /api/basket` - Update basket
- `DELETE /api/basket/{id}` - Delete basket

## Configuration

### Database
- **Ecommerce.db**: SQLite database for products, brands, types
- **Identity.db**: SQLite database for user authentication

### Connection Strings (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=Ecommerce.db",
	"IdentityConnection": "Data Source=Identity.db",
	"Redis": "localhost"
  }
}
```

### Redis
- Optional for basket functionality
- If Redis unavailable, basket operations degrade gracefully

## Test Data
- Pre-seeded with 10 product brands
- Pre-seeded with 3 product types
- Pre-seeded with sample products

## Troubleshooting

### Port 5103 already in use?
```powershell
# Change in launchSettings.json or use environment variable:
$env:ASPNETCORE_URLS = 'http://localhost:5104'
```

### Database locked error?
```powershell
# Make sure no other instance is running
Get-Process | Where-Object {$_.Name -like "*API*"} | Stop-Process -Force
```

### Redis connection error?
- API will start without Redis
- Basket operations will skip Redis caching
- This is expected in development if Redis not installed

