using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IFileService : IServiceBase
    {
        /// <summary>
        /// Delete a specific file by blob identifier
        /// </summary>
        /// <param name="fileId">blob identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteFileById(BlobId fileId);
        /// <summary>
        /// Upload a file to the the system.
        /// The file will be available for everyone access.
        /// </summary>
        /// <param name="ftype">File media type</param>
        /// <param name="fileName">The name must respect Azure blob storage requirements</param>
        /// <param name="fileStream">File's content stream</param>
        /// <param name="folder">By default files are uploaded to the root folder</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> UploadFile(FileType ftype, string fileName, Stream fileStream, string folder = "");
        /// <summary>
        /// Download a file by blob identifier
        /// </summary>
        /// <param name="blobId">blob identifier</param>
        /// <returns>File's content stream</returns>
        Task<Result<Stream>> DownloadFile(BlobId blobId);
        /// <summary>
        /// Retrieve a collection of existing files of a specific type that are located on a specific folder
        /// </summary>
        /// <param name="ftype">File media type</param>
        /// <param name="folderPath">Default value will point to the root folder</param>
        /// <returns>A collection of files objects: only metadata associated with files is returned</returns>
        Task<Result<IEnumerable<FileObject>>> ListAllFolderFilesByType(FileType ftype, string folderPath = "");
        /// <summary>
        /// Retrieve a collection of existing files of a specific type that are located on a specific folder
        /// </summary>
        /// <param name="ftype">File media type</param>
        /// <param name="folder">FileObject type must be Folder. Otherwise this method will return a failure.</param>
        /// <returns>A collection of files objects: only metadata associated with files is returned</returns>
        Task<Result<IEnumerable<FileObject>>> ListAllFolderObjectFilesByType(FileType ftype, FileObject folder);
        /// <summary>
        /// Retrieve a collection of all files that exist on a specific folder
        /// </summary>
        /// <param name="folderPath">Default value will point to the root folder</param>
        /// <param name="ignoreTypeList">Ignore specific type of files</param>
        /// <returns>A collection of files objects: only metadata associated with files is returned</returns>
        Task<Result<IEnumerable<FileObject>>> ListAllFilesByFolderPath(string folderPath = "", IEnumerable<FileType> ignoreTypeList = default);
        /// <summary>
        /// Create a new folder. If no parentFolder is provided, the new one will be created on the root folder.
        /// </summary>
        /// <param name="folder">Folder name. Must respect Azure blob storage name requirements</param>
        /// <param name="parentFolder">Default value will point to the root folder</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> AddFolder(string folder, string parentFolder = "");
        /// <summary>
        /// Delete the folder and all files that it contains
        /// </summary>
        /// <param name="folderPath">Default value will point to the root folder</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteFolderByPath(string folderPath);
        /// <summary>
        /// Delete the folder and all files that it contains
        /// </summary>
        /// <param name="folder">FileObject type must be Folder. Otherwise this method will return a failure.</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteFolderByObject(FileObject folder);
        /// <summary>
        /// Retrieve a ZIP folder file as a stream containing a specific set of files
        /// </summary>
        /// <param name="fileIds">Files to be included on the zip</param>
        /// <returns>A ZIP file stream</returns>
        Task<Result<Stream>> ZipFiles(IEnumerable<BlobId> fileIds);
        /// <summary>
        /// Retrieve a ZIP folder file as a stream containing a specific set of files
        /// </summary>
        /// <param name="files">Files to be included on the zip</param>
        /// <param name="parentFolder">All provided files must belong to the same parent folder</param>
        /// <returns></returns>
        Task<Result<Stream>> ZipFilesByObject(IEnumerable<FileObject> files, string parentFolder);
    }
}
