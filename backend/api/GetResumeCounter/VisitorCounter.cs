using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace GetResumeCounter
{
    public static class VisitorCounter
    {
        
        [FunctionName("VisitorCounter")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(Environment.GetEnvironmentVariable("VisitorCounterConnectionString"));
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;


             // Retrieve data from Cosmos DB table
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("VisitorCounterConnectionString"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("VisitorCount");

            TableOperation retrieveOperation = TableOperation.Retrieve<DynamicTableEntity>("1", "1");
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            DynamicTableEntity entity = (DynamicTableEntity)result.Result;
            int visitorCounter = int.Parse(entity.Properties["visitorCount"].StringValue);
            visitorCounter+=1;
            entity.Properties["visitorCount"].StringValue = visitorCounter.ToString();

            TableOperation updateOperation = TableOperation.Replace(entity);
            await table.ExecuteAsync(updateOperation);

            return new OkObjectResult(visitorCounter.ToString());
        }
    }
}
