using MongoDB.Driver;
using InvoiceParser.Api.Interfaces;
using InvoiceParser.Models;

namespace InvoiceParser.Services
{
    public class ApiResponseLogService : IApiResponseLogService
    {
        private readonly IMongoCollection<ApiResponseLog> _apiResponsesCollection;

        public ApiResponseLogService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDB") 
                ?? throw new ArgumentNullException("MongoDB connection string is not configured");

            var databaseName = configuration["MongoDB:DatabaseName"] 
                ?? throw new ArgumentNullException("MongoDB database name is not configured");

            var collectionName = configuration["MongoDB:ApiResponsesCollectionName"] ?? "HistoryCollection";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _apiResponsesCollection = database.GetCollection<ApiResponseLog>(collectionName);

            // Create indexes for better performance
            CreateIndexes();
        }

        public async Task<string> SaveApiResponseAsync(ApiResponseLog apiResponse)
        {
            try
            {
                await _apiResponsesCollection.InsertOneAsync(apiResponse);
                return apiResponse.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving API response to MongoDB: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponseLog?> GetApiResponseAsync(string id)
        {
            try
            {
                return await _apiResponsesCollection
                    .Find(x => x.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving API response from MongoDB: {ex.Message}", ex);
            }
        }

        public async Task<List<ApiResponseLog>> GetApiResponsesByProviderAsync(string provider, int limit = 100)
        {
            try
            {
                return await _apiResponsesCollection
                    .Find(x => x.ApiProvider == provider)
                    .SortByDescending(x => x.Timestamp)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving API responses by provider from MongoDB: {ex.Message}", ex);
            }
        }

        public async Task<List<ApiResponseLog>> GetRecentApiResponsesAsync(int limit = 50)
        {
            try
            {
                return await _apiResponsesCollection
                    .Find(_ => true)
                    .SortByDescending(x => x.Timestamp)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving recent API responses from MongoDB: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteApiResponseAsync(string id)
        {
            try
            {
                var result = await _apiResponsesCollection.DeleteOneAsync(x => x.Id == id);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting API response from MongoDB: {ex.Message}", ex);
            }
        }

        public async Task<int> DeleteAllApiResponsesAsync()
        {
            try
            {
                var result = await _apiResponsesCollection.DeleteManyAsync(_ => true);
                return (int)result.DeletedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting all API responses from MongoDB: {ex.Message}", ex);
            }
        }

        private void CreateIndexes()
        {
            try
            {
                // Index on timestamp for recent queries
                var timestampIndex = Builders<ApiResponseLog>.IndexKeys.Descending(x => x.Timestamp);
                _apiResponsesCollection.Indexes.CreateOne(new CreateIndexModel<ApiResponseLog>(timestampIndex));

                // Index on API provider
                var providerIndex = Builders<ApiResponseLog>.IndexKeys.Ascending(x => x.ApiProvider);
                _apiResponsesCollection.Indexes.CreateOne(new CreateIndexModel<ApiResponseLog>(providerIndex));

                // Index on request ID for fast lookups
                var requestIdIndex = Builders<ApiResponseLog>.IndexKeys.Ascending(x => x.RequestId);
                _apiResponsesCollection.Indexes.CreateOne(new CreateIndexModel<ApiResponseLog>(requestIdIndex));

                // Compound index on provider and timestamp
                var compoundIndex = Builders<ApiResponseLog>.IndexKeys
                    .Ascending(x => x.ApiProvider)
                    .Descending(x => x.Timestamp);
                _apiResponsesCollection.Indexes.CreateOne(new CreateIndexModel<ApiResponseLog>(compoundIndex));
            }
            catch (Exception)
            {
                // Indexes might already exist, ignore errors
            }
        }
    }
}
