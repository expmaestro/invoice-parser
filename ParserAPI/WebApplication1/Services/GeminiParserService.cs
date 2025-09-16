using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IGeminiParserService
    {
        Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream);
    }

    public class GeminiParserService : IGeminiParserService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiParserService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _apiKey = _configuration["Google:GeminiApiKey"] 
                ?? throw new ArgumentNullException("Google:GeminiApiKey is not configured");
        }

        public async Task<ParsedInvoice> ParseInvoiceImageAsync(Stream imageStream)
        {
            try
            {
                // Convert image to base64
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBase64 = Convert.ToBase64String(ms.ToArray());

                var prompt = @"Analyze this logistics invoice image and extract the following information:
                    - Freight Bill Number (labeled as Freight Bill No., FR Bill No., etc.)
                    - Bill of Lading Number (labeled as B/L No., BOL, Bill of Lading No., etc.)
                    - Vendor name
                    - Customer name
                    - Line items (description and amount)
                    - Subtotal
                    - Tax amount
                    - Total amount
                    - Shipper information (name, address)
                    - Consignee information (name, address)
                    Format as JSON with this structure:
                    {
                        ""freightBillNo"": ""string"",
                        ""billOfLading"": ""string"",
                        ""vendorName"": ""string"",
                        ""customerName"": ""string"",
                        ""shipper"": {
                            ""name"": ""string"",
                            ""address"": {
                                ""street"": ""string"",
                                ""city"": ""string"",
                                ""state"": ""string"",
                                ""country"": ""string"",
                                ""postalCode"": ""string""
                            }
                        },
                        ""consignee"": {
                            ""name"": ""string"",
                            ""address"": {
                                ""street"": ""string"",
                                ""city"": ""string"",
                                ""state"": ""string"",
                                ""country"": ""string"",
                                ""postalCode"": ""string""
                            }
                        },
                        ""items"": [
                            {
                                ""description"": ""string"",
                                ""amount"": { ""currencySymbol"": ""string"", ""amount"": number }
                            }
                        ],
                        ""subTotal"": { ""currencySymbol"": ""string"", ""amount"": number },
                        ""totalTax"": { ""currencySymbol"": ""string"", ""amount"": number },
                        ""invoiceTotal"": { ""currencySymbol"": ""string"", ""amount"": number }
                    }";

                //var requestBody = new GeminiRequest
                //{
                //    Contents = new[]
                //    {
                //        new RequestContent
                //        {
                //            Role = "user",
                //            Parts = new IPart[]
                //            {
                //                new TextPart { Text = prompt },
                //                new InlineDataPart 
                //                { 
                //                    InlineData = new InlineData 
                //                    { 
                //                        MimeType = "image/jpeg",
                //                        Data = imageBase64
                //                    }
                //                }
                //            }
                //        }
                //    },
                //    GenerationConfig = new GenerationConfig
                //    {
                //        Temperature = 0.1f,
                //        TopP = 0.8f,
                //        TopK = 40
                //    }
                //};

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
                
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

                // Extract the JSON response from the Gemini text output
                var jsonStart = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text?.IndexOf('{') ?? -1;
                var jsonEnd = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text?.LastIndexOf('}') ?? -1;

                if (jsonStart == -1 || jsonEnd == -1)
                {
                    throw new Exception("Could not find valid JSON in Gemini response");
                }

                var jsonResponse = geminiResponse.Candidates[0].Content.Parts[0].Text
                    .Substring(jsonStart, jsonEnd - jsonStart + 1);

                // Parse the JSON response into our model
                var parsedInvoice = JsonSerializer.Deserialize<ParsedInvoice>(
                    jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                // Add confidence scores (Gemini doesn't provide these, so we'll set a default)
                const double defaultConfidence = 0.85;
                parsedInvoice.VendorNameConfidence = defaultConfidence;
                parsedInvoice.CustomerNameConfidence = defaultConfidence;

                // Set confidence for shipping information
                if (parsedInvoice.Shipper != null)
                {
                    parsedInvoice.Shipper.NameConfidence = defaultConfidence;
                    if (parsedInvoice.Shipper.Address != null)
                    {
                        parsedInvoice.Shipper.AddressConfidence = defaultConfidence;
                    }
                }

                if (parsedInvoice.Consignee != null)
                {
                    parsedInvoice.Consignee.NameConfidence = defaultConfidence;
                    if (parsedInvoice.Consignee.Address != null)
                    {
                        parsedInvoice.Consignee.AddressConfidence = defaultConfidence;
                    }
                }

                foreach (var item in parsedInvoice.Items)
                {
                    item.DescriptionConfidence = defaultConfidence;
                    if (item.Amount != null)
                    {
                        item.Amount.Confidence = defaultConfidence;
                    }
                }

                if (parsedInvoice.SubTotal != null)
                    parsedInvoice.SubTotal.Confidence = defaultConfidence;
                if (parsedInvoice.TotalTax != null)
                    parsedInvoice.TotalTax.Confidence = defaultConfidence;
                if (parsedInvoice.InvoiceTotal != null)
                    parsedInvoice.InvoiceTotal.Confidence = defaultConfidence;

                return parsedInvoice;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing invoice with Gemini: {ex.Message}", ex);
            }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }
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
    }
}
