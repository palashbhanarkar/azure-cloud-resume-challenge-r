using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace GetResumeCounter
{
    public static class VisitorCounter
    {
        // Configuration constants - centralized for easier maintenance
        private const string TableName = "VisitorCount";
        private const string PartitionKey = "1";
        private const string RowKey = "1";
        private const string CounterPropertyName = "visitorCount";

        [FunctionName("VisitorCounter")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Handle CORS preflight requests
            if (req.Method == "OPTIONS")
            {
                return HandleCorsPreFlight();
            }

            try
            {
                log.LogInformation("Visitor counter request received.");

                // Add CORS headers to the request context for response
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // Get connection string from environment
                string connectionString = Environment.GetEnvironmentVariable("VisitorCounterConnectionString");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    log.LogError("VisitorCounterConnectionString environment variable is not set.");
                    return new ObjectResult("Configuration error") { StatusCode = 500 };
                }

                // Initialize table client
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(TableName);

                // Retrieve the visitor count entity
                TableOperation retrieveOperation = TableOperation.Retrieve<DynamicTableEntity>(PartitionKey, RowKey);
                TableResult result = await table.ExecuteAsync(retrieveOperation);

                // Auto-initialize entity if it doesn't exist
                DynamicTableEntity entity = (DynamicTableEntity)result.Result;
                if (entity == null)
                {
                    log.LogInformation($"Counter entity not found. Auto-initializing with value 0.");
                    
                    // Create initial entity
                    entity = new DynamicTableEntity(PartitionKey, RowKey);
                    entity.Properties = new Dictionary<string, EntityProperty>
                    {
                        { CounterPropertyName, new EntityProperty("0") }
                    };
                    
                    // Insert the new entity
                    TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
                    await table.ExecuteAsync(insertOperation);
                    log.LogInformation("Counter entity created successfully with initial value: 0");
                    
                    // Re-fetch to ensure we have the latest version with ETag
                    result = await table.ExecuteAsync(retrieveOperation);
                    entity = (DynamicTableEntity)result.Result;
                    if (entity == null)
                    {
                        log.LogError("Failed to retrieve newly created counter entity.");
                        return new ObjectResult("Failed to initialize counter") { StatusCode = 500 };
                    }
                }

                // Safely parse and increment counter
                if (!entity.Properties.TryGetValue(CounterPropertyName, out var counterProperty))
                {
                    log.LogError($"Property '{CounterPropertyName}' not found in counter entity.");
                    return new BadRequestObjectResult($"Invalid entity structure: missing '{CounterPropertyName}'");
                }

                if (!int.TryParse(counterProperty.StringValue, out int visitorCount))
                {
                    log.LogError($"Failed to parse counter value: '{counterProperty.StringValue}'");
                    return new BadRequestObjectResult("Invalid counter value: not an integer");
                }

                // Increment counter
                visitorCount++;
                entity.Properties[CounterPropertyName].StringValue = visitorCount.ToString();

                // Update the table
                TableOperation updateOperation = TableOperation.Replace(entity);
                await table.ExecuteAsync(updateOperation);

                log.LogInformation($"Visitor count incremented to: {visitorCount}");
                return new OkObjectResult(visitorCount.ToString());
            }
            catch (StorageException ex)
            {
                log.LogError($"Storage operation failed: {ex.Message}");
                return new ObjectResult($"Storage error: {ex.Message}") { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected error: {ex.Message}");
                return new ObjectResult("An unexpected error occurred") { StatusCode = 500 };
            }
        }

        /// <summary>
        /// Helper method to add CORS headers to response
        /// </summary>
        private static IActionResult AddCorsHeaders(IActionResult result)
        {
            // This is handled by AddCorsHeaders extension method below
            return result;
        }

        /// <summary>
        /// Handle CORS preflight requests
        /// </summary>
        private static IActionResult HandleCorsPreFlight()
        {
            return new OkResult();
        }
    }
}
