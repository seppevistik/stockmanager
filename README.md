# Stock Manager - Inventory Management Application

A full-stack inventory management system built with Angular and ASP.NET Core, designed for businesses to efficiently track, manage, and monitor their product stock.

## 🚀 Tech Stack

### Backend
- **ASP.NET Core 8.0** - Web API
- **Entity Framework Core 8.0** - ORM with SQL Server
- **ASP.NET Core Identity** - User authentication and authorization
- **JWT** - Token-based authentication
- **AutoMapper** - Object mapping
- **Swagger/OpenAPI** - API documentation

### Frontend
- **Angular 18** - Frontend framework
- **Angular Material** - UI component library
- **RxJS** - Reactive programming
- **TypeScript** - Type-safe JavaScript

## 📋 Features

### Phase 1 - MVP (Current Implementation)

#### Authentication & Authorization
- User registration with business creation
- Login with JWT token-based authentication
- Role-based access control (Admin, Manager, Staff, Viewer)
- Secure password requirements
- Token refresh and auto-logout

#### Product Management
- Add, edit, view, and delete products
- SKU (Stock Keeping Unit) management
- Product categories
- Minimum stock level configuration
- Cost per unit tracking
- Current stock display
- Product status (Active/Inactive)
- Location tracking

#### Inventory Tracking
- Real-time stock levels
- Stock movements tracking:
  - **Stock In**: Record incoming inventory
  - **Stock Out**: Record outgoing inventory
  - **Stock Transfer**: Move inventory between locations
  - **Stock Adjustment**: Manual corrections
- Movement history with user attribution
- Automatic stock calculation

#### Dashboard
- Overview of key metrics:
  - Total products count
  - Total inventory value
  - Low stock items count
  - Out of stock items count
- Recent stock movements
- Stock summary (in stock, low stock, out of stock)
- Quick access to key features

#### Reporting
- Current stock levels report
- Low stock products report
- Stock movement history
- Export capabilities (planned)

## 🏗️ Project Structure

```
stockmanager/
├── backend/
│   ├── StockManager.API/          # Web API project
│   │   ├── Controllers/           # API controllers
│   │   ├── Services/              # Business logic services
│   │   ├── Mapping/               # AutoMapper profiles
│   │   └── Program.cs             # Application configuration
│   ├── StockManager.Core/         # Domain layer
│   │   ├── Entities/              # Domain entities
│   │   ├── Enums/                 # Enumerations
│   │   ├── DTOs/                  # Data transfer objects
│   │   └── Interfaces/            # Repository interfaces
│   ├── StockManager.Data/         # Data access layer
│   │   ├── Contexts/              # EF DbContext
│   │   ├── Repositories/          # Repository implementations
│   │   └── Migrations/            # EF migrations
│   └── StockManager.sln           # Solution file
│
├── frontend/
│   └── stock-manager-app/         # Angular application
│       └── src/
│           └── app/
│               ├── models/        # TypeScript interfaces
│               ├── services/      # API services
│               ├── guards/        # Route guards
│               └── components/    # UI components
│                   ├── auth/      # Login, Register
│                   ├── dashboard/ # Dashboard
│                   ├── products/  # Product management
│                   ├── stock-movements/
│                   └── layout/    # Layout components
│
└── docs/                          # Documentation
```

## 🔧 Setup Instructions

### Prerequisites
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **SQL Server** or **SQL Server LocalDB**
- **Angular CLI**: `npm install -g @angular/cli`
- **EF Core Tools**: `dotnet tool install --global dotnet-ef`

### Backend Setup

1. **Navigate to backend directory**
   ```bash
   cd backend
   ```

2. **Update database connection string**

   Edit `StockManager.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StockManagerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

   For Docker SQL Server, use:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=StockManagerDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true;"
     }
   }
   ```

3. **Update JWT secret key (Important for production!)**

   Edit `StockManager.API/appsettings.json`:
   ```json
   {
     "Jwt": {
       "SecretKey": "YOUR_SECURE_SECRET_KEY_HERE_AT_LEAST_32_CHARS",
       "Issuer": "StockManagerAPI",
       "Audience": "StockManagerApp"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update --project StockManager.Data --startup-project StockManager.API
   ```

5. **Run the API**
   ```bash
   cd StockManager.API
   dotnet run
   ```

   API will be available at: `https://localhost:5001` or `http://localhost:5000`

   Swagger UI: `http://localhost:5000/swagger`

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd frontend/stock-manager-app
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Update API URL** (if different from default)

   Update API URL in all service files if your backend runs on a different port:
   - `src/app/services/auth.service.ts`
   - `src/app/services/product.service.ts`
   - `src/app/services/stock-movement.service.ts`
   - `src/app/services/dashboard.service.ts`

