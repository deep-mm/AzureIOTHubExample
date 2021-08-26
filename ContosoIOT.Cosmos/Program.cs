using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace ContosoIOT.Cosmos
{
    class Program
    {
        const string cosmosEndpoint = ""; // Cosmos Endpoint
        const string cosmosConnstring = ""; // Cosmos Connection String

        static async Task Main(string[] args)
        {
            Console.WriteLine("Add a new document");

            await AddDocument();
        }

        static async Task AddDocument()
        {
            var identifier = Guid.NewGuid();

            dynamic document = new
            {
                id = identifier,
                Name = "ford",
                origin = "USA",
                telemetries = new string[]
                {
                    "fuelstatus",
                    "maintainancestatus",
                    "emergencycall"
                }
            };

            using (var client = GetClient())
            {
                var container = client.GetContainer("testdb", "testcontainer");

                var result = await container.CreateItemAsync(document);
                var createCost = result.RequestCharge;

                var iterator = container.GetItemQueryIterator<dynamic>(
                    queryText: $"SELECT * From c where c.id = '{identifier}'",
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(identifier.ToString()) });
                result = await iterator.ReadNextAsync();

                var readCost = result.RequestCharge;

                Console.WriteLine($"Cost to create document: {createCost} RUs. Cost to read document: {readCost} RUs.");
            }
        }

        static CosmosClient GetClient()
        {
            var client = new CosmosClient(cosmosConnstring);

            return client;
        }
    }
}
