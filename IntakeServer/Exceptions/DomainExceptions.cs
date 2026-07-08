namespace IntakeServer.Exceptions;

public class InvoiceValidationException(string message) : Exception(message);

public class DuplicateInvoiceException(string invoiceNumber, string supplier) : Exception($"Invoice '{invoiceNumber}' from supplier '{supplier}' already exists.");

public class InvoiceNotFoundException(int invoiceId) : Exception($"Invoice with id {invoiceId} was not found.");
