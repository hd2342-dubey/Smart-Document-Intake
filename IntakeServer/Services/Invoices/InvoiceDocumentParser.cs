using System.Globalization;
using System.Text.Json;
using IntakeServer.Exceptions;
using LibShared.Models.Invoices;

namespace IntakeServer.Services.Invoices;

/// <summary>
/// Extracts structured invoice data from uploaded text-based documents.
/// Supported formats: .json, .csv, .txt
/// </summary>
public static class InvoiceDocumentParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<InvoiceRequest> ParseAsync(IFormFile file)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        using var reader = new StreamReader(file.OpenReadStream());
        string content = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvoiceValidationException("The uploaded document is empty.");

        return extension switch
        {
            ".json" => ParseJson(content),
            ".csv" => ParseCsv(content),
            ".txt" => ParseText(content),
            _ => throw new InvoiceValidationException(
                $"Unsupported file type '{extension}'. Supported types: .json, .csv, .txt")
        };
    }

    /// <summary>
    /// JSON documents must match the InvoiceRequest shape:
    /// { "invoiceNumber": "...", "supplier": "...", "invoiceDate": "2026-01-15", "totalAmount": 100, "items": [...] }
    /// </summary>
    private static InvoiceRequest ParseJson(string content)
    {
        try
        {
            InvoiceRequest? request = JsonSerializer.Deserialize<InvoiceRequest>(content, JsonOptions);
            return request ?? throw new InvoiceValidationException("The JSON document could not be parsed.");
        }
        catch (JsonException ex)
        {
            throw new InvoiceValidationException($"Invalid JSON document: {ex.Message}");
        }
    }

    /// <summary>
    /// CSV documents use one row per line item with the invoice header repeated:
    /// InvoiceNumber,Supplier,InvoiceDate,Description,Quantity,UnitPrice
    /// </summary>
    private static InvoiceRequest ParseCsv(string content)
    {
        string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
            throw new InvoiceValidationException("The CSV document must contain a header row and at least one data row.");

        var request = new InvoiceRequest();

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string[] columns = lines[lineIndex].Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length < 6)
                throw new InvoiceValidationException(
                    $"CSV row {lineIndex + 1} is invalid. Expected columns: InvoiceNumber,Supplier,InvoiceDate,Description,Quantity,UnitPrice");

            if (lineIndex == 1)
            {
                request.InvoiceNumber = columns[0];
                request.Supplier = columns[1];
                request.InvoiceDate = ParseDate(columns[2]);
            }

            request.Items.Add(new InvoiceItemRequest
            {
                Description = columns[3],
                Quantity = ParseDecimal(columns[4], "quantity", lineIndex + 1),
                UnitPrice = ParseDecimal(columns[5], "unit price", lineIndex + 1)
            });
        }

        request.TotalAmount = request.Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice, 2));
        return request;
    }

    /// <summary>
    /// TXT documents use "Key: Value" lines for the header and pipe-separated item lines:
    /// InvoiceNumber: INV-001
    /// Supplier: Acme Corp
    /// InvoiceDate: 2026-01-15
    /// Item: Widget | 2 | 19.99
    /// </summary>
    private static InvoiceRequest ParseText(string content)
    {
        var request = new InvoiceRequest();
        string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string line in lines)
        {
            int separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            string key = line[..separatorIndex].Trim().ToLowerInvariant();
            string value = line[(separatorIndex + 1)..].Trim();

            switch (key)
            {
                case "invoicenumber" or "invoice number":
                    request.InvoiceNumber = value;
                    break;
                case "supplier":
                    request.Supplier = value;
                    break;
                case "invoicedate" or "invoice date" or "date":
                    request.InvoiceDate = ParseDate(value);
                    break;
                case "item":
                    request.Items.Add(ParseTextItem(value));
                    break;
            }
        }

        if (request.Items.Count == 0)
            throw new InvoiceValidationException(
                "The TXT document contains no line items. Expected lines like: Item: Description | Quantity | UnitPrice");

        request.TotalAmount = request.Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice, 2));
        return request;
    }

    private static InvoiceItemRequest ParseTextItem(string value)
    {
        string[] parts = value.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            throw new InvoiceValidationException(
                $"Invalid item line '{value}'. Expected format: Description | Quantity | UnitPrice");

        return new InvoiceItemRequest
        {
            Description = parts[0],
            Quantity = ParseDecimal(parts[1], "quantity", 0),
            UnitPrice = ParseDecimal(parts[2], "unit price", 0)
        };
    }

    private static DateOnly ParseDate(string value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out DateOnly date))
            return date;

        throw new InvoiceValidationException($"Invalid invoice date '{value}'. Expected format: yyyy-MM-dd");
    }

    private static decimal ParseDecimal(string value, string fieldName, int row)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
            return result;

        string location = row > 0 ? $" (row {row})" : string.Empty;
        throw new InvoiceValidationException($"Invalid {fieldName} value '{value}'{location}.");
    }
}
