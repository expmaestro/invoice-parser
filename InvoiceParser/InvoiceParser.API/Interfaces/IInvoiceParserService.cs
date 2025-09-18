using InvoiceParser.Models;

namespace InvoiceParser.Api.Interfaces
{
    public interface IInvoiceParserService
    {
        Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream);
    }
}
