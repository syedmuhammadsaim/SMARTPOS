using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SmartPOS.Application.Features.Auth.DTOs;
using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.Products.DTOs;

namespace SmartPOS.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _jwtToken;

    public string BaseUrl { get; set; } = "http://localhost:5000";
    public Guid CurrentTenantId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid CurrentBranchId { get; set; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public Guid CurrentCounterId { get; set; } = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public Guid CurrentSessionId { get; set; } = Guid.NewGuid();

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _http.DefaultRequestHeaders.Add("X-Tenant-Id", CurrentTenantId.ToString());
    }

    public void SetToken(string token)
    {
        _jwtToken = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
            if (!res.IsSuccessStatusCode) return null;
            var result = await res.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token != null) SetToken(result.Token);
            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductSearchResultDto?> ScanBarcodeAsync(string barcode)
    {
        try
        {
            var res = await _http.GetAsync($"/api/v1/products/scan/{Uri.EscapeDataString(barcode)}?branchId={CurrentBranchId}");
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<ProductSearchResultDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ProductSearchResultDto>> SearchProductsAsync(string query)
    {
        try
        {
            var res = await _http.GetAsync($"/api/v1/products/search?query={Uri.EscapeDataString(query)}&branchId={CurrentBranchId}");
            if (!res.IsSuccessStatusCode) return new List<ProductSearchResultDto>();
            return await res.Content.ReadFromJsonAsync<List<ProductSearchResultDto>>() ?? new List<ProductSearchResultDto>();
        }
        catch
        {
            return new List<ProductSearchResultDto>();
        }
    }

    public async Task<SaleOrderResponseDto?> SubmitOrderAsync(CreateSaleOrderDto request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/pos/orders", request);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<SaleOrderResponseDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> HoldBillAsync(HoldBillDto request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/pos/hold", request);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return json != null && json.TryGetValue("holdNumber", out var hn) ? hn : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> VerifyManagerPinAsync(Guid managerId, string pin, string actionCode, string reason)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/auth/verify-pin", new ManagerPinVerifyRequest(managerId, pin, actionCode, null, reason));
            if (!res.IsSuccessStatusCode) return false;
            var response = await res.Content.ReadFromJsonAsync<ManagerPinVerifyResponse>();
            return response?.IsAuthorized ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CounterSessionReportDto?> GetXReportAsync(Guid sessionId)
    {
        try
        {
            var res = await _http.GetAsync($"/api/v1/pos/sessions/{sessionId}/x-report");
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<CounterSessionReportDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<CounterSessionReportDto?> GetZReportAsync(Guid sessionId, decimal actualCash)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"/api/v1/pos/sessions/{sessionId}/z-report", actualCash);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<CounterSessionReportDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync()
    {
        try
        {
            var res = await _http.GetAsync("/api/v1/customers");
            if (!res.IsSuccessStatusCode) return new List<CustomerDto>();
            return await res.Content.ReadFromJsonAsync<List<CustomerDto>>() ?? new List<CustomerDto>();
        }
        catch
        {
            return new List<CustomerDto>();
        }
    }
}
