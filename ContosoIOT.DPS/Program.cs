using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace ContosoIOT.DPS
{
    class Program
    {
        private const string dpsConnString = ""; //DPS connection string
        private const string SampleRegistrationId = "sample-individual-csharp";
        private const string SampleTpmEndorsementKey =
                "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
                "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
                "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
                "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
                "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";
        private const string OptionalDeviceId = "myCSharpDevice";
        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            try
            {
                Console.WriteLine("IoT Device Provisioning example");

                SetRegistrationDataAsync().GetAwaiter().GetResult();

                Console.WriteLine("Done, hit enter to exit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
            Console.ReadLine();
        }

        static async Task SetRegistrationDataAsync()
        {
            Console.WriteLine("Starting SetRegistrationData");

            Attestation attestation = new TpmAttestation(SampleTpmEndorsementKey);

            IndividualEnrollment individualEnrollment = new IndividualEnrollment(SampleRegistrationId, attestation);

            individualEnrollment.DeviceId = OptionalDeviceId;
            individualEnrollment.ProvisioningStatus = OptionalProvisioningStatus;

            Console.WriteLine("\nAdding new individualEnrollment...");
            var serviceClient = ProvisioningServiceClient.CreateFromConnectionString(dpsConnString);

            IndividualEnrollment individualEnrollmentResult =
                await serviceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);

            Console.WriteLine("\nIndividualEnrollment created with success.");
            Console.WriteLine(individualEnrollmentResult);
        }
    }
}
