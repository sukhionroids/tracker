using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using System.Text.Json;

namespace WebApp.Services;

public class AzureStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageService(ILogger<AzureStorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Get connection string from configuration
        _connectionString = configuration.GetValue<string>("AzureStorage:ConnectionString") ?? 
            "UseDevelopmentStorage=true"; // Use local storage emulator if not configured
        
        _containerName = configuration.GetValue<string>("AzureStorage:ContainerName") ?? "lifetrack-data";
        
        // Initialize the clients
        try
        {
            _blobServiceClient = new BlobServiceClient(_connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            
            // Create the container if it doesn't exist
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Azure Blob Storage");
            throw;
        }
    }

    public async Task<T?> GetDataAsync<T>(string fileName)
    {
        try
        {
            // Get the blob client
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            
            // Check if the blob exists
            if (await blobClient.ExistsAsync())
            {
                // Download the blob content
                var response = await blobClient.DownloadAsync();
                
                using (StreamReader reader = new StreamReader(response.Value.Content))
                {
                    string content = await reader.ReadToEndAsync();
                    
                    // Deserialize the JSON content
                    return JsonSerializer.Deserialize<T>(content);
                }
            }
            
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving data from Azure Storage: {fileName}");
            return default;
        }
    }

    public async Task SaveDataAsync<T>(string fileName, T data)
    {
        try
        {
            // Get the blob client
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            
            // Serialize the data to JSON
            string jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true
            });
            
            // Upload the content to the blob
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
            {
                await blobClient.UploadAsync(memoryStream, overwrite: true);
            }
            
            _logger.LogInformation($"Data saved to Azure Storage: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving data to Azure Storage: {fileName}");
            throw;
        }
    }

    public async Task DeleteDataAsync(string fileName)
    {
        try
        {
            // Get the blob client
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            
            // Delete the blob if it exists
            await blobClient.DeleteIfExistsAsync();
            
            _logger.LogInformation($"Data deleted from Azure Storage: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting data from Azure Storage: {fileName}");
            throw;
        }
    }
}