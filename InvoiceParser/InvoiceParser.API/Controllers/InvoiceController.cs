using Microsoft.AspNetCore.Mvc;
using InvoiceParser.Models;
using InvoiceParser.Api.Interfaces;

namespace InvoiceParser.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceParserService _azureParser;
        private readonly IGeminiParserService _geminiParser;
        private readonly IOpenAIParserService _openAIParser;

        public InvoiceController(
            IInvoiceParserService azureParser, 
            IGeminiParserService geminiParser,
            IOpenAIParserService openAIParser
            )
        {
            _azureParser = azureParser;
            _geminiParser = geminiParser;
            _openAIParser = openAIParser;
        }

        [HttpPost("parse")]
        public async Task<ActionResult<ParsedInvoice>> ParseInvoice(IFormFile file, [FromQuery] string parser = "azure")
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("File must be an image");

            using var stream = file.OpenReadStream();
            try
            {
                var result = parser.ToLower() switch
                {
                    "gemini" => await _geminiParser.ParseInvoiceImageAsync(stream),
                    "openai" => await _openAIParser.ParseInvoiceImageAsync(stream),
                    "azure" => await _azureParser.ParseInvoiceImageAsync(stream),
                    _ => throw new ArgumentException("Invalid parser specified. Use 'azure', 'gemini', or 'openai'")
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing the invoice: {ex.Message}");
            }
        }
    }
}
