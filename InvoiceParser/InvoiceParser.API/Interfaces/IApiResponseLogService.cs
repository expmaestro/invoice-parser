using InvoiceParser.Models;

namespace InvoiceParser.Api.Interfaces
{
    public interface IApiResponseLogService
    {
        Task<string> SaveApiResponseAsync(ApiResponseLog apiResponse);
        Task<ApiResponseLog?> GetApiResponseAsync(string id);
        Task<List<ApiResponseLog>> GetApiResponsesByProviderAsync(string provider, int limit = 100);
        Task<List<ApiResponseLog>> GetRecentApiResponsesAsync(int limit = 50);
        Task<bool> DeleteApiResponseAsync(string id);
        Task<int> DeleteAllApiResponsesAsync();
    }
}
