using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoIOT.EventHub
{
    class Program
    {
        const string eventHubConnString = ""; //Event Hub Connection String
        const string eventHubName = "iot-rampup-1";
        const string consumerGroup = "$Default";

        const string storageConnString = ""; //Storage Connection String
        const string blobContainerName = "checkPoints";

        static ConcurrentDictionary<string, int> partitioEventCount = new ConcurrentDictionary<string, int>();
        static EventHubProducerClient producerClient;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var producerOptions = new EventHubProducerClientOptions
            {
                RetryOptions = new EventHubsRetryOptions
                {
                    Mode = EventHubsRetryMode.Exponential,
                    MaximumDelay = TimeSpan.FromSeconds(10),
                    MaximumRetries = 5,
                    Delay = TimeSpan.FromMilliseconds(800)
                }
            };

            producerClient = new EventHubProducerClient(eventHubConnString, eventHubName, producerOptions);

            EventHubProperties properties = await producerClient.GetEventHubPropertiesAsync();

            Console.WriteLine("The Event Hub has following properties:");
            Console.WriteLine($"\tThe path to the event Hub from the namespace is: {properties.Name}");
            Console.WriteLine($"The Evnt Hub as created at: {properties.CreatedOn}, in UTC");
            Console.WriteLine($"The following partitions are available: [{string.Join(", ", properties.PartitionIds)}]");

            string[] partitions = await producerClient.GetPartitionIdsAsync();
            string firstPartition = partitions.FirstOrDefault();

            PartitionProperties partitionProperties = await producerClient.GetPartitionPropertiesAsync(firstPartition);

            Console.WriteLine($"Partition: {partitionProperties.Id}");

            await SendMessages();

            var storageClient = new BlobContainerClient(storageConnString, blobContainerName);

            var processor = new EventProcessorClient(storageClient, consumerGroup, eventHubConnString, eventHubName);

            await ProcessMessages(storageClient, processor);
        }

        static async Task SendMessages()
        {
            //Send to a partition
            string firstPartitionInfo = (await producerClient.GetPartitionIdsAsync()).First();

            var batchOptions = new CreateBatchOptions
            {
                PartitionId = firstPartitionInfo
            };

            //Partition key ensures all messages sent with this partition key are sent to same partition and in order
            // var batchOptions = new CreateBatchOptions
            // {
            //     PartitionKey = firstPartitionInfo
            // };

            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            for (int i = 1; i <= 3; i++)
            {
                if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"Event {i}"))))
                {
                    throw new Exception($"Event {i} is too large for the batch and cannot be sent");
                }
            }

            try
            {
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine("A batch of 3 events has been published");
            }
            finally
            {
                await producerClient.DisposeAsync();
            }
        }

        static async Task ProcessMessages(BlobContainerClient storageClient, EventProcessorClient processor)
        {
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(3000));

            processor.PartitionInitializingAsync += initializeEventHandler;
            processor.ProcessEventAsync += processEventHandler;
            processor.ProcessErrorAsync += processErrorHandler;

            try
            {
                await processor.StartProcessingAsync(cancellationSource.Token);
                await Task.Delay(Timeout.Infinite, cancellationSource.Token);
            }
            catch (Exception e)
            {

            }
            finally
            {
                await processor.StopProcessingAsync();
                processor.PartitionInitializingAsync -= initializeEventHandler;
                processor.ProcessEventAsync -= processEventHandler;
                processor.ProcessErrorAsync -= processErrorHandler;
            }
        }

        static Task initializeEventHandler(PartitionInitializingEventArgs args)
        {
            try
            {
                if (args.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }

                EventPosition startPositionWhenNoCheckpoint = EventPosition.Latest;

                args.DefaultStartingPosition = startPositionWhenNoCheckpoint;
            }
            catch (Exception e)
            {

            }

            return Task.CompletedTask;
        }
        
        static async Task processEventHandler(ProcessEventArgs args)
        {
            try
            {
                if (args.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                string partition = args.Partition.PartitionId;
                byte[] eventBody = args.Data.EventBody.ToArray();
                var payload = Encoding.UTF8.GetString(eventBody);
                Console.WriteLine($"Event: {payload} from partition {partition} with length {eventBody.Length}.");

                int eventsSinceLastCheckpoint = partitioEventCount.AddOrUpdate(
                    key: partition,
                    addValue: 1,
                    updateValueFactory: (_, currentCount) => currentCount + 1);

                if (eventsSinceLastCheckpoint >= 50)
                {
                    await args.UpdateCheckpointAsync();
                    partitioEventCount[partition] = 0;
                }
            }
            catch (Exception e)
            {

            }
        }

        static Task processErrorHandler(ProcessErrorEventArgs args)
        {
            try
            {
                Console.WriteLine($"Error in the EventProcessorClient: {args.Exception}");
            }
            catch (Exception e)
            {

            }

            return Task.CompletedTask;
        }
    }
}
