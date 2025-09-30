using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Constants;
using InvoiceParser.Models;

namespace InvoiceParser.Services
{
    public class OpenAIParserService : IOpenAIParserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIParserService> _logger;
        private readonly IApiResponseLogService _logService;

        public OpenAIParserService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIParserService> logger,
            IApiResponseLogService logService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
        }

        public async Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream)
        {
            string? generatedJson = null;
            
            try
            {
                // Convert stream to base64 for OpenAI API
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64Image = Convert.ToBase64String(imageBytes);

                // Create the prompt for OpenAI vision processing
                var prompt = PromptConstants.InvoiceParsingPrompt;

                // Create OpenAI API request with image
                var request = new
                {
                    model = "gpt-4o", // Using GPT-4 with vision
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = prompt },
                                new { 
                                    type = "image_url", 
                                    image_url = new { 
                                        url = $"data:image/jpeg;base64,{base64Image}",
                                        detail = "high"
                                    } 
                                }
                            }
                        }
                    },
                    temperature = 0.1,
                    max_tokens = 4000
                };

                // Serialize request
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set authorization header
                var apiKey = _configuration["OpenAI:ApiKey"];
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Make API call
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log API response
                var apiLog = new ApiResponseLog
                {
                    ApiProvider = "OpenAI",
                    RequestPayload = JsonSerializer.Serialize(request),
                    ResponseContent = responseContent,
                    Timestamp = DateTime.UtcNow,
                    Success = response.IsSuccessStatusCode,
                    ModelVersion = "gpt-4o",
                    ImageData = imageBytes,
                    ImageMimeType = "image/jpeg"
                };
                await _logService.SaveApiResponseAsync(apiLog);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenAI API error: {responseContent}");
                    throw new Exception($"OpenAI API error: {responseContent}");
                }

                // Parse OpenAI response
                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (openAIResponse?.Choices?.Length == 0)
                {
                    throw new Exception("No response from OpenAI API");
                }

                generatedJson = openAIResponse.Choices[0].Message.Content.Trim();
                
                // Clean up the response using shared utility
                generatedJson = AiResponseParser.CleanOpenAIContent(generatedJson);

                // Log the generated JSON for debugging
                _logger.LogInformation("Generated JSON from OpenAI: {Json}", generatedJson);

                // Parse the generated JSON using shared utility
                var parsedInvoice = AiResponseParser.ParseInvoiceJson(generatedJson);
                return parsedInvoice ?? throw new Exception("Failed to parse OpenAI response");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in OpenAI response. JSON: {Json}", generatedJson ?? "null");
                throw new Exception($"Invalid JSON response from OpenAI: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing invoice image with OpenAI");
                throw;
            }
        }
    }

    // OpenAI API response models
    public class OpenAIResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public OpenAIChoice[] Choices { get; set; } = Array.Empty<OpenAIChoice>();
        public OpenAIUsage Usage { get; set; } = new OpenAIUsage();
    }

    public class OpenAIChoice
    {
        public int Index { get; set; }
        public OpenAIMessage Message { get; set; } = new OpenAIMessage();
        public string? LogProbs { get; set; }
        public string FinishReason { get; set; } = string.Empty;
    }

    public class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
