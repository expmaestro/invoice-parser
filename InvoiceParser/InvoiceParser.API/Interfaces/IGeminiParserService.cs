using InvoiceParser.Models;

namespace InvoiceParser.Api.Interfaces
{
    public interface IGeminiParserService
    {
        Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream);
    }
}
