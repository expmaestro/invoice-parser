namespace InvoiceParser.Models
{
    public class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}