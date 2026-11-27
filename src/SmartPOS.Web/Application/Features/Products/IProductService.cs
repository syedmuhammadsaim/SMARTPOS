using SmartPOS.Application.Features.Products.DTOs;

namespace SmartPOS.Application.Features.Products;

public interface IProductService
{
    Task<List<ProductSearchResultDto>> SearchProductsAsync(string query, Guid? branchId = null, CancellationToken ct = default);
    Task<ProductSearchResultDto?> ScanBarcodeAsync(string barcode, Guid? branchId = null, CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<bool> UpdateProductPriceAsync(Guid id, decimal newSalePrice, decimal newPurchasePrice, string changedBy, CancellationToken ct = default);
    Task<List<ProductBatchDto>> GetProductBatchesAsync(Guid productId, Guid? warehouseId = null, CancellationToken ct = default);
}
