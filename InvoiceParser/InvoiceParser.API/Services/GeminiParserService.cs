using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Models;

namespace InvoiceParser.Services
{

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

                var prompt = @"Analyze this logistics invoice image and extract the following information as structured JSON:

                INVOICE INFORMATION:
                - Service
                - Freight Bill No
                - Shipment Date
                - Amount Due
                - Payment Due Date
                - FED TAX ID

                REMIT TO INFORMATION:
                - Company Name
                - Address (full address as one string)
                - Phone / Fax
                - Email / Website
                - Account No

                BILL TO & PAYMENT DUE FROM:
                - Company Name
                - Address (full address as one string)
                - Phone
                - Account No

                SHIPPER INFORMATION:
                - Shipper Account #
                - Shipper Name
                - Shipper Address (full address as one string)
                - Shipper Phone

                CONSIGNEE INFORMATION:
                - Consignee Account #
                - Consignee Name
                - Consignee Address (full address as one string)
                - Consignee Phone

                SHIPMENT DETAILS:
                - P.O. Number
                - Bill of Lading No
                - Tariff
                - Payment Terms
                - Total Pieces
                - Total Weight

                LINE ITEMS (extract each piece/line item):
                - Number of pieces
                - Description
                - Weight (lbs)
                - Class
                - Rate
                - Charge

                TOTALS:
                - Subtotal
                - Tax amount
                - Total Amount

                Format as JSON with this exact structure:
                {
                    ""service"": ""string"",
                    ""freightBillNo"": ""string"",
                    ""shipmentDate"": ""string"",
                    ""amountDue"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""paymentDueDate"": ""string"",
                    ""fedTaxId"": ""string"",
                    ""remitTo"": {
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string"",
                        ""fax"": ""string"",
                        ""email"": ""string"",
                        ""website"": ""string"",
                        ""accountNumber"": ""string""
                    },
                    ""billTo"": {
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string"",
                        ""accountNumber"": ""string""
                    },
                    ""shipper"": {
                        ""accountNumber"": ""string"",
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string""
                    },
                    ""consignee"": {
                        ""accountNumber"": ""string"",
                        ""name"": ""string"",
                        ""address"": { ""fullAddress"": ""string"" },
                        ""phone"": ""string""
                    },
                    ""shipmentDetails"": {
                        ""service"": ""string"",
                        ""shipmentDate"": ""string"",
                        ""poNumber"": ""string"",
                        ""billOfLading"": ""string"",
                        ""tariff"": ""string"",
                        ""paymentTerms"": ""string"",
                        ""totalPieces"": 0,
                        ""totalWeight"": 0.00
                    },
                    ""items"": [
                        {
                            ""pieces"": 0,
                            ""description"": ""string"",
                            ""weight"": 0.00,
                            ""class"": ""string"",
                            ""rate"": 0.00,
                            ""charge"": { ""currencySymbol"": ""$"", ""amount"": 0.00 }
                        }
                    ],
                    ""subTotal"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""totalTax"": { ""currencySymbol"": ""$"", ""amount"": 0.00 },
                    ""invoiceTotal"": { ""currencySymbol"": ""$"", ""amount"": 0.00 }
                }";


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
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { 
                        new FlexibleDecimalConverter(),
                        new FlexibleIntConverter()
                    }
                };
                
                var parsedInvoice = JsonSerializer.Deserialize<ParsedInvoice>(jsonResponse, options);

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

    public class FlexibleDecimalConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetDecimal();
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return null;
                    if (decimal.TryParse(stringValue, out decimal result))
                        return result;
                    return null;
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public class FlexibleIntConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetInt32();
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return null;
                    if (int.TryParse(stringValue, out int result))
                        return result;
                    return null;
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
