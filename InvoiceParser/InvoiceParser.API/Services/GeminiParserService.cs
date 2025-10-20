using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Constants;
using InvoiceParser.Models;
using Microsoft.Extensions.Logging;

namespace InvoiceParser.Services
{

    public class GeminiParserService : IGeminiParserService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IApiResponseLogService _apiResponseLogService;
        private readonly ILogger<GeminiParserService> _logger;
        private readonly string _apiKey;

        public GeminiParserService(IConfiguration configuration, IHttpClientFactory httpClientFactory, IApiResponseLogService apiResponseLogService, ILogger<GeminiParserService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _apiResponseLogService = apiResponseLogService;
            _logger = logger;
            _apiKey = _configuration["Google:GeminiApiKey"] 
                ?? throw new ArgumentNullException("Google:GeminiApiKey is not configured");
        }

        public async Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            string? requestPayload = null;
            string? jsonResponse = null;
            
            try
            {
                // Convert image to base64 and capture image data
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var imageBase64 = Convert.ToBase64String(imageBytes);
                var imageSize = ms.Length;
                var imageMimeType = GetImageMimeType(imageBytes);

                var prompt = PromptConstants.InvoiceParsingPrompt;

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = "image/jpeg",
                                        data = imageBase64
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topP = 0.8,
                        topK = 40
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}");
                
