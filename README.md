# rental-service

## Getting Started

Welcome to the team! Follow these steps to set up and run the project locally.

### 1. Prerequisites
- .NET 9 SDK (or higher)
- Visual Studio 2022 (or newer) with ASP.NET and web development workload
- **MySQL Server** (local or remote instance)

### 2. Clone the Repository
Clone this repository to your local machine.

### 3. Environment Variables
Copy `.env.example` to `.env` and update the values as needed:
- `DB_CONNECTION_STRING` – your **MySQL** connection string (see `appsettings.json` for format)
- `SMTP_*` – email settings for notifications (can use test values for local dev)
- `ADMIN_EMAIL` and `ADMIN_PASSWORD` – credentials for the initial admin user
- `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `AWS_BUCKET_NAME` – required for S3 file/image upload features

### 4. Database & S3 Setup
- Ensure your **MySQL** database is running and accessible.
- For file/image upload, you must have an AWS S3 bucket and credentials set in your `.env` and `appsettings.json`.

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
- `Services/S3Service.cs` – AWS S3 integration for file/image upload

### 8. Features
- Room listing, search, and booking requests
- Host and customer dashboards
- File/image upload to AWS S3
- Notifications system
- Chat/messages between users
- Contracts and reviews

### 9. Troubleshooting
- If you get database errors, check your connection string and that MySQL Server is running.
- If you change models, run `Add-Migration` and `Update-Database` to update the schema.

---