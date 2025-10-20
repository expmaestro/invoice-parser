using InvoiceParser.Controllers;

namespace InvoiceParser.Api.Interfaces
{
    public interface IWeatherRouteService
    {
        Task<WeatherRouteResponse> GetWeatherRouteAsync(string origin, string destination, DateTime? startDate = null);
    }
}
