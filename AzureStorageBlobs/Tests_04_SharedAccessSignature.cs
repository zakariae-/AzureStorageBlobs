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
    public class Tests_04_SharedAccessSignature
    {
        private const string ContainerName = "test-bob-sharedaccesssignature";
        private readonly CloudBlobContainer _cloudBlobContainer;
        private readonly string _sas_read;

        public Tests_04_SharedAccessSignature()
        {
            var StorageAccountName = "Your_Storage_Account_Name";
            var StorageAccountKey = "Your_Storage_Account_Key";

            var storageCredentials = new StorageCredentials(StorageAccountName, StorageAccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            var cloudClient = cloudStorageAccount.CreateCloudBlobClient();

            _cloudBlobContainer = cloudClient.GetContainerReference(ContainerName);
            _cloudBlobContainer.CreateIfNotExistsAsync().Wait();

            _sas_read = _cloudBlobContainer.GetSharedAccessSignature(
                new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddSeconds(50)
                });
        }

        [Fact(DisplayName = "Should Use SAS")]
        public async Task ShouldUseSAS()
        {
            var loadedAssemblyDirectory = FileProcess.GetLoadedAssemblyDirectory(Assembly.GetAssembly(GetType()));
            var filePath = Path.Combine(loadedAssemblyDirectory, @"./Blobs/text.txt");
            var fileName = Path.GetFileName(filePath);

            var cloudBlobContainer = _cloudBlobContainer.GetBlockBlobReference(fileName);
            await cloudBlobContainer.DeleteIfExistsAsync();

            await cloudBlobContainer.UploadFromFileAsync(filePath);

            var blob = new CloudBlob(
                new Uri($"Your_Container_Uri/{fileName}"), 
                new StorageCredentials(_sas_read));

            var memoryStream = new MemoryStream();
            await blob.DownloadToStreamAsync(memoryStream);
            memoryStream.Capacity.Should().BeGreaterThan(0);
        }
    }
}