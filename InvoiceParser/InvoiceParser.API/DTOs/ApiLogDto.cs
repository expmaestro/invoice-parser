namespace InvoiceParser.DTOs
{
    public class ApiLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ApiProvider { get; set; } = string.Empty;
        public string? ModelVersion { get; set; }
        public long ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? ImageMimeType { get; set; }
        public UsageMetadataDto? UsageMetadata { get; set; }
    }

    public class UsageMetadataDto
    {
        public int TotalTokenCount { get; set; }
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int? ThoughtsTokenCount { get; set; }
        public List<PromptTokenDetailDto>? PromptTokensDetails { get; set; }
    }

    public class PromptTokenDetailDto
    {
        public string Modality { get; set; } = string.Empty;
        public int TokenCount { get; set; }
    }
}
