using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Xunit;

namespace AzureStorageBlobs
{
    public class Test_01_ConnectTo
    {
        [Fact(DisplayName = "Should Connect To Localhost")]
        public async Task ShouldConnectToLocalhost()
        {
            var blobName = "test-blob";

            var StorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var client = StorageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = client.GetContainerReference(blobName);

            await cloudBlobContainer.CreateIfNotExistsAsync();
            var isExist = await cloudBlobContainer.ExistsAsync();

            isExist.Should().BeTrue();
            cloudBlobContainer.Name.Should().BeEquivalentTo(blobName);
        }

        [Fact(DisplayName = "Should Connect To Cloud")]
        public async Task ShouldConnectToCloud()
        {
            var blobName = "test-blob";
            var StorageAccountName = "Your_Storage_Account_Name";
            var StorageAccountKey = "Your_Storage_Account_Key";

            var storageCredentials = new StorageCredentials(StorageAccountName, StorageAccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            var cloudClient = cloudStorageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudClient.GetContainerReference(blobName);

            await cloudBlobContainer.CreateIfNotExistsAsync();
            var isExist = await cloudBlobContainer.ExistsAsync();

            isExist.Should().BeTrue();
            cloudBlobContainer.Name.Should().BeEquivalentTo(blobName);
        }
    }
}
