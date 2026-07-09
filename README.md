# Smart Document Intake

A .NET solution for uploading invoice documents (JSON / CSV / TXT), extracting structured invoice data, validating it, storing it in PostgreSQL, and browsing it through a Blazor WebAssembly UI.

## Architecture

```
IntakeWeb (Blazor WebAssembly, MudBlazor)
        |
IntakeClient (typed HTTP API client)
        |
IntakeServer (ASP.NET Core Web API)
   Controller -> Service -> Repository (EF Core)
        |
PostgreSQL
```

## Projects

| Project | Type | Purpose |
|---|---|---|
| `LibShared` | Class library | Shared entities and DTOs (`Invoice`, `InvoiceItem`, request/response models, search filter) |
| `IntakeDatabase` | SQL scripts | Plain `CREATE TABLE` scripts for the invoice schema |
| `IntakeServer` | ASP.NET Core Web API | `InvoicesController`, `InvoiceService` + `InvoiceDocumentParser`, `InvoiceRepository` (EF Core), `IntakeDbContext` with Fluent API configurations, exception middleware, Swagger, health checks |
| `IntakeClient` | Class library | `InvoiceApiClient` — typed HTTP client consumed by the UI |
| `IntakeWeb` | Blazor WebAssembly | Invoice search/upload/detail/compare pages and dashboard stats |

Solution file: `SmartDocumentIntake.sln`

## Prerequisites

- .NET 10 SDK
- A PostgreSQL database

## Setup

1. **Connection string** — configure it in one of two ways:

   **Option A (recommended): a `.env` file** in the `IntakeServer/` folder (git-ignored, so credentials are never committed):

   ```
   ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;
   ```

   Note the double underscore (`__`) — that is how .NET maps environment variables to the `ConnectionStrings:DefaultConnection` configuration key. The file is loaded at startup by `Program.cs`.

   **Option B: `IntakeServer/appsettings.json`** (or `appsettings.Development.json`):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;"
     }
   }
   ```

   The application fails fast at startup with a clear error message if no connection string is configured.

   The DbContext is registered via dependency injection in `IntakeServer/Program.cs`:

   ```csharp
   builder.Services.AddDbContext<IntakeDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

2. **Schema** — run `IntakeDatabase/Invoices/TABLES/CREATE_TABLES.sql` against your database. It creates `tracker_schema.invoices` and `tracker_schema.invoice_items` with a unique index on (`invoice_number`, `supplier`).

3. **Run the API**:

   ```bash
   cd IntakeServer
   dotnet run
   # Swagger UI at /swagger (Development), health probe at /health
   ```

4. **Point the UI at the API** in `IntakeWeb/wwwroot/appsettings.json`:

   ```json
   { "ApiBaseUrl": "http://localhost:5226/" }
   ```

   Use whatever URL the API prints on startup, and make sure that origin is listed in the CORS policy in `IntakeServer/Program.cs`.

5. **Run the UI**:

   ```bash
   cd IntakeWeb
   dotnet run
   ```

## API Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/invoices` | Upload an invoice document (multipart form, field `file`). Parses, validates, and stores it. `201` on success, `409` on duplicate, `400` on parse/validation errors. |
| `GET` | `/api/invoices` | Search invoices. Query: `invoiceNumber`, `supplier`, `dateFrom`, `dateTo`, `page`, `pageSize`. |
| `GET` | `/api/invoices/{id}` | Invoice detail with line items. `404` if not found. |
| `DELETE` | `/api/invoices/{id}` | Delete an invoice (cascades to items). |
| `GET` | `/api/invoices/dashboard` | Totals, averages, top suppliers, recent invoices. |
| `POST` | `/api/invoices/compare` | Upload a document and compare it against the stored invoice with the same number + supplier; returns field-level and line-item differences. |
| `GET` | `/health` | Health check including database connectivity. |

## Supported Document Formats

**JSON**

```json
{
  "invoiceNumber": "INV-1001",
  "supplier": "Acme Office Supplies",
  "invoiceDate": "2026-06-15",
  "totalAmount": 335.50,
  "items": [
    { "description": "A4 Paper (box)", "quantity": 10, "unitPrice": 25.00, "lineTotal": 250.00 }
  ]
}
```

**CSV** (one row per line item; invoice header columns repeated; `TotalAmount` computed from line totals when omitted)

```csv
InvoiceNumber,Supplier,InvoiceDate,Description,Quantity,UnitPrice
INV-2002,Globex Logistics,2026-07-01,Freight - June,1,900.00
INV-2002,Globex Logistics,2026-07-01,Fuel Surcharge,1,300.00
```

**TXT** — `key: value` header lines plus line items as `description | quantity | unitPrice`.

## Validation Rules

- Invoice number, supplier, and date are required.
- Invoice date cannot be in the future.
- Total amount must be positive and match the sum of line totals (small rounding tolerance).
- Each line item requires a description, positive quantity, and non-negative unit price.
- Duplicate uploads (same invoice number + supplier) are rejected with `409 Conflict` (also enforced by a unique index).

## Error Handling

`ExceptionHandlingMiddleware` translates domain exceptions to HTTP responses in one place:

| Exception | Status |
|---|---|
| `ValidationException`, `DocumentParseException` | 400 |
| `NotFoundException` | 404 |
| `DuplicateInvoiceException` | 409 |
| Anything else | 500 |

## Design Decisions

- **No over-engineering**: one repository, one service, one parser class. No generic repository, no MediatR, no AutoMapper — mapping is a few explicit lines.
- **Clear layering**: UI never talks to the database; every call flows UI -> `IntakeClient` -> Controller -> Service -> Repository -> DbContext.
- **Parsing at the service boundary**: `InvoiceDocumentParser` converts raw uploads into an `InvoiceRequest`; the service validates and persists. The parser is a plain class, easy to extend with new formats.
  
