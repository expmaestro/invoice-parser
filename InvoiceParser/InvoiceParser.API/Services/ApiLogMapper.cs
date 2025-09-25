using MongoDB.Bson;
using InvoiceParser.Models;
using InvoiceParser.DTOs;
using System.Text.Json;

namespace InvoiceParser.Services
{
    public static class ApiLogMapper
    {
        public static ApiLogDto ToDto(ApiResponseLog log)
        {
            return new ApiLogDto
            {
                Id = log.Id,
                RequestId = log.RequestId,
                Timestamp = log.Timestamp,
                ApiProvider = log.ApiProvider,
                ModelVersion = log.ModelVersion,
                ProcessingTimeMs = log.ProcessingTimeMs,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage,
                FileName = log.FileName,
                FileSize = log.FileSize,
                ImageMimeType = log.ImageMimeType,
                UsageMetadata = MapUsageMetadata(log.UsageMetadata)
            };
        }

        public static ApiLogDetailDto ToDetailDto(ApiResponseLog log)
        {
            var dto = new ApiLogDetailDto
            {
                Id = log.Id,
                RequestId = log.RequestId,
                Timestamp = log.Timestamp,
                ApiProvider = log.ApiProvider,
                ModelVersion = log.ModelVersion,
                ProcessingTimeMs = log.ProcessingTimeMs,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage,
                FileName = log.FileName,
                FileSize = log.FileSize,
                ImageMimeType = log.ImageMimeType,
                UsageMetadata = MapUsageMetadata(log.UsageMetadata),
                RequestPayload = log.RequestPayload,
                ResponseContent = log.ResponseContent,
                ImageBase64 = log.ImageBase64
            };

            // Extract and parse the invoice data from the Gemini response
            dto.ParsedInvoice = ExtractParsedInvoice(log.ResponseContent);

            return dto;
        }

        private static ParsedInvoice? ExtractParsedInvoice(string? responseContent)
        {
            if (string.IsNullOrEmpty(responseContent)) return null;

            try
            {
                // First, try to parse the response as Gemini API response structure
                var geminiResponse = JsonSerializer.Deserialize<JsonDocument>(responseContent);
                
                if (geminiResponse?.RootElement.TryGetProperty("candidates", out var candidatesElement) == true &&
                    candidatesElement.ValueKind == JsonValueKind.Array &&
                    candidatesElement.GetArrayLength() > 0)
                {
                    var firstCandidate = candidatesElement[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var partsElement) &&
                        partsElement.ValueKind == JsonValueKind.Array &&
                        partsElement.GetArrayLength() > 0)
                    {
                        var firstPart = partsElement[0];
                        if (firstPart.TryGetProperty("text", out var textElement))
                        {
                            var textContent = textElement.GetString();
                            if (!string.IsNullOrEmpty(textContent))
                            {
                                // Extract JSON from the text content (same logic as in GeminiParserService)
                                var jsonStart = textContent.IndexOf('{');
                                var jsonEnd = textContent.LastIndexOf('}');
                                
                                if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
                                {
                                    var jsonResponse = textContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                                    
                                    // Parse the extracted JSON as invoice data
                                    var options = new JsonSerializerOptions 
                                    { 
                                        PropertyNameCaseInsensitive = true
                                    };
                                    
                                    return JsonSerializer.Deserialize<ParsedInvoice>(jsonResponse, options);
                                }
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the mapping
                Console.WriteLine($"Failed to extract parsed invoice from response: {ex.Message}");
                return null;
            }
        }

        private static UsageMetadataDto? MapUsageMetadata(BsonDocument? usageMetadata)
        {
            if (usageMetadata == null) return null;

            try
            {
                var dto = new UsageMetadataDto();

                if (usageMetadata.Contains("totalTokenCount"))
                    dto.TotalTokenCount = usageMetadata["totalTokenCount"].AsInt32;

                if (usageMetadata.Contains("promptTokenCount"))
                    dto.PromptTokenCount = usageMetadata["promptTokenCount"].AsInt32;

                if (usageMetadata.Contains("candidatesTokenCount"))
                    dto.CandidatesTokenCount = usageMetadata["candidatesTokenCount"].AsInt32;

                if (usageMetadata.Contains("thoughtsTokenCount"))
                    dto.ThoughtsTokenCount = usageMetadata["thoughtsTokenCount"].AsInt32;

                if (usageMetadata.Contains("promptTokensDetails") && usageMetadata["promptTokensDetails"].IsBsonArray)
                {
                    var details = new List<PromptTokenDetailDto>();
                    foreach (var detail in usageMetadata["promptTokensDetails"].AsBsonArray)
                    {
                        if (detail.IsBsonDocument)
                        {
                            var detailDoc = detail.AsBsonDocument;
                            details.Add(new PromptTokenDetailDto
                            {
                                Modality = detailDoc.Contains("modality") ? detailDoc["modality"].AsString : string.Empty,
                                TokenCount = detailDoc.Contains("tokenCount") ? detailDoc["tokenCount"].AsInt32 : 0
                            });
                        }
                    }
                    dto.PromptTokensDetails = details;
                }

                return dto;
            }
            catch (Exception)
            {
                // Return null if mapping fails
                return null;
            }
        }
    }
}