4. **Run the application**
   ```bash
   ng serve
   ```

   Application will be available at: `http://localhost:4200`

## 🎯 Usage

### Getting Started

1. **Register a new account**
   - Go to `http://localhost:4200/register`
   - Fill in your details and business name
   - Select your role (Admin recommended for first user)
   - Submit registration

2. **Login**
   - Use your email and password to login
   - You'll be redirected to the dashboard

3. **Add your first product**
   - Navigate to Products
   - Click "Add Product"
   - Fill in product details:
     - Name, SKU, Description
     - Initial stock quantity
     - Cost per unit
     - Minimum stock level (for alerts)
   - Submit

4. **Record stock movements**
   - Navigate to Stock Movements
   - Click "Add Movement"
   - Select product, movement type, and quantity
   - Add reason/notes if needed
   - Submit

### User Roles

- **Admin**: Full access to all features including user management
- **Manager**: Manage inventory, view reports, manage products
- **Staff**: Add/update products and stock entries
- **Viewer**: Read-only access to inventory and reports

## 🔒 API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user and business
- `POST /api/auth/login` - Login and get JWT token

### Products
- `GET /api/products` - Get all products for business
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/low-stock` - Get low stock products
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Stock Movements
- `GET /api/stockmovements` - Get all movements
- `GET /api/stockmovements/product/{id}` - Get movements for product
- `GET /api/stockmovements/recent` - Get recent movements
- `POST /api/stockmovements` - Create new movement

### Dashboard
- `GET /api/dashboard/stats` - Get dashboard statistics
- `GET /api/dashboard/stock-summary` - Get stock summary
- `GET /api/dashboard/recent-activity` - Get recent activities

## 🔐 Security Features

- JWT token-based authentication
- Password hashing with ASP.NET Core Identity
- Role-based authorization
- CORS configuration for frontend
- HTTP-only cookies (configurable)
- Token expiration and refresh
- Automatic logout on unauthorized access

## 📊 Database Schema

### Key Tables

- **AspNetUsers** - Extended with business context and role
- **Businesses** - Multi-tenant business data
- **Products** - Product catalog
- **Categories** - Product categories
- **StockMovements** - Inventory transaction history

All tables include:
- Soft delete support (`IsDeleted`)
- Audit fields (`CreatedAt`, `UpdatedAt`)
- Business isolation (multi-tenancy)

## 🚧 Future Enhancements (Phase 2+)

### Planned Features
- [ ] Barcode/QR code scanning
- [ ] Purchase order management
- [ ] Supplier management module
- [ ] Multi-location/warehouse support
- [ ] Batch/lot tracking
- [ ] Expiry date management
- [ ] Advanced analytics and forecasting
- [ ] Email notifications
- [ ] PDF report generation
- [ ] Excel/CSV import/export
- [ ] Mobile responsive improvements
- [ ] API rate limiting
- [ ] Audit logging
- [ ] User activity tracking
- [ ] Integration APIs (accounting, e-commerce)

## 🧪 Testing

### Backend Testing
```bash
cd backend
dotnet test
```

### Frontend Testing
```bash
cd frontend/stock-manager-app
ng test
```

## 📝 Development Notes

### Adding New Migrations

After modifying entities:
```bash
cd backend
dotnet ef migrations add MigrationName --project StockManager.Data --startup-project StockManager.API
dotnet ef database update --project StockManager.Data --startup-project StockManager.API
```

### Building for Production

**Backend**:
```bash
cd backend/StockManager.API
dotnet publish -c Release -o ./publish
```

**Frontend**:
```bash
cd frontend/stock-manager-app
ng build --configuration production
```

## 🐛 Troubleshooting

### Common Issues

**Database connection fails**
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure migrations are applied

**CORS errors**
- Check `AllowAngularApp` policy in Program.cs
- Verify frontend URL matches CORS configuration

**JWT token errors**
- Ensure JWT secret key is at least 32 characters
- Check token expiration settings
- Verify Issuer and Audience match

**Port conflicts**
- Backend default: 5000/5001
- Frontend default: 4200
- Change in launchSettings.json (backend) or angular.json (frontend)

## 📄 License

This project is developed as part of an inventory management solution.

## 👥 Contributing

1. Create a feature branch
2. Make your changes
3. Submit a pull request

## 📞 Support

For issues and questions, please use the issue tracker or contact the development team.

---

**Built with ❤️ using Angular and ASP.NET Core**
