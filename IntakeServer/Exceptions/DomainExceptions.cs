namespace IntakeServer.Exceptions;

/// <summary>
/// Thrown when an uploaded document cannot be parsed or fails business validation.
/// Mapped to HTTP 400 by the exception handling middleware.
/// </summary>
public class InvoiceValidationException(string message) : Exception(message);

/// <summary>
/// Thrown when an invoice with the same invoice number and supplier already exists.
/// Mapped to HTTP 409 by the exception handling middleware.
/// </summary>
public class DuplicateInvoiceException(string invoiceNumber, string supplier)
    : Exception($"Invoice '{invoiceNumber}' from supplier '{supplier}' already exists.");

/// <summary>
/// Thrown when a requested invoice does not exist.
/// Mapped to HTTP 404 by the exception handling middleware.
/// </summary>
public class InvoiceNotFoundException(int invoiceId)
    : Exception($"Invoice with id {invoiceId} was not found.");
