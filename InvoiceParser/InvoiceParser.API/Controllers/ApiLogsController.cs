using Microsoft.AspNetCore.Mvc;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Models;
using InvoiceParser.DTOs;
using InvoiceParser.Services;

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
        public async Task<ActionResult<List<ApiLogDto>>> GetRecentLogs([FromQuery] int limit = 20)
        {
            try
            {
                var logs = await _apiResponseLogService.GetRecentApiResponsesAsync(limit);
                var dtos = logs.Select(ApiLogMapper.ToDto).ToList();
                return Ok(dtos);
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
        public async Task<ActionResult<List<ApiLogDto>>> GetLogsByProvider(string provider, [FromQuery] int limit = 20)
        {
            try
            {
                var logs = await _apiResponseLogService.GetApiResponsesByProviderAsync(provider, limit);
                var dtos = logs.Select(ApiLogMapper.ToDto).ToList();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving API logs for provider {provider}: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get specific API response log by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiLogDetailDto>> GetLogById(string id)
        {
            try
            {
                var log = await _apiResponseLogService.GetApiResponseAsync(id);
                if (log == null)
                {
                    return NotFound(new { message = "API log not found" });
                }
                var dto = ApiLogMapper.ToDetailDto(log);
                return Ok(dto);
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

        /// <summary>
        /// Delete a specific API response log by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(string id)
        {
            try
            {
                var deleted = await _apiResponseLogService.DeleteApiResponseAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "API log not found" });
                }
                return Ok(new { message = "API log deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting API log: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete all API response logs
        /// </summary>
        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllLogs()
        {
            try
            {
                var deletedCount = await _apiResponseLogService.DeleteAllApiResponsesAsync();
                return Ok(new { message = $"Deleted {deletedCount} API logs successfully", deletedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting all API logs: {ex.Message}" });
            }
        }
    }
}
