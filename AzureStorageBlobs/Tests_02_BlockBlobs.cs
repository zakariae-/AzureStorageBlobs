using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using TestHelper;
using Xunit;

namespace AzureStorageBlobs
{
    public class Test_02_BlockBlobs
    {
        private const string ContainerName = "test-block-blob";
        private readonly CloudBlobClient _cloudClient;
        private string _eTag;

        public Test_02_BlockBlobs()
        {
            var StorageAccountName = "Your_Storage_Account_Name";
            var StorageAccountKey = "Your_Storage_Account_Key";

            var storageCredentials = new StorageCredentials(StorageAccountName, StorageAccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            _cloudClient = cloudStorageAccount.CreateCloudBlobClient();
        }


        [Fact(DisplayName ="Should Upload Blob")]
        public async Task ShouldUploadBlob()
        {
            await UploadBlob();
        }

        private async Task UploadBlob()
        {
            var cloudBlobContainer = _cloudClient.GetContainerReference(ContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var loadedAssemblyDirectory = FileProcess.GetLoadedAssemblyDirectory(Assembly.GetAssembly(GetType()));
            var filePath = Path.Combine(loadedAssemblyDirectory, @"./Blobs/cat.jpg");

            var isExists = File.Exists(filePath);
            isExists.Should().BeTrue();

            var fileName = Path.GetFileName(filePath);
            var blob = cloudBlobContainer.GetBlockBlobReference(fileName);
            await blob.DeleteIfExistsAsync();

            await blob.UploadFromFileAsync(filePath);
        }

        [Fact(DisplayName = "Should Not Upload An Existing Blob")]
        public async Task ShouldNotUploadAnExistingBlob()
        {
            await UploadBlob();

            var cloudBlobContainer = _cloudClient.GetContainerReference(ContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var loadedAssemblyDirectory = FileProcess.GetLoadedAssemblyDirectory(Assembly.GetAssembly(GetType()));
            var filePath = Path.Combine(loadedAssemblyDirectory, @"./Blobs/cat.jpg");

            var isExists = File.Exists(filePath);
            isExists.Should().BeTrue();

            var fileName = Path.GetFileName(filePath);
            var blob = cloudBlobContainer.GetBlockBlobReference(fileName);

            var accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
            await Assert.ThrowsAsync<StorageException>(() => blob.UploadFromFileAsync(filePath, accessCondition, null, null));
        }

        [Fact(DisplayName = "Should Upload Each Lines As An Individual Block")]
        public async Task ShouldUploadEachLinesAsAnIndividualBlock()
        {
            await UploadEachLineAsAnIndividualBlock();
        }

        private async Task UploadEachLineAsAnIndividualBlock()
        {
            var cloudBlobContainer = _cloudClient.GetContainerReference(BlobName);
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var loadedAssemblyDirectory = FileProcess.GetLoadedAssemblyDirectory(Assembly.GetAssembly(GetType()));
            var filePath = Path.Combine(loadedAssemblyDirectory, @"./Blobs/text.txt");

            var isExists = File.Exists(filePath);
            isExists.Should().BeTrue();

            var fileName = Path.GetFileName(filePath);
            var blob = cloudBlobContainer.GetBlockBlobReference(fileName);

            await blob.DeleteAsync();

            int id = 0;
            string line;
            var blockList = new List<string>();
            StreamReader file = new StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                id++;
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString("d6")));
                var blockData = new MemoryStream(Encoding.UTF8.GetBytes(line + Environment.NewLine));
                await blob.PutBlockAsync(blockId, blockData, null);
                blockList.Add(blockId);
            }

            var blobExists = await blob.ExistsAsync();
            blobExists.Should().BeFalse();

            await blob.PutBlockListAsync(blockList);
            _eTag = blob.Properties.ETag;

            blobExists = await blob.ExistsAsync();
            blobExists.Should().BeTrue();
        }

        [Fact(DisplayName = "Should Modify A Block On Blob")]
        public async Task ShouldModifyABlockOnBlob()
        {
            await ModifyABlockOnBlob();
        }

        private async Task ModifyABlockOnBlob()
        {
            await UploadEachLineAsAnIndividualBlock();

            var cloudBlobContainer = _cloudClient.GetContainerReference(BlobName);
            var blob = cloudBlobContainer.GetBlockBlobReference("text.txt");

            var isExists = await blob.ExistsAsync();
            isExists.Should().BeTrue();

            var listBlockItems = await blob.DownloadBlockListAsync();
            var blockIds = listBlockItems.Select(block => block.Name);

            var id = 5;
            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString("d6")));
            var blockData = new MemoryStream(
                Encoding.UTF8.GetBytes($"modification du block avec l'identifiant égale à {id}"));

            blockIds.Contains(blockId).Should().BeTrue();

            await blob.PutBlockAsync(blockId, blockData, null);

            await blob.PutBlockListAsync(blockIds);
        }

        [Fact(DisplayName = "Should Not Modify A Blob If Any Think Change")]
        public async Task ShouldNotModifyABlobIfChange()
        {
            await ModifyABlockOnBlob();

            var cloudBlobContainer = _cloudClient.GetContainerReference(BlobName);
            var blob = cloudBlobContainer.GetBlockBlobReference("text.txt");

            var isExists = await blob.ExistsAsync();
            isExists.Should().BeTrue();

            var listBlockItems = await blob.DownloadBlockListAsync();
            var blockIds = listBlockItems.Select(block => block.Name);

            var id = 3;
            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString("d6")));
            var blockData = new MemoryStream(
                Encoding.UTF8.GetBytes($"modification du block avec l'identifiant égale à {id}"));

            blockIds.Contains(blockId).Should().BeTrue();

            var accessCondition = AccessCondition.GenerateIfMatchCondition(_eTag);

            await blob.PutBlockAsync(blockId, blockData, null, accessCondition, null, null);

            await Assert.ThrowsAsync<StorageException>(
                () => blob.PutBlockListAsync(blockIds, accessCondition, null, null));
            
        }
    }
}
