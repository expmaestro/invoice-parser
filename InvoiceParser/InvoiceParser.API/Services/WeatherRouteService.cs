using InvoiceParser.Api.Interfaces;
using InvoiceParser.Controllers;
using System.Text.Json;

namespace InvoiceParser.Services
{
    public class WeatherRouteService : IWeatherRouteService
    {
        private readonly HttpClient _httpClient;
        private readonly IGeminiParserService _geminiService;
        private readonly IConfiguration _configuration;

        public WeatherRouteService(HttpClient httpClient, IGeminiParserService geminiService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _geminiService = geminiService;
            _configuration = configuration;
        }

        public async Task<WeatherRouteResponse> GetWeatherRouteAsync(string origin, string destination, DateTime? startDate = null)
        {
            try
            {
                // Use current date if no start date provided
                var forecastDate = startDate ?? DateTime.UtcNow;
                
                // Step 1: Get route from Google Maps Directions API
                var route = await GetRouteAsync(origin, destination);
                
                // Step 2: Sample points along the route (every ~100km)
                var samplePoints = SampleRoutePoints(route.Path, 10); // 10 points along route
                
                // Step 3: Get weather data for each sampled point
                var weatherPoints = new List<WeatherPoint>();
                foreach (var point in samplePoints)
                {
                    var weather = await GetWeatherAsync(point.Lat, point.Lng, forecastDate);
                    if (weather != null)
                        weatherPoints.Add(weather);
                }

                // Step 4: Generate summary using Gemini
                var summary = await GenerateWeatherSummaryAsync(origin, destination, weatherPoints, forecastDate);

                return new WeatherRouteResponse
                {
                    Summary = summary,
                    Route = route,
                    WeatherPoints = weatherPoints
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get weather route: {ex.Message}", ex);
            }
        }

        private async Task<RouteInfo> GetRouteAsync(string origin, string destination)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Google Maps API key not configured");

            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&key={apiKey}&mode=driving";
            
            var response = await _httpClient.GetStringAsync(url);
            var jsonDoc = JsonDocument.Parse(response);
            
            if (jsonDoc.RootElement.GetProperty("status").GetString() != "OK")
                throw new Exception("Could not find route between locations");

            var routes = jsonDoc.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
                throw new Exception("No routes found");

            var route = routes[0];
            var leg = route.GetProperty("legs")[0];
            
            var distance = leg.GetProperty("distance").GetProperty("text").GetString() ?? "";
            var duration = leg.GetProperty("duration").GetProperty("text").GetString() ?? "";

            // Decode polyline to get path coordinates
            var polyline = route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "";
            var path = DecodePolyline(polyline);

            return new RouteInfo
            {
                Distance = distance,
                Duration = duration,
                Path = path
            };
        }

        private async Task<WeatherPoint?> GetWeatherAsync(double lat, double lng, DateTime forecastDate)
        {
            var apiKey = _configuration["OpenWeatherMap:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("OpenWeatherMap API key not configured");

            // Check if we need current weather or forecast
            var now = DateTime.UtcNow;
            var daysDifference = (forecastDate.Date - now.Date).Days;
            
            string url;
            if (daysDifference <= 0)
            {
                // Current weather
                url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lng}&appid={apiKey}&units=metric";
            }
            else if (daysDifference <= 5)
            {
                // 5-day forecast
                url = $"https://api.openweathermap.org/data/2.5/forecast?lat={lat}&lon={lng}&appid={apiKey}&units=metric";
            }
            else
            {
                // For dates beyond 5 days, use current weather as fallback
                url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lng}&appid={apiKey}&units=metric";
            }
            
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);

                JsonElement main, weather;
                string name;
                
                if (daysDifference > 0 && daysDifference <= 5)
                {
                    // Handle forecast response - find the closest forecast to the target date
                    var forecasts = jsonDoc.RootElement.GetProperty("list");
                    JsonElement closestForecast = forecasts[0];
                    
                    var targetTime = ((DateTimeOffset)forecastDate).ToUnixTimeSeconds();
                    var minDifference = long.MaxValue;
                    
                    foreach (var forecast in forecasts.EnumerateArray())
                    {
                        var forecastTime = forecast.GetProperty("dt").GetInt64();
                        var difference = Math.Abs(forecastTime - targetTime);
                        if (difference < minDifference)
                        {
                            minDifference = difference;
                            closestForecast = forecast;
                        }
                    }
                    
                    main = closestForecast.GetProperty("main");
                    weather = closestForecast.GetProperty("weather")[0];
                    name = jsonDoc.RootElement.GetProperty("city").GetProperty("name").GetString() ?? $"{lat:F2},{lng:F2}";
                }
                else
                {
                    // Handle current weather response
                    main = jsonDoc.RootElement.GetProperty("main");
                    weather = jsonDoc.RootElement.GetProperty("weather")[0];
                    name = jsonDoc.RootElement.GetProperty("name").GetString() ?? $"{lat:F2},{lng:F2}";
                }

