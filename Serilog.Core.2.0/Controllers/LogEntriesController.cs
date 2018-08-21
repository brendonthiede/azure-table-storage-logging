using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Serilog.Core._2._0.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LogEntriesController : Controller
    {
        private CloudStorageAccount storageAccount;
        private readonly string storageTableName;

        public LogEntriesController(IConfiguration configuration)
        {
            storageAccount = CloudStorageAccount.Parse(configuration.GetSection("AzureStorageConnectionString").Value);
            storageTableName = configuration.GetSection("LoggingStorageTableName").Value;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {

            return new string[] { "value1", "value2" };
        }

        // GET api/logentries/partition/20180820
        [HttpGet("partition/{partitionKey}")]
        public ICollection<LogEntryEntity> Get(string partitionKey)
        {
            var logEntries = new List<string>();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Create the CloudTable object that represents the "quotes" table. 
            CloudTable table = tableClient.GetTableReference(storageTableName);
            // Print the fields for each customer.
            var results = Get(partitionKey, table).GetAwaiter().GetResult();
            return results;
        }

        public static async Task<List<LogEntryEntity>> Get(string partitionKey, CloudTable table)
        {
            TableQuery<LogEntryEntity> query = new TableQuery<LogEntryEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            var re = new List<LogEntryEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<LogEntryEntity> employees = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                re.AddRange(employees.Results);
                continuationToken = employees.ContinuationToken;
            } while (continuationToken != null);
            return re;
        }

        public class LogEntryEntity : TableEntity
        {
            public LogEntryEntity() { }
            public string MessageTemplate { get; set; }
            public string Level { get; set; }
            public string RenderedMessage { get; set; }
            public string Data { get; set; }
        }
    }
}