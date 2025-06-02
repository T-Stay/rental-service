using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using dotenv.net;
using Pomelo.EntityFrameworkCore.MySql;
using RentalService.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if present
DotEnv.Load();

// Get connection string from environment variable or appsettings.json
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
// Register S3Service for dependency injection
builder.Services.AddSingleton<S3Service>();

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    string[] roles = ["customer", "host", "admin", "consultant", "guest"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
    // Seed admin user
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new Admin
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "Admin",
                AvatarUrl = "",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.ConfirmEmailAsync(adminUser, await userManager.GenerateEmailConfirmationTokenAsync(adminUser));
                await userManager.AddToRoleAsync(adminUser, UserRoleHelper.ToIdentityRoleString(UserRole.Admin));
            }
        }
    }

    // Seed amenities if empty
    if (!scope.ServiceProvider.GetRequiredService<AppDbContext>().Amenities.Any())
    {
        var amenities = new[]
        {
            new Amenity { Id = Guid.NewGuid(), Name = "WiFi" },
            new Amenity { Id = Guid.NewGuid(), Name = "Air Conditioning" },
            new Amenity { Id = Guid.NewGuid(), Name = "Heating" },
            new Amenity { Id = Guid.NewGuid(), Name = "Kitchen" },
            new Amenity { Id = Guid.NewGuid(), Name = "Washer" },
            new Amenity { Id = Guid.NewGuid(), Name = "Dryer" },
            new Amenity { Id = Guid.NewGuid(), Name = "Free Parking" },
            new Amenity { Id = Guid.NewGuid(), Name = "Pool" },
            new Amenity { Id = Guid.NewGuid(), Name = "Gym" },
            new Amenity { Id = Guid.NewGuid(), Name = "Pet Friendly" }
        };
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Amenities.AddRange(amenities);
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
