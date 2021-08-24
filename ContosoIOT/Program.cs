using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ContosoIOT.Common;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json;

namespace ContosoIOT.DeviceClientProject
{
    class Program
    {
        // Install Microsoft.Azure.Devices.Client nuget
        // Create the device. Get the connection string.
        // Create a device client to communicate to cloud
        // Decide the structure of telemetry/data
        // Serialize the data and convert to binary which is xpected by IOT hub
        // Add any additional properties that are required
        // Use SendEventAsync method to send data to cloud

        public const string DeviceConnectionString = ""; //Device connection string
        static async Task Main(string[] args)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString);
            try
            {
                Console.WriteLine("Connecting Device");
                await deviceClient.OpenAsync();
                Console.WriteLine("Device is connected");

                //await ReceiveEvents(deviceClient);

                Console.ReadKey();

                await SendTelemetry(deviceClient);

                Console.ReadKey();
            }
            catch (IotHubException ex)
            {
                throw ex;
            }
        }

        private static async Task SendTelemetry(DeviceClient deviceClient)
        {
            Console.WriteLine("Sending telemetry data");

            var telemetry = new VehicleStatus() { KilometerDriven = (new Random().Next(0, 100000)), TirePressure = (new Random().Next(0, 50)) };
            Message msg = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetry)));
            msg.Properties.Add("MessageType", "VehicleStatus");
            await deviceClient.SendEventAsync(msg);

            Console.WriteLine("Message Sent!");
        }

        private static async Task ReceiveEvents(DeviceClient deviceClient)
        {
            while(true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage == null)
                {
                    continue;
                }

                var messageBody = receivedMessage.GetBytes();

                var payload = Encoding.ASCII.GetString(messageBody);

                var formattedMessage = new StringBuilder($"Received message from cloud: [{payload}]\n");

                foreach (KeyValuePair<string,string> prop in receivedMessage.Properties)
                {
                    formattedMessage.Append($"\tProperty: key={prop.Key}, value={prop.Value}");
                }

                Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
                await deviceClient.CompleteAsync(receivedMessage);

            }
        }
    }
}
