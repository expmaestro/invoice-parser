using InvoiceParser.Models;

namespace InvoiceParser.DTOs
{
    public class ApiLogDetailDto : ApiLogDto
    {
        public string? RequestPayload { get; set; }
        public string ResponseContent { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
        public ParsedInvoice? ParsedInvoice { get; set; }
    }
}
