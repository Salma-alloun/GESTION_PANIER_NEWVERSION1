using GESTION_PANIER.Data;
using GESTION_PANIER.Models.GESTION_PANIER.Models;
using GESTION_PANIER.Models.Session;
using GESTION_PANIER.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ======================
// BASE DE DONNÉES
// ======================
builder.Services.AddDbContext<GESTION_PANIERContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GESTION_PANIERContext")));

// ======================
// AUTHENTIFICATION PAR COOKIES
// ======================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
    });

// ======================
// AUTORISATION SELON ROLE
// ======================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin")); // <-- RequireRole au lieu de RequireClaim
});

// ======================
// SERVICES UTILES
// ======================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartSessionService>();
builder.Services.AddScoped<HuggingFaceLlmService>();
builder.Services.AddMemoryCache();


// ======================
// RAZOR PAGES
// ======================
builder.Services.AddRazorPages();

var app = builder.Build();

// ======================
// MIDDLEWARE
// ======================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // obligatoire avant Authorization
app.UseAuthorization();

app.MapRazorPages();

// ======================
// SEED ADMIN (optionnel)
// ======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GESTION_PANIERContext>();

    // Vérifier si l'admin existe
    if (!context.AppUsers.Any(u => u.Email == "admin@example.com"))
    {
        context.AppUsers.Add(new AppUser
        {
            Email = "admin@example.com",
            Password = "Admin123!", // ?? stocker un hash dans une vraie appli
            Role = "Admin"
        });
        context.SaveChanges();
    }
}

app.Run();
