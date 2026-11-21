using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SmartPOS.Infrastructure;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure & EF Core DbContext
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "SmartPOS_Enterprise_Super_Secret_Key_For_Jwt_Authentication_2026_!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SmartPOS.API",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "SmartPOS.Clients",
        ClockSkew = TimeSpan.Zero
    };
});

// Add API Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-initialize and seed DB schema if available
await DbInitializer.EnsureDatabaseCreatedAndSeededAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check / System status endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    System = "SMARTPOS Enterprise API",
    Version = "2.0.0",
    TimestampUtc = DateTime.UtcNow
}));

// Automatic database migration & default seeding in development
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SmartPOSDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedSystemDefaultsAsync(context, roleManager, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
