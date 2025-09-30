using InvoiceParser.Models;

namespace InvoiceParser.Api.Interfaces
{
    public interface IOpenAIParserService
    {
        Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream);
    }
}
