using ContosoIOT.Common;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ContosoIOT.Portal
{
    class Program
    {
        private const string serviceConnectionString = ""; //Connection String
        static async Task Main(string[] args)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);

            while (true)
            {
                Console.WriteLine("Which device do you wish to send remote command to?");
                Console.Write("> ");
                var deviceId = Console.ReadLine();

                await SendCloudToDeviceMessage(serviceClient, deviceId);
                Console.WriteLine("Message sent!");
            }
        }

        private static async Task SendCloudToDeviceMessage(ServiceClient serviceClient, string deviceId)
        {
            var remoteCommand = new RemoteStartCommand() { cloudUTCDateTime = DateTime.UtcNow, messageId = new Random().Next(1, 1000)};

            Console.WriteLine("messageId: " + remoteCommand.messageId);

            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(remoteCommand)));

            await serviceClient.SendAsync(deviceId, commandMessage);
        }
    }
}
