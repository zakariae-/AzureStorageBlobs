using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using TestHelper;
using Xunit;

namespace AzureStorageBlobs
{
    public class Tests_03_AppendBlobs
    {
        private const string ContainerName = "test-append-blob";
        private readonly CloudBlobClient _cloudClient;

        public Tests_03_AppendBlobs()
        {
            var StorageAccountName = "Your_Storage_Account_Name";
            var StorageAccountKey = "Your_Storage_Account_Key";

            var storageCredentials = new StorageCredentials(StorageAccountName, StorageAccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            _cloudClient = cloudStorageAccount.CreateCloudBlobClient();
        }

        [Fact(DisplayName = "Should Append Blob")]
        public async Task ShouldAppendBlob()
        {
            var cloudBlobContainer = _cloudClient.GetContainerReference(ContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var blob = cloudBlobContainer.GetAppendBlobReference("text.data");
            await blob.CreateOrReplaceAsync();

            var loadedAssemblyDirectory = FileProcess.GetLoadedAssemblyDirectory(Assembly.GetAssembly(GetType()));
            var filePath = Path.Combine(loadedAssemblyDirectory, @"./Blobs/text.txt");

            string line;
            var i = 0;
            for (int idx = 0; idx < 20; idx++)
            {
                StreamReader file = new StreamReader(filePath);
                while ((line = file.ReadLine()) != null)
                {
                    i++;
                    await blob.AppendTextAsync($"Line {i.ToString("d6")} added at : {DateTimeOffset.UtcNow.ToLocalTime()}  {line} {Environment.NewLine}");
                }
            }
        }

        [Fact(DisplayName = "Should Lease Blob")]
        public async Task ShouldLeaseBlob()
        {
            var cloudBlobContainer = _cloudClient.GetContainerReference(ContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var blob = cloudBlobContainer.GetAppendBlobReference("lease.data");
            await blob.CreateOrReplaceAsync();

            var leaseGuid = Guid.NewGuid().ToString();
            var accessCondition = new AccessCondition();
            var operationContext = new OperationContext();

            var leaseId = await blob.AcquireLeaseAsync(null, leaseGuid, accessCondition, null, operationContext);
            accessCondition.LeaseId = leaseId;

            var data = "quos imitatae matronae complures opertis capitibus et basternis per latera civitatis cuncta discurrunt";
            LeaseStatus leaseStatus;
            try
            {
                leaseStatus = blob.Properties.LeaseStatus;
                leaseStatus.Should().BeEquivalentTo(LeaseStatus.Unspecified);

                await blob.AppendTextAsync(data, null, accessCondition, null, operationContext);

                leaseStatus = blob.Properties.LeaseStatus;
                leaseStatus.Should().BeEquivalentTo(LeaseStatus.Locked);
            }
            finally
            {
                await blob.ReleaseLeaseAsync(accessCondition, null, operationContext);

                leaseStatus = blob.Properties.LeaseStatus;
                leaseStatus.Should().BeEquivalentTo(LeaseStatus.Unlocked);
            }
        }
    }
}