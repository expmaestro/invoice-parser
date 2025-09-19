using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InvoiceParser.Models
{
    public class ApiResponseLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("apiProvider")]
        public string ApiProvider { get; set; } = string.Empty; // "gemini" or "azure"

        [BsonElement("modelVersion")]
        public string? ModelVersion { get; set; }

        [BsonElement("requestPayload")]
        public string? RequestPayload { get; set; }

        [BsonElement("responseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        [BsonElement("usageMetadata")]
        public BsonDocument? UsageMetadata { get; set; }

        [BsonElement("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        [BsonElement("success")]
        public bool Success { get; set; }

        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }

        [BsonElement("fileName")]
        public string? FileName { get; set; }

        [BsonElement("fileSize")]
        public long? FileSize { get; set; }
    }
}
