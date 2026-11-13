using ArchivePlayer.Data;
using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<TMDBService>();
builder.Services.AddScoped<ArchiveSearchService>();
builder.Services.AddSingleton<SettingsService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string dbPath = GetSafeDatabasePath();
    string connectionString = $"Data Source={dbPath};";
    options.UseSqlite(connectionString);
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

var app = builder.Build();

// Automatically create/migrate the database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

string GetSafeDatabasePath()
{
    string localDbPath = Path.Combine(AppContext.BaseDirectory, "archiveplayer.db");
    if (File.Exists(localDbPath))
    {
        return localDbPath;
    }

    string appDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArchivePlayer"
    );

    Directory.CreateDirectory(appDataFolder);

    return Path.Combine(appDataFolder, "archiveplayer.db");
}