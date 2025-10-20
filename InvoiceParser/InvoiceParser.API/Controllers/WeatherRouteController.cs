using Microsoft.AspNetCore.Mvc;
using InvoiceParser.Api.Interfaces;

namespace InvoiceParser.Controllers
{
    [ApiController]
    [Route("api/weather-route")]
    public class WeatherRouteController : ControllerBase
    {
        private readonly IWeatherRouteService _weatherRouteService;

        public WeatherRouteController(IWeatherRouteService weatherRouteService)
        {
            _weatherRouteService = weatherRouteService;
        }

        [HttpPost("forecast")]
        public async Task<ActionResult<WeatherRouteResponse>> GetWeatherForecast(WeatherRouteRequest request)
        {
            if (string.IsNullOrEmpty(request.Origin) || string.IsNullOrEmpty(request.Destination))
                return BadRequest("Origin and destination are required");

            try
            {
                var result = await _weatherRouteService.GetWeatherRouteAsync(request.Origin, request.Destination, request.StartDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing weather route: {ex.Message}");
            }
        }
    }

    public class WeatherRouteRequest
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
    }

    public class WeatherRouteResponse
    {
        public string Summary { get; set; } = string.Empty;
        public RouteInfo Route { get; set; } = new();
        public List<WeatherPoint> WeatherPoints { get; set; } = new();
    }

    public class RouteInfo
    {
        public string Distance { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public List<LatLng> Path { get; set; } = new();
    }

    public class WeatherPoint
    {
        public LatLng Location { get; set; } = new();
        public string LocationName { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Precipitation { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public class LatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
