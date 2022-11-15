using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos.Table;
using Azure;
using System.Net.Http;
using System.Net;

namespace GetResumeCounterFunction
{
    public static class GetResumeCounterFunction
    {
        [FunctionName("GetResumeCounterFunction")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, Counter counter, CloudTable outputTable,
            ILogger log)
        {

            try {

                counter.PartitionKey = counter.id;
                counter.RowKey = counter.id;
                counter.ETag = "*";
                counter.Count += 1;

                var operation = TableOperation.Replace(counter);
                await outputTable.ExecuteAsync(operation);

            return new HttpResponseMessage(HttpStatusCode.NoContent);

            }

            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong. Exception thrown: {ex.Message}");
                Console.ReadLine();
                throw;
            }

        }


        public class Counter: TableEntity
        {
            public string id { get; set; }
            public string Count { get; set; }
        }
            }

    
}
