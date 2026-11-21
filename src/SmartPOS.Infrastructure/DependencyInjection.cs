using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Auth.Interfaces;
using SmartPOS.Application.Features.Communication;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.FBR;
using SmartPOS.Application.Features.ImportExport;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Application.Features.POS.Interfaces;
using SmartPOS.Application.Features.Products;
using SmartPOS.Application.Features.Purchasing;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;
using SmartPOS.Infrastructure.Services;

namespace SmartPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=SmartPOS_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPOSService, POSService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IFbrService, FbrService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

        services.AddDbContext<SmartPOSDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(SmartPOSDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            });
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<SmartPOSDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