                requestPayload = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(
                    requestPayload,
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

                // Save API response to MongoDB
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiResponseLog = new ApiResponseLog
                        {
                            RequestId = requestId,
                            Timestamp = startTime,
                            ApiProvider = "gemini",
                            ModelVersion = geminiResponse?.ModelVersion,
                            RequestPayload = requestPayload,
                            ResponseContent = responseContent,
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                            Success = true,
                            FileSize = imageSize,
                            ImageData = imageBytes,
                            ImageMimeType = imageMimeType,
                            ImageBase64 = imageBase64
                        };

                        // Add usage metadata as BSON document
                        if (geminiResponse?.UsageMetadata != null)
                        {
                            var usageMetadataDict = new Dictionary<string, object>
                            {
                                ["promptTokenCount"] = geminiResponse.UsageMetadata.PromptTokenCount,
                                ["candidatesTokenCount"] = geminiResponse.UsageMetadata.CandidatesTokenCount,
                                ["totalTokenCount"] = geminiResponse.UsageMetadata.TotalTokenCount
                            };

                            if (geminiResponse.UsageMetadata.ThoughtsTokenCount.HasValue)
                            {
                                usageMetadataDict["thoughtsTokenCount"] = geminiResponse.UsageMetadata.ThoughtsTokenCount.Value;
                            }

                            if (geminiResponse.UsageMetadata.PromptTokensDetails?.Any() == true)
                            {
                                usageMetadataDict["promptTokensDetails"] = geminiResponse.UsageMetadata.PromptTokensDetails
                                    .Select(ptd => new Dictionary<string, object>
                                    {
                                        ["modality"] = ptd.Modality ?? "",
                                        ["tokenCount"] = ptd.TokenCount
                                    }).ToList();
                            }

                            apiResponseLog.UsageMetadata = new MongoDB.Bson.BsonDocument(usageMetadataDict);
                        }

                        await _apiResponseLogService.SaveApiResponseAsync(apiResponseLog);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the main request
                        Console.WriteLine($"Failed to save API response to MongoDB: {ex.Message}");
                    }
                });

                // Extract the JSON response using shared utility
                var textContent = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;
                if (string.IsNullOrEmpty(textContent))
                {
                    throw new Exception("No text content found in Gemini response");
                }

                // Extract JSON from the text content using shared logic
                var jsonStart = textContent.IndexOf('{');
                var jsonEnd = textContent.LastIndexOf('}');

                if (jsonStart == -1 || jsonEnd == -1)
                {
                    throw new Exception("Could not find valid JSON in Gemini response");
                }

                jsonResponse = textContent.Substring(jsonStart, jsonEnd - jsonStart + 1);

                // Log the generated JSON for debugging
                _logger.LogInformation("Generated JSON from Gemini: {Json}", jsonResponse);

                // Parse the JSON response using shared utility
                var parsedInvoice = AiResponseParser.ParseInvoiceJson(jsonResponse);
                if (parsedInvoice == null)
                {
                    throw new Exception("Failed to parse Gemini JSON response");
                }

                // Add metadata from Gemini response
                if (geminiResponse?.UsageMetadata != null)
                {
                    parsedInvoice.UsageMetadata = new UsageMetadata
                    {
                        PromptTokenCount = geminiResponse.UsageMetadata.PromptTokenCount,
                        CandidatesTokenCount = geminiResponse.UsageMetadata.CandidatesTokenCount,
                        TotalTokenCount = geminiResponse.UsageMetadata.TotalTokenCount,
                        ThoughtsTokenCount = geminiResponse.UsageMetadata.ThoughtsTokenCount,
                        PromptTokensDetails = geminiResponse.UsageMetadata.PromptTokensDetails?.Select(ptd => new PromptTokenDetail
                        {
                            Modality = ptd.Modality,
                            TokenCount = ptd.TokenCount
                        }).ToList()
                    };
                }

                parsedInvoice.ModelVersion = geminiResponse?.ModelVersion;

                return parsedInvoice;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in Gemini response. JSON: {Json}", jsonResponse ?? "null");
                
                stopwatch?.Stop();
                
                // Save failed API response to MongoDB
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiResponseLog = new ApiResponseLog
                        {
                            RequestId = requestId,
                            Timestamp = startTime,
                            ApiProvider = "gemini",
                            RequestPayload = requestPayload,
                            ResponseContent = jsonEx.Message,
                            ProcessingTimeMs = stopwatch?.ElapsedMilliseconds ?? 0,
                            Success = false,
                            ErrorMessage = jsonEx.ToString()
                        };

                        await _apiResponseLogService.SaveApiResponseAsync(apiResponseLog);
                    }
                    catch (Exception logEx)
                    {
                        // Log error but don't fail the main request
                        Console.WriteLine($"Failed to save failed API response to MongoDB: {logEx.Message}");
                    }
                });

                throw new Exception($"Invalid JSON response from Gemini: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                
                // Save failed API response to MongoDB
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiResponseLog = new ApiResponseLog
                        {
                            RequestId = requestId,
                            Timestamp = startTime,
                            ApiProvider = "gemini",
                            RequestPayload = requestPayload,
                            ResponseContent = ex.Message,
                            ProcessingTimeMs = stopwatch?.ElapsedMilliseconds ?? 0,
                            Success = false,
                            ErrorMessage = ex.ToString()
                        };

                        await _apiResponseLogService.SaveApiResponseAsync(apiResponseLog);
                    }
                    catch (Exception logEx)
                    {
                        // Log error but don't fail the main request
                        Console.WriteLine($"Failed to save failed API response to MongoDB: {logEx.Message}");
                    }
                });

                throw new Exception($"Error processing invoice with Gemini: {ex.Message}", ex);
            }
        }

        private static string GetImageMimeType(byte[] imageBytes)
        {
            if (imageBytes.Length < 4)
                return "application/octet-stream";

            // Check for common image file signatures
            // PNG: 89 50 4E 47
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return "image/png";

            // JPEG: FF D8 FF
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                return "image/jpeg";

            // GIF: 47 49 46 38
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x38)
                return "image/gif";

            // WebP: 52 49 46 46 (RIFF) and at offset 8: 57 45 42 50 (WEBP)
            if (imageBytes.Length >= 12 && 
                imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46 &&
                imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
                return "image/webp";

            // BMP: 42 4D
            if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                return "image/bmp";

            // Default to JPEG if no match (most common for uploads)
            return "image/jpeg";
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }

            [JsonPropertyName("usageMetadata")]
            public GeminiUsageMetadata UsageMetadata { get; set; }

            [JsonPropertyName("modelVersion")]
            public string ModelVersion { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        // Request models
        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public RequestContent[] Contents { get; set; }

            [JsonPropertyName("generationConfig")]
            public GenerationConfig GenerationConfig { get; set; }
        }

        private class RequestContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("parts")]
            public IPart[] Parts { get; set; }
        }

        private interface IPart { }

        private class TextPart : IPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class InlineDataPart : IPart
        {
            [JsonPropertyName("inline_data")]
            public InlineData InlineData { get; set; }
        }

        private class InlineData
        {
            [JsonPropertyName("mime_type")]
            public string MimeType { get; set; }

            [JsonPropertyName("data")]
            public string Data { get; set; }
        }

        private class GenerationConfig
        {
            [JsonPropertyName("temperature")]
            public float Temperature { get; set; }

            [JsonPropertyName("topP")]
            public float TopP { get; set; }

            [JsonPropertyName("topK")]
            public int TopK { get; set; }
        }

        private class GeminiUsageMetadata
        {
            [JsonPropertyName("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonPropertyName("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }

            [JsonPropertyName("totalTokenCount")]
            public int TotalTokenCount { get; set; }

            [JsonPropertyName("promptTokensDetails")]
            public List<GeminiPromptTokenDetail> PromptTokensDetails { get; set; }

            [JsonPropertyName("thoughtsTokenCount")]
            public int? ThoughtsTokenCount { get; set; }
        }

        private class GeminiPromptTokenDetail
        {
            [JsonPropertyName("modality")]
            public string Modality { get; set; }

            [JsonPropertyName("tokenCount")]
            public int TokenCount { get; set; }
        }

        public async Task<string> GenerateTextAsync(string prompt)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            string? requestPayload = null;
            string? jsonResponse = null;
            
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 1000
                    }
                };

                requestPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });

                var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
                //  var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);
                jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API request failed: {StatusCode}, Response: {Response}", response.StatusCode, jsonResponse);
                    throw new Exception($"Gemini API request failed: {response.StatusCode}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);
                
                if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text is string text)
                {
                    return text;
                }

                throw new Exception("No valid response from Gemini API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateTextAsync");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await _apiResponseLogService.SaveApiResponseAsync(new Models.ApiResponseLog
                {
                    RequestId = requestId,
                    Timestamp = startTime,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    RequestPayload = requestPayload,
                    ResponseContent = jsonResponse ?? "",
                    ApiProvider = "Gemini",
                    ModelVersion = "gemini-1.5-flash",
                    Success = jsonResponse != null
                });
            }
        }
    }
}
