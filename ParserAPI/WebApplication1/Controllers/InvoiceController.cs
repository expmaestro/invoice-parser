using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceParserService _azureParser;
        private readonly IGeminiParserService _geminiParser;

        public InvoiceController(
            IInvoiceParserService azureParser, 
            IGeminiParserService geminiParser
            )
        {
            _azureParser = azureParser;
            _geminiParser = geminiParser;
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
                    "azure" => await _azureParser.ParseInvoiceImageAsync(stream),
                    _ => throw new ArgumentException("Invalid parser specified. Use 'azure' or 'gemini'")
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
