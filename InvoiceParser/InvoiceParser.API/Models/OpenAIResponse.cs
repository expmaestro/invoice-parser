namespace InvoiceParser.Models
{
    public class OpenAIResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public OpenAIChoice[] Choices { get; set; } = Array.Empty<OpenAIChoice>();
        public OpenAIUsage Usage { get; set; } = new OpenAIUsage();
    }
}