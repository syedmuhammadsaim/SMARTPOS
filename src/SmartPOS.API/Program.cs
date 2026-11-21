using Microsoft.AspNetCore.Identity;
using SmartPOS.Infrastructure;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure & EF Core DbContext
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add API Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
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
