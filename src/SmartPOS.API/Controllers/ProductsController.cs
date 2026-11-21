using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Products;
using SmartPOS.Application.Features.Products.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<ProductSearchResultDto>>> Search([FromQuery] string query, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        var results = await _productService.SearchProductsAsync(query, branchId, ct);
        return Ok(results);
    }

    [HttpGet("scan/{barcode}")]
    public async Task<ActionResult<ProductSearchResultDto>> ScanBarcode(string barcode, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        var result = await _productService.ScanBarcodeAsync(barcode, branchId, ct);
        if (result == null)
            return NotFound(new { message = $"No active product matching barcode or SKU '{barcode}'" });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetProductByIdAsync(id, ct);
        if (product == null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(product);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var id = await _productService.CreateProductAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:guid}/batches")]
    public async Task<ActionResult<List<ProductBatchDto>>> GetBatches(Guid id, [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var batches = await _productService.GetProductBatchesAsync(id, warehouseId, ct);
        return Ok(batches);
    }
}
