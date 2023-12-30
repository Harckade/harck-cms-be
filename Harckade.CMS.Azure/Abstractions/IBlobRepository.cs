using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IBlobRepository
    {
        Task DeleteBlobAsync(BlobId blobId);
        Task<Stream> DownloadFileAsync(BlobId blobId);
        Task UploadBinary(BlobId blobId, Stream fileStream, string folder = "");
        Task<bool> AddFolder(string folder, string parentFolder = "");
        Task<IEnumerable<FileObject>> ListAllFilesByType(FileType ftype, string folder="");
        Task<FileObject> FetchFileObjectAsync(BlobId blobId);
    }
}
