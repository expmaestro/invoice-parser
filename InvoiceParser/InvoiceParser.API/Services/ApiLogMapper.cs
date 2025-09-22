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
            return new ApiLogDetailDto
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
