using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Serverless_Doc_Processor.Interfaces;
using System.Net;

namespace Serverless_Doc_Processor;

public class DocProcess
{
    private readonly ILogger<DocProcess> _logger;
    private readonly IEmailService _emailService;

    public DocProcess(ILogger<DocProcess> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    // Ingestion API (HTTP Trigger)
    [Function("UploadDocument")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // Parse multipart/form-data or JSON (simplified for demo)
        // Here, we expect headers: UploaderEmail, and body is the file
        if (!req.Headers.TryGetValues("UploaderEmail", out var emailHeaders))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Missing uploader email in header 'UploaderEmail'.");
            return badResponse;
        }

        string uploaderEmail = emailHeaders.FirstOrDefault()!;
        if (string.IsNullOrWhiteSpace(uploaderEmail))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Uploader email cannot be empty.");
            return badResponse;
        }

        var blobContainerClient = new BlobContainerClient(Environment.GetEnvironmentVariable("StorageConnectionString"), "uploads");
        await blobContainerClient.CreateIfNotExistsAsync();

        // Get original filename from header
        string originalFileName = req.Headers.Contains("FileName")
            ? req.Headers.GetValues("FileName").FirstOrDefault()!
            : $"uploaded_{Guid.NewGuid()}";

        // Sanitize filename to remove unsafe characters (optional)
        originalFileName = originalFileName.Replace(" ", "_");

        // Use original filename as blob name
        var blobClient = blobContainerClient.GetBlobClient(originalFileName);

        // Upload file stream
        using (var stream = req.Body)
        {
            await blobClient.UploadAsync(stream);
        }

        // Set metadata (uploader email + timestamp)
        var metadata = new Dictionary<string, string>{
        { "UploaderEmail", uploaderEmail },
        { "UploadedOn", DateTime.UtcNow.ToString("o") } };

        await blobClient.SetMetadataAsync(metadata);

        _logger.LogInformation($"Uploaded file by '{uploaderEmail}'");

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteStringAsync($"File uploaded successfully.");
        return response;
    }

    // (Blob Trigger)
    [Function("ProcessDocument")]
    public async Task Run(
        [BlobTrigger("uploads/{name}", Connection = "StorageConnectionString")] BlobClient blobClient,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation($"Processing file: {name}");

        // Get blob properties + metadata
        var properties = await blobClient.GetPropertiesAsync();
        var metadata = properties.Value.Metadata;

        if (!metadata.TryGetValue("UploaderEmail", out var uploaderEmail))
        {
            _logger.LogWarning("Uploader email not found in metadata.");
            return;
        }

        // Simulate processing
        _logger.LogInformation($"Processing completed for {name}");

        // Send email: Processing completed
        await _emailService.SendEmailAsync(
            to: uploaderEmail,
            subject: "Your document has been processed",
            body: $"Hello,\n\nYour file '{name}' has been successfully processed.\n\nThanks."
        );

        // Set expiry metadata (optional, helpful)
        var expiryDate = DateTime.UtcNow.AddDays(30).ToString("o");

        metadata["ExpiryDate"] = expiryDate;
        await blobClient.SetMetadataAsync(metadata);

        _logger.LogInformation($"Expiry set for {name} at {expiryDate}");
    }

    [Function("CleanupOldFiles")]
    public async Task Run(
        [TimerTrigger("0 0 0 * * *")] TimerInfo timer, // runs daily at midnight
        FunctionContext context)
    {
        var container = new BlobContainerClient(
            Environment.GetEnvironmentVariable("StorageConnectionString"),
            "uploads");

        await foreach (var blobItem in container.GetBlobsAsync(traits: BlobTraits.Metadata))
        {
            if (blobItem.Metadata.TryGetValue("ExpiryDate", out var expiryString))
            {
                if (DateTime.TryParse(expiryString, out var expiryDate))
                {
                    if (DateTime.UtcNow >= expiryDate)
                    {
                        var blobClient = container.GetBlobClient(blobItem.Name);

                        string uploaderEmail = blobItem.Metadata.ContainsKey("UploaderEmail") ? blobItem.Metadata["UploaderEmail"] : string.Empty;

                        // Delete blob
                        await blobClient.DeleteIfExistsAsync();

                        _logger.LogInformation($"Deleted expired file: {blobItem.Name}");

                        // Send email notification
                        if (!string.IsNullOrEmpty(uploaderEmail))
                        {
                            await _emailService.SendEmailAsync(
                                to: uploaderEmail,
                                subject: "Your document has been deleted",
                                body: $"Hello,\n\n Your file '{blobItem.Name}' has been deleted after 30 days retention period.\n\n It is no longer available for processing."
                            );
                        }
                    }
                }
            }
        }
    }
}