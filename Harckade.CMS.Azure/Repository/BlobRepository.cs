using Azure.Storage.Blobs;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Repository
{
    public class BlobRepository : IBlobRepository
    {
        protected BlobContainerClient _blobContainer;
        public BlobRepository(string connectionString)
        {
            _blobContainer = new BlobContainerClient(connectionString, "harckade-binaries");
            _blobContainer.CreateIfNotExists();
        }

        private bool FolderExistsByFileId(string folderFileId)
        {
            BlobClient tmpBlob = _blobContainer.GetBlobClient(folderFileId);
            if (!tmpBlob.Exists())
            {
                return false;
            }
            return true;
        }
        private bool FolderExists(string folderPath)
        {
            return FolderExistsByFileId(Utils.FolderHelper.FolderPathToId(folderPath, Enum.GetName(FileType.Folder).ToLower()));
        }
        public async Task UploadBinary(BlobId blobId, Stream fileStream, string folder = "")
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (!FolderExists(folder))
                {
                    throw new Exception("Folder does not exists");
                }
                blobId = new BlobId($"{folder}/{blobId}");
            }
            BlobClient blob = _blobContainer.GetBlobClient(blobId.ToString());
            await blob.UploadAsync(fileStream, overwrite: true);
        }

        public async Task<Stream> DownloadFileAsync(BlobId blobId)
        {
            BlobClient blob = _blobContainer.GetBlobClient(blobId.ToString());
            if (blob.Exists())
            {
                var ms = new MemoryStream();
                await blob.DownloadToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            else
            {
                return null;
            }
        }

        public async Task<FileObject> FetchFileObjectAsync(BlobId blobId)
        {
            BlobClient blob = _blobContainer.GetBlobClient(blobId.ToString());
            try
            {
                var fileProps = await blob.GetPropertiesAsync();
                if (blob.Exists())
                {
                    return new FileObject(blobId, fileProps.Value);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("Service request failed.\r\nStatus: 404"))
                {
                    return null;
                }
                throw new Exception(e.Message);
            }
        }

        public async Task DeleteBlobAsync(BlobId blobId)
        {
            BlobClient blob = _blobContainer.GetBlobClient(blobId.ToString());
            await blob.DeleteIfExistsAsync();
        }

        public async Task<IEnumerable<FileObject>> ListAllFilesByType(FileType ftype, string folder = "")
        {
            string typeName = Enum.GetName(typeof(FileType), ftype).ToLower();
            if (ftype == FileType.Folder && (!string.IsNullOrEmpty(folder) && !FolderExists(folder)))
            {
                throw new Exception("Folder does not exists");
            }
            string folderPrefix = !string.IsNullOrWhiteSpace(folder) ? string.Concat(folder, '/') : string.Empty;
            string prefix = $"{folderPrefix}{typeName}_";

            var files = _blobContainer.GetBlobsAsync(prefix: prefix);
            var fileList = new List<FileObject>();
            await foreach (var file in files)
            {
                var obj = new FileObject(ftype, folder, file);
                fileList.Add(obj);
            }
            return fileList;
        }

        public async Task<bool> AddFolder(string folder, string parentFolder = "")
        {
            var folderType = Enum.GetName(FileType.Folder).ToLower();
            if (!string.IsNullOrWhiteSpace(parentFolder))
            {
                if (!FolderExists(parentFolder))
                {
                    throw new DirectoryNotFoundException();
                }
                folder = $"{parentFolder}/{folderType}_{folder}";
                if (FolderExists(folder))
                {
                    throw new ArgumentException("Folder with this name already exists");
                }
            }
            else
            {
                folder = $"{folderType}_{folder}";
            }
            var emptyStream = new MemoryStream();
            BlobClient blob = _blobContainer.GetBlobClient($"{folder}");
            await blob.UploadAsync(emptyStream, overwrite: true);
            return FolderExistsByFileId(folder);
        }
    }
}