                var precipitation = 0.0;
                if (jsonDoc.RootElement.TryGetProperty("rain", out var rain))
                {
                    if (rain.TryGetProperty("1h", out var rainVolume))
                        precipitation = rainVolume.GetDouble();
                    else if (rain.TryGetProperty("3h", out var rain3h))
                        precipitation = rain3h.GetDouble() / 3; // Convert 3h to 1h average
                }
                if (jsonDoc.RootElement.TryGetProperty("snow", out var snow))
                {
                    if (snow.TryGetProperty("1h", out var snowVolume))
                        precipitation += snowVolume.GetDouble();
                    else if (snow.TryGetProperty("3h", out var snow3h))
                        precipitation += snow3h.GetDouble() / 3; // Convert 3h to 1h average
                }

                return new WeatherPoint
                {
                    Location = new LatLng { Lat = lat, Lng = lng },
                    LocationName = name,
                    Temperature = main.GetProperty("temp").GetDouble(),
                    Description = weather.GetProperty("description").GetString() ?? "",
                    Precipitation = precipitation,
                    Icon = weather.GetProperty("icon").GetString() ?? ""
                };
            }
            catch
            {
                return null; // Skip points with weather API errors
            }
        }

        private async Task<string> GenerateWeatherSummaryAsync(string origin, string destination, List<WeatherPoint> weatherPoints, DateTime forecastDate)
        {
            var weatherData = string.Join("\n", weatherPoints.Select(w => 
                $"Location: {w.LocationName}, Temp: {w.Temperature:F1}°C, Conditions: {w.Description}, Precipitation: {w.Precipitation}mm"));

            var dateString = forecastDate.ToString("MMMM dd, yyyy");
            //            var prompt = $@"Given this weather data for a trip from {origin} to {destination} on {dateString}, provide a brief summary of potential weather hazards for drivers:

            //{weatherData}

            //Focus on hazards like:
            //- Snow or ice conditions
            //- Heavy rain or storms
            //- Extreme temperatures
            //- Poor visibility conditions

            //Keep the summary concise (2-3 sentences) and actionable for trip planning.";

            var prompt = $@"Given this weather data for a delivery route from {origin} to {destination} on {dateString}, provide a brief summary of potential weather-related risks that could impact delivery timing or safety:

{weatherData}

Focus on factors such as:
- Snow or ice that may delay transit
- Heavy rain or storms that could disrupt roads
- Extreme heat or cold affecting delivery conditions
- Poor visibility impacting transportation

Keep the summary concise (2–3 sentences) and suitable for informing customers about possible delays or issues.";


            try
            {
                return await _geminiService.GenerateTextAsync(prompt);
            }
            catch
            {
                return "Unable to generate weather summary at this time. Please check individual weather points for conditions.";
            }
        }

        private List<LatLng> SampleRoutePoints(List<LatLng> path, int sampleCount)
        {
            if (path.Count <= sampleCount)
                return path;

            var samples = new List<LatLng>();
            var interval = (double)(path.Count - 1) / (sampleCount - 1);

            for (int i = 0; i < sampleCount; i++)
            {
                var index = (int)Math.Round(i * interval);
                samples.Add(path[index]);
            }

            return samples;
        }

        private List<LatLng> DecodePolyline(string polyline)
        {
            var points = new List<LatLng>();
            var index = 0;
            var lat = 0;
            var lng = 0;

            while (index < polyline.Length)
            {
                int shift = 0, result = 0;
                int b;
                do
                {
                    b = polyline[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);
                
                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;

                shift = 0;
                result = 0;
                do
                {
                    b = polyline[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);
                
                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;

                points.Add(new LatLng 
                { 
                    Lat = lat / 1e5, 
                    Lng = lng / 1e5 
                });
            }

            return points;
        }
    }
}
