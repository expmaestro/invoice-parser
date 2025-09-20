using Microsoft.AspNetCore.Mvc;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Models;

namespace InvoiceParser.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiLogsController : ControllerBase
    {
        private readonly IApiResponseLogService _apiResponseLogService;

        public ApiLogsController(IApiResponseLogService apiResponseLogService)
        {
            _apiResponseLogService = apiResponseLogService;
        }

        /// <summary>
        /// Get recent API response logs
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<List<ApiResponseLog>>> GetRecentLogs([FromQuery] int limit = 20)
        {
            try
            {
                var logs = await _apiResponseLogService.GetRecentApiResponsesAsync(limit);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving API logs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get API response logs by provider (gemini, azure)
        /// </summary>
        [HttpGet("provider/{provider}")]
        public async Task<ActionResult<List<ApiResponseLog>>> GetLogsByProvider(string provider, [FromQuery] int limit = 20)
        {
            try
            {
                var logs = await _apiResponseLogService.GetApiResponsesByProviderAsync(provider, limit);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving API logs for provider {provider}: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get specific API response log by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseLog>> GetLogById(string id)
        {
            try
            {
                var log = await _apiResponseLogService.GetApiResponseAsync(id);
                if (log == null)
                {
                    return NotFound(new { message = "API log not found" });
                }
                return Ok(log);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving API log: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get the image from a specific API response log
        /// </summary>
        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetLogImage(string id)
        {
            try
            {
                var log = await _apiResponseLogService.GetApiResponseAsync(id);
                if (log == null)
                {
                    return NotFound(new { message = "API log not found" });
                }

                if (log.ImageData == null || log.ImageData.Length == 0)
                {
                    return NotFound(new { message = "No image data found for this log" });
                }

                var mimeType = log.ImageMimeType ?? "image/jpeg";
                return File(log.ImageData, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving image: {ex.Message}" });
            }
        }
    }
}
