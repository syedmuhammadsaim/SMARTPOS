namespace SmartPOS.Application.Features.ImportExport;

public class ProductImportRowDto
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Barcode { get; set; }
    public string CategoryName { get; set; } = "General";
    public string UnitName { get; set; } = "Piece";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal OpeningStock { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ImportResultDto
{
    public Guid JobId { get; set; }
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}

public interface IImportService
{
    Task<ImportResultDto> ImportProductsFromCsvAsync(Stream csvStream, Guid warehouseId, CancellationToken ct = default);
}
