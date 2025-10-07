namespace InvoiceParser.Models
{
    public class OpenAIChoice
    {
        public int Index { get; set; }
        public OpenAIMessage Message { get; set; } = new OpenAIMessage();
        public string? LogProbs { get; set; }
        public string FinishReason { get; set; } = string.Empty;
    }
}