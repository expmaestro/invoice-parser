using System.Text.Json;
using InvoiceParser.Models;

namespace InvoiceParser.Services
{
    public static class AiResponseParser
    {
        /// <summary>
        /// Extracts and parses invoice data from AI provider responses (OpenAI, Gemini, etc.)
        /// </summary>
        public static ParsedInvoice? ExtractParsedInvoice(string? responseContent)
        {
            if (string.IsNullOrEmpty(responseContent)) return null;

            try
            {
                // Parse the response as a JSON document to determine the format
                var responseDocument = JsonSerializer.Deserialize<JsonDocument>(responseContent);
                string? textContent = null;
                
                // Check if it's a Gemini response format
                if (responseDocument?.RootElement.TryGetProperty("candidates", out var candidatesElement) == true &&
                    candidatesElement.ValueKind == JsonValueKind.Array &&
                    candidatesElement.GetArrayLength() > 0)
                {
                    textContent = ExtractGeminiTextContent(candidatesElement);
                }
                // Check if it's an OpenAI response format
                else if (responseDocument?.RootElement.TryGetProperty("choices", out var choicesElement) == true &&
                    choicesElement.ValueKind == JsonValueKind.Array &&
                    choicesElement.GetArrayLength() > 0)
                {
                    textContent = ExtractOpenAITextContent(choicesElement);
                }

                if (!string.IsNullOrEmpty(textContent))
                {
                    // Extract and parse JSON from the text content
                    return ExtractInvoiceFromText(textContent);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the extraction
                Console.WriteLine($"Failed to extract parsed invoice from AI response: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cleans and extracts invoice JSON from OpenAI raw content text
        /// </summary>
        public static string? CleanOpenAIContent(string? content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            var cleanedContent = content.Trim();
            
            // Remove markdown code blocks if present
            if (cleanedContent.StartsWith("```json"))
            {
                cleanedContent = cleanedContent.Substring(7);
            }
            if (cleanedContent.EndsWith("```"))
            {
                cleanedContent = cleanedContent.Substring(0, cleanedContent.Length - 3);
            }
            
            return cleanedContent.Trim();
        }

        /// <summary>
        /// Parses cleaned content directly as invoice JSON
        /// </summary>
        public static ParsedInvoice? ParseInvoiceJson(string? jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return null;

            try
            {
                var parseOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    Converters = { new FlexibleDecimalConverter(), new FlexibleIntConverter() }
                };

                return JsonSerializer.Deserialize<ParsedInvoice>(jsonContent, parseOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse invoice JSON: {ex.Message}");
                return null;
            }
        }

        private static string? ExtractGeminiTextContent(JsonElement candidatesElement)
        {
            try
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
                        return textElement.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract Gemini text content: {ex.Message}");
            }
            return null;
        }

        private static string? ExtractOpenAITextContent(JsonElement choicesElement)
        {
            try
            {
                var firstChoice = choicesElement[0];
                if (firstChoice.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract OpenAI text content: {ex.Message}");
            }
            return null;
        }

        private static ParsedInvoice? ExtractInvoiceFromText(string textContent)
        {
            try
            {
                // Extract JSON from the text content (same logic as in GeminiParserService)
                var jsonStart = textContent.IndexOf('{');
                var jsonEnd = textContent.LastIndexOf('}');
                
                if (jsonStart == -1 || jsonEnd == -1)
                {
                    return null; // Could not find valid JSON
                }

                var jsonResponse = textContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return ParseInvoiceJson(jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract invoice from text: {ex.Message}");
                return null;
            }
        }
    }
}
