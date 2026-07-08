using System.Net.Http.Json;
using System.Text.Json;
using LibShared.Models.Invoices;

namespace IntakeClient;

public class InvoiceApiClient(HttpClient http) : IInvoiceApiClient
{
    private readonly HttpClient _http = http;

    public async Task<SearchInvoiceResponse?> SearchAsync(string? invoiceNumber, string? supplier, DateOnly? dateFrom, DateOnly? dateTo, int page, int pageSize)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            query.Add($"invoiceNumber={Uri.EscapeDataString(invoiceNumber)}");
        }
        if (!string.IsNullOrWhiteSpace(supplier))
        {
            query.Add($"supplier={Uri.EscapeDataString(supplier)}");
        }
        if (dateFrom.HasValue)
        {
            query.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        }
        if (dateTo.HasValue)
        {
            query.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        }

        return await _http.GetFromJsonAsync<SearchInvoiceResponse>($"api/invoices?{string.Join("&", query)}");
    }

    public async Task<InvoiceResponse?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<InvoiceResponse>($"api/invoices/{id}");
    }

    public async Task<DashboardResponse?> GetDashboardAsync()
    {
        return await _http.GetFromJsonAsync<DashboardResponse>("api/invoices/dashboard");
    }

    public async Task<(InvoiceResponse? Invoice, string? Error)> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        using var content = BuildFileContent(fileStream, fileName, contentType);
        HttpResponseMessage response = await _http.PostAsync("api/invoices", content);

        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<InvoiceResponse>(), null);
        }

        return (null, await ReadErrorDetailAsync(response));
    }

    public async Task<(CompareInvoiceResponse? Result, string? Error)> CompareAsync(Stream fileStream, string fileName, string contentType)
    {
        using var content = BuildFileContent(fileStream, fileName, contentType);
        HttpResponseMessage response = await _http.PostAsync("api/invoices/compare", content);

        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<CompareInvoiceResponse>(), null);
        }

        return (null, await ReadErrorDetailAsync(response));
    }

    public async Task<bool> DeleteAsync(int id)
    {
        HttpResponseMessage response = await _http.DeleteAsync($"api/invoices/{id}");
        return response.IsSuccessStatusCode;
    }

    private static MultipartFormDataContent BuildFileContent(Stream fileStream, string fileName, string contentType)
    {
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        return new MultipartFormDataContent { { fileContent, "file", fileName } };
    }

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("detail").GetString() ?? $"Request failed ({(int)response.StatusCode})";
        }
        catch
        {
            return $"Request failed ({(int)response.StatusCode})";
        }
    }
}
