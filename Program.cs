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
// Đăng ký IHttpClientFactory để dùng DI cho HttpClient
builder.Services.AddHttpClient();
// Đăng ký SmsService vào DI container để có thể inject vào controller nếu cần
// builder.Services.AddTransient<SmsService>(sp =>
//     new SmsService(
//         Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? "YOUR_SID",
//         Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? "YOUR_TOKEN",
//         Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE") ?? "+12000000000"
//     )
// );

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

    // Seed amenities: auto add if not exist by name
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var amenitiesList = new[]
    {
        "WiFi", "Máy lạnh", "Hệ thống sưởi", "Bếp", "Máy giặt", "Máy sấy", "Đậu xe miễn phí", "Hồ bơi", "Phòng gym", "Cho phép thú cưng", "Ban công riêng", "TV màn hình phẳng", "Bồn tắm", "Camera an ninh", "Bảo vệ 24/7", "Bàn làm việc", "Thang máy", "Két sắt", "Máy nước nóng", "Sân vườn"
    };
    foreach (var amenityName in amenitiesList)
    {
        if (!db.Amenities.Any(a => a.Name == amenityName))
        {
            db.Amenities.Add(new Amenity { Id = Guid.NewGuid(), Name = amenityName });
        }
    }
    await db.SaveChangesAsync();
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
