using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ProductsController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index(string? search, Guid? categoryId)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Barcodes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                     p.Sku.ToLower().Contains(term) ||
                                     p.Barcodes.Any(b => b.Barcode.ToLower().Contains(term)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        var stockBalances = await _context.StockBalances.ToListAsync();
        var stockMap = stockBalances.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.Sum(s => s.QtyOnHand));
        ViewBag.StockMap = stockMap;

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Units = await _context.Units.OrderBy(u => u.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name,
        string sku,
        string barcode,
        Guid categoryId,
        Guid? brandId,
        Guid unitId,
        decimal purchasePrice,
        decimal salePrice,
        decimal minStock,
        decimal openingStock)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        // Prevent duplicate SKU
        if (await _context.Products.AnyAsync(p => p.Sku == sku.Trim()))
        {
            ModelState.AddModelError("Sku", "A product with this SKU already exists.");
        }

        // Prevent duplicate Barcode
        if (!string.IsNullOrWhiteSpace(barcode) && await _context.ProductBarcodes.AnyAsync(b => b.Barcode == barcode.Trim()))
        {
            ModelState.AddModelError("Barcode", "This barcode is already assigned to another product.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Units = await _context.Units.OrderBy(u => u.Name).ToListAsync();
            return View();
        }

        var product = new Product
        {
            BusinessId = businessId.Value,
            Name = name.Trim(),
            Sku = sku.Trim().ToUpper(),
            CategoryId = categoryId,
            BrandId = brandId,
            UnitId = unitId,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            MinStock = minStock,
            Status = "Active"
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assign Barcode
        var barcodeStr = string.IsNullOrWhiteSpace(barcode) ? product.Sku : barcode.Trim();
        _context.ProductBarcodes.Add(new ProductBarcode
        {
            BusinessId = businessId.Value,
            ProductId = product.Id,
            Barcode = barcodeStr,
            IsPrimary = true
        });

        // Seed opening stock if specified
        if (openingStock > 0)
        {
            var defaultWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Status == "Active");
            if (defaultWarehouse != null)
            {
                _context.StockBalances.Add(new StockBalance
                {
                    BusinessId = businessId.Value,
                    WarehouseId = defaultWarehouse.Id,
                    ProductId = product.Id,
                    QtyOnHand = openingStock
                });

                _context.StockTransactions.Add(new StockTransaction
                {
                    BusinessId = businessId.Value,
                    WarehouseId = defaultWarehouse.Id,
                    ProductId = product.Id,
                    Type = Domain.Enums.StockTransactionType.OpeningStock,
                    Qty = openingStock,
                    UnitCost = purchasePrice,
                    ReferenceType = "OpeningStock",
                    ReferenceId = product.Id,
                    Note = "OPENING-STOCK"
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Product '{product.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> BarcodeLabels(Guid id, int count = 12)
    {
        var product = await _context.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        var business = await _context.Businesses.FindAsync(product.BusinessId);
        ViewBag.Business = business;
        ViewBag.Count = count > 0 && count <= 100 ? count : 12;

        return View(product);
    }
}
