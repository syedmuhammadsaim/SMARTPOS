using System.IO;
using System.Text.Json;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.Products.DTOs;

namespace SmartPOS.Desktop.Services;

/// <summary>
/// Manages offline transaction spooling and local catalog caching for uninterrupted POS checkout.
/// When network drops, sales are queued locally and automatically synced when connection resumes.
/// </summary>
public class OfflineQueueService
{
    private readonly string _storageDir;
    private readonly string _ordersFilePath;
    private readonly string _catalogFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event Action<int>? QueueCountChanged;
    public event Action<string>? SyncStatusChanged;

    public OfflineQueueService(string? baseDirectory = null)
    {
        _storageDir = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartPOS",
            "OfflineStorage");

        Directory.CreateDirectory(_storageDir);
        _ordersFilePath = Path.Combine(_storageDir, "queued_orders.json");
        _catalogFilePath = Path.Combine(_storageDir, "cached_catalog.json");
    }

    public async Task EnqueueOfflineOrderAsync(CreateSaleOrderDto order, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var orders = await ReadQueuedOrdersInternalAsync(ct);
            orders.Add(order);
            await File.WriteAllTextAsync(_ordersFilePath, JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true }), ct);
            QueueCountChanged?.Invoke(orders.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<CreateSaleOrderDto>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await ReadQueuedOrdersInternalAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        var orders = await GetPendingOrdersAsync(ct);
        return orders.Count;
    }

    public async Task<int> SyncPendingOrdersAsync(ApiClient apiClient, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        int syncedCount = 0;
        try
        {
            var orders = await ReadQueuedOrdersInternalAsync(ct);
            if (orders.Count == 0) return 0;

            SyncStatusChanged?.Invoke($"Syncing {orders.Count} offline transactions...");
            var remaining = new List<CreateSaleOrderDto>();

            foreach (var order in orders)
            {
                try
                {
                    var response = await apiClient.SubmitOrderAsync(order);
                    if (response != null)
                    {
                        syncedCount++;
                    }
                    else
                    {
                        // Network still flaky or server error, retain order in offline queue
                        remaining.Add(order);
                    }
                }
                catch
                {
                    remaining.Add(order);
                }
            }

            await File.WriteAllTextAsync(_ordersFilePath, JsonSerializer.Serialize(remaining, new JsonSerializerOptions { WriteIndented = true }), ct);
            QueueCountChanged?.Invoke(remaining.Count);
            SyncStatusChanged?.Invoke(remaining.Count == 0 ? "All offline transactions synced successfully!" : $"{remaining.Count} offline orders waiting for sync.");
            return syncedCount;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveCachedCatalogAsync(IEnumerable<ProductSearchResultDto> products, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_catalogFilePath, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProductSearchResultDto?> FindCachedProductByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var catalog = await GetCachedCatalogAsync(ct);
        return catalog.FirstOrDefault(p =>
            string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase) ||
            p.SKU.Equals(barcode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ProductSearchResultDto>> SearchCachedCatalogAsync(string query, CancellationToken ct = default)
    {
        var catalog = await GetCachedCatalogAsync(ct);
        if (string.IsNullOrWhiteSpace(query)) return catalog;

        return catalog.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.SKU.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (p.Barcode != null && p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    private async Task<List<ProductSearchResultDto>> GetCachedCatalogAsync(CancellationToken ct)
    {
        if (!File.Exists(_catalogFilePath)) return new List<ProductSearchResultDto>();
        try
        {
            var json = await File.ReadAllTextAsync(_catalogFilePath, ct);
            return JsonSerializer.Deserialize<List<ProductSearchResultDto>>(json) ?? new List<ProductSearchResultDto>();
        }
        catch
        {
            return new List<ProductSearchResultDto>();
        }
    }

    private async Task<List<CreateSaleOrderDto>> ReadQueuedOrdersInternalAsync(CancellationToken ct)
    {
        if (!File.Exists(_ordersFilePath)) return new List<CreateSaleOrderDto>();
        try
        {
            var json = await File.ReadAllTextAsync(_ordersFilePath, ct);
            return JsonSerializer.Deserialize<List<CreateSaleOrderDto>>(json) ?? new List<CreateSaleOrderDto>();
        }
        catch
        {
            return new List<CreateSaleOrderDto>();
        }
    }
}
