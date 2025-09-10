using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceParserService _parserService;

        public InvoiceController(IInvoiceParserService parserService)
        {
            _parserService = parserService;
        }

        [HttpPost("parse")]
        public async Task<ActionResult> ParseInvoice(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("File must be an image");

            using var stream = file.OpenReadStream();
            try
            {
                var result = await _parserService.ParseInvoiceImageAsync(stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing the invoice: {ex.Message}");
            }
        }
    }
}
