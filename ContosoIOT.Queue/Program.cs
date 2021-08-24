using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ContosoIOT.Queue
{
    class Program
    {
        const string sbConnString = ""; //Service Bus Connection String
        const string QueueName = "testqueue";

        static async Task Main(string[] args)
        {
            // Create a Queue
            // var client = new ServiceBusAdministrationClient(sbConnString);
            // await client.CreateQueueAsync(QueueName);

            await using var queueClient = new ServiceBusClient(sbConnString, new ServiceBusClientOptions()
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    TryTimeout = TimeSpan.FromSeconds(2)
                }
            });

            // Create the Sender
            //await SendQueueMessage(queueClient);

            // Receive Message
            await ReceiveMessage(queueClient);

        }

        static async Task SendQueueMessage(ServiceBusClient queueClient)
        {
            ServiceBusSender sender = queueClient.CreateSender(QueueName);

            try
            {
                Console.WriteLine("Sending a message to service bus queue...");
                string messageBody = $"This is a text message";
                var message1 = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));
                message1.ApplicationProperties.Add("propertyName", "propertyValue");

                await sender.SendMessageAsync(message1);
                Console.WriteLine("Message sent successfully!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured: " + e);
            }
        }

        static async Task ReceiveMessage(ServiceBusClient queueClient)
        {
            try
            {
                Console.WriteLine("Receiving messages from service bus queue...");

                var receiverOptions = new ServiceBusReceiverOptions()
                {
                    ReceiveMode = ServiceBusReceiveMode.PeekLock,
                    PrefetchCount = 10,
                    SubQueue = SubQueue.None
                };

                // Approach 1
                ServiceBusReceiver receiver = queueClient.CreateReceiver(QueueName, receiverOptions);

                await foreach (var message in receiver.ReceiveMessagesAsync())
                {
                    string body = message.Body.ToString();
                    Console.WriteLine($"Received message: Seq Number: {message.SequenceNumber} Body: {Encoding.UTF8.GetString(message.Body)}");
                    Console.WriteLine(body);
                }

                // Approach 2
                var processorOptions = new ServiceBusProcessorOptions()
                {
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = 1,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10),
                    ReceiveMode = ServiceBusReceiveMode.PeekLock,
                    PrefetchCount = 10
                };

                await using var sbReceiver = queueClient.CreateProcessor(QueueName, processorOptions);

                sbReceiver.ProcessMessageAsync += MessageHandler;
                sbReceiver.ProcessErrorAsync += ErrorHandler;

                await sbReceiver.StartProcessingAsync();

                Console.ReadKey();

                await sbReceiver.StopProcessingAsync();

                Console.Read();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured: " + e);
            }
        }

        static Task ErrorHandler (ProcessErrorEventArgs errorEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {errorEventArgs.Exception}");
            return Task.CompletedTask;
        }

        static async Task MessageHandler(ProcessMessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            Console.WriteLine($"Received message with '{message.MessageId}' and content '{Encoding.UTF8.GetString(message.Body)}'");
            await messageEventArgs.CompleteMessageAsync(message);
        }
    }
}
