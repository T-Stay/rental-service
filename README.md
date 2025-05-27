# rental-service

## Getting Started

Welcome to the team! Follow these steps to set up and run the project locally.

### 1. Prerequisites
- .NET 9 SDK (or higher)
- Visual Studio 2022 (or newer) with ASP.NET and web development workload
- SQL Server (local or remote instance)

### 2. Clone the Repository
Clone this repository to your local machine.

### 3. Environment Variables
Copy `.env.example` to `.env` and update the values as needed:
- `DB_CONNECTION_STRING` – your SQL Server connection string
- `SMTP_*` – email settings for notifications (can use test values for local dev)
- `ADMIN_EMAIL` and `ADMIN_PASSWORD` – credentials for the initial admin user

### 4. Database Setup
Open the solution in Visual Studio and open the **Package Manager Console**. Run:

```
Update-Database
```

This will create the database and apply all migrations.

### 5. Running the Project
- Set `RentalService` as the startup project.
- Press **F5** or click **Start Debugging**.

The app will launch at `https://localhost:xxxx`.

### 6. Default Admin User
On first run, the app seeds an admin user using the credentials from your `.env` file.

### 7. Project Structure
- `Controllers/` – MVC controllers
- `Models/` – Entity and view models
- `Data/` – EF Core DbContext
- `Views/` – Razor views
- `Migrations/` – EF Core migrations
- `wwwroot/` – Static files

### 8. Useful Docs
- See `Documents/` for database schema and entity design.
- See `MVP.md` for project goals and features.

### 9. Troubleshooting
- If you get database errors, check your connection string and that SQL Server is running.
- If you change models, run `Add-Migration` and `Update-Database` to update the schema.

---