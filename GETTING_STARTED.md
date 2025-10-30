# Getting Started with Stock Manager

This guide will help you get the application up and running quickly.

## Quick Start (Development)

### 1. Start the Backend API

```bash
# Navigate to backend directory
cd backend/StockManager.API

# Run the API (will use SQL LocalDB by default)
dotnet run
```

The API will start on `http://localhost:5000` and `https://localhost:5001`

Access Swagger UI at: `http://localhost:5000/swagger`

### 2. Apply Database Migrations

On first run, apply the database migrations:

```bash
# From the backend directory
cd backend
dotnet ef database update --project StockManager.Data --startup-project StockManager.API
```

### 3. Start the Angular Frontend

```bash
# Navigate to frontend directory
cd frontend/stock-manager-app

# Install dependencies (first time only)
npm install

# Start the development server
ng serve
```

The app will open at: `http://localhost:4200`

### 4. Create Your First Account

1. Go to `http://localhost:4200/register`
2. Fill in the registration form:
   - **Email**: your@email.com
   - **Password**: Choose a strong password (min 6 chars, uppercase, lowercase, number)
   - **First Name**: Your first name
   - **Last Name**: Your last name
   - **Business Name**: Your company name
   - **Role**: Select Admin
3. Click Register
4. You'll be automatically logged in and redirected to the dashboard

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Angular Frontend                         │
│                   (http://localhost:4200)                    │
│                                                              │
│  ┌────────────┐  ┌────────────┐  ┌─────────────────────┐  │
│  │   Login/   │  │ Dashboard  │  │   Products &        │  │
│  │  Register  │  │  (Stats)   │  │  Stock Movements    │  │
│  └────────────┘  └────────────┘  └─────────────────────┘  │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP + JWT Token
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core API                           │
│                  (http://localhost:5000)                     │
│                                                              │
│  ┌────────────┐  ┌────────────┐  ┌─────────────────────┐  │
│  │   Auth     │  │  Products  │  │  Stock Movements    │  │
│  │ Controller │  │ Controller │  │    Controller       │  │
│  └────────────┘  └────────────┘  └─────────────────────┘  │
│         │                │                     │            │
│         ▼                ▼                     ▼            │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              Service Layer                           │  │
│  │  (Business Logic, Validation, Authorization)        │  │
│  └─────────────────────────────────────────────────────┘  │
│         │                                                   │
│         ▼                                                   │
│  ┌─────────────────────────────────────────────────────┐  │
│  │         Repository Layer (Unit of Work)              │  │
│  └─────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────┘
                       │ Entity Framework Core
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    SQL Server Database                       │
│                   (StockManagerDb)                           │
│                                                              │
│  Tables: Users, Businesses, Products, Categories,           │
│          StockMovements, Identity Tables                    │
└─────────────────────────────────────────────────────────────┘
```

## Key Concepts

### Authentication Flow

1. User registers → Creates Business and User account
2. User logs in → Receives JWT token (valid for 7 days)
3. Token is stored in localStorage
4. All API requests include token in Authorization header
5. Backend validates token and extracts user/business info
6. API returns 401 if token is invalid/expired → Auto logout

### Multi-Tenancy

- Each user belongs to a Business
- All data is isolated by BusinessId
- Users can only see/modify data for their business
- Business context is embedded in JWT token

### Stock Movement Types

1. **Stock In**: Adding inventory (purchases, returns, production)
2. **Stock Out**: Removing inventory (sales, waste, damages)
3. **Stock Transfer**: Moving between locations
4. **Stock Adjustment**: Manual corrections with reason

### Role-Based Access

| Feature | Admin | Manager | Staff | Viewer |
|---------|-------|---------|-------|--------|
| View Dashboard | ✅ | ✅ | ✅ | ✅ |
| View Products | ✅ | ✅ | ✅ | ✅ |
| Add Products | ✅ | ✅ | ✅ | ❌ |
| Edit Products | ✅ | ✅ | ✅ | ❌ |
| Delete Products | ✅ | ✅ | ❌ | ❌ |
| Stock Movements | ✅ | ✅ | ✅ | ❌ |
| View Reports | ✅ | ✅ | ✅ | ✅ |
| User Management | ✅ | ❌ | ❌ | ❌ |

## Component Development Guide

The frontend components are generated but need implementation. Here's what needs to be completed:

### Login Component
Location: `frontend/stock-manager-app/src/app/components/auth/login`

Key tasks:
- Create reactive form with email and password
- Call `authService.login()`
- Handle errors and show user feedback
- Redirect to dashboard on success

### Register Component
Location: `frontend/stock-manager-app/src/app/components/auth/register`

Key tasks:
- Create reactive form with all registration fields
- Call `authService.register()`
- Validate password strength
- Redirect to dashboard on success

### Dashboard Component
Location: `frontend/stock-manager-app/src/app/components/dashboard`

Key tasks:
- Display key metrics using `dashboardService.getStats()`
- Show recent stock movements
- Display stock summary chart/cards
- Quick action buttons

### Products List Component
Location: `frontend/stock-manager-app/src/app/components/products/products-list`

Key tasks:
- Display products in a table/grid
- Search and filter functionality
- Sort by columns
- Navigate to add/edit product
- Delete product with confirmation
- Show low stock badge

### Product Form Component
Location: `frontend/stock-manager-app/src/app/components/products/product-form`

Key tasks:
- Create reactive form for product details
- Handle both add and edit modes (check route params)
- Call `productService.create()` or `productService.update()`
- Form validation
- Navigate back on success

### Stock Movements List Component
Location: `frontend/stock-manager-app/src/app/components/stock-movements/stock-movements-list`

Key tasks:
- Display movements in a table
- Filter by date range
- Filter by product
- Add new movement form/dialog
- Show movement type badges

### Main Layout Component
Location: `frontend/stock-manager-app/src/app/components/layout/main-layout`

Key tasks:
- Header with navigation
- Sidebar/menu with routes
- User info display
- Logout button
- `<router-outlet>` for child routes

## API Testing with Swagger

1. Start the backend API
2. Go to `http://localhost:5000/swagger`
3. Test authentication:
   - POST `/api/auth/register` - Create an account
   - POST `/api/auth/login` - Get a JWT token
   - Click "Authorize" button at the top
   - Enter: `Bearer YOUR_TOKEN_HERE`
   - Now you can test authenticated endpoints

## Database Management

### View Database
- **SQL Server Management Studio** (SSMS)
- **Azure Data Studio**
- **Visual Studio SQL Server Object Explorer**

Connection string: `Server=(localdb)\mssqllocaldb;Database=StockManagerDb;`

### Add New Migration

After modifying entities in `StockManager.Core/Entities`:

```bash
cd backend
dotnet ef migrations add YourMigrationName --project StockManager.Data --startup-project StockManager.API
dotnet ef database update --project StockManager.Data --startup-project StockManager.API
```

### Reset Database

```bash
cd backend
dotnet ef database drop --project StockManager.Data --startup-project StockManager.API
dotnet ef database update --project StockManager.Data --startup-project StockManager.API
```

## Environment Configuration

### Development
- Backend: `appsettings.Development.json` (auto-loaded in dev)
- Frontend: `environment.ts`

### Production
- Backend: `appsettings.json`
- Frontend: `environment.prod.ts`

## Common Tasks

### Add a New API Endpoint

1. Add method to service (`StockManager.API/Services/`)
2. Add controller action (`StockManager.API/Controllers/`)
3. Update frontend service (`frontend/src/app/services/`)
4. Call from component

### Add a New Entity

1. Create entity in `StockManager.Core/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Configure in `OnModelCreating`
4. Create DTO in `StockManager.Core/DTOs/`
5. Create migration and update database
6. Create repository interface and implementation
7. Add to service layer
8. Create API controller
9. Update frontend models and services

## Tips

- Use Angular Material components for consistent UI
- Handle loading states in components
- Show user feedback (success/error messages)
- Implement proper error handling
- Use reactive forms for validation
- Keep business logic in services, not components
- Follow the existing patterns in the codebase

## Next Steps

1. ✅ Backend API is complete and functional
2. ✅ Frontend structure is set up with routing
3. 🔨 Implement component logic and templates
4. 🔨 Add Angular Material UI
5. 🔨 Style the application
6. 🔨 Add form validation
7. 🔨 Implement error handling
8. 🔨 Add loading spinners
9. 🔨 Add success/error toasts
10. 🔨 Write tests

## Resources

- [Angular Documentation](https://angular.io/docs)
- [Angular Material](https://material.angular.io/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [RxJS](https://rxjs.dev/)

## Need Help?

Check the main README.md for troubleshooting and additional documentation.
