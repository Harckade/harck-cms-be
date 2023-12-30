using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace Harckade.CMS.Services
{
    public class FileService : ServiceBase, IFileService
    {
        private IBlobRepository _blobRepository;
        private readonly ILogger<FileService> _appInsights;

        public FileService(IBlobRepository blobRepository, ILogger<FileService> appInsights)
        {
            _blobRepository = blobRepository;
            _appInsights = appInsights;
            _oidIsSet = false;
        }

        public async Task<Result> AddFolder(string folder, string parentFolder = "")
        {
            _appInsights.LogInformation($"FileService | AddFolder: {folder} | {parentFolder}", _oid);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return Result.Fail(Failure.InvalidInput);
            }
            await _blobRepository.AddFolder(folder, parentFolder);
            return Result.Ok();
        }

        private string GetFolderNameAsPath(FileObject folder)
        {
            _appInsights.LogDebug($"FileService | GetFolderNameAsPath: {folder.Id}", _oid);
            if (folder.FileType != FileType.Folder)
            {
                throw new InvalidCastException();
            }
            var folderParts = folder.Id.ToString().Split('/');
            var folderPath = folder.Name;
            if (folderParts.Length > 1)
            {
                folderPath = "";
                for (var i = 0; i < folderParts.Length - 1; i++)
                {
                    folderPath = $"{folderPath}{(!string.IsNullOrWhiteSpace(folderPath) ? "/" : "")}{folderParts[i]}";
                }
                folderPath = $"{folderPath}/{folder.Name}";
            }
            return folderPath;
        }

        private Result IsFolder(FileObject folder)
        {
            _appInsights.LogDebug($"FileService | IsFolder: {folder.Id}", _oid);
            if (folder.FileType != FileType.Folder)
            {
                throw new InvalidCastException();
            }
            if (!folder.IsLoaded)
            {
                return Result.Fail(Failure.FolderNotInitialized);
            }
            return Result.Ok();
        }

        public async Task<Result> DeleteFolderByObject(FileObject folder)
        {
            _appInsights.LogInformation($"FileService | DeleteFolderByObject: {folder.Id}", _oid);
            var isFolder = IsFolder(folder);
            if (isFolder.Failed)
            {
                return Result.Fail(isFolder.FailureReason);
            }
            await DeleteFolderByPath(GetFolderNameAsPath(folder));
            return Result.Ok();
        }

        public async Task<Result> DeleteFolderByPath(string folderPath)
        {
            _appInsights.LogInformation($"FileService | DeleteFolderByPath: {folderPath}", _oid);
            var filesToDelete = new List<FileObject>();
            var filesInFolderResult = await ListAllFilesByFolderPath(folderPath);
            if (filesInFolderResult.Failed)
            {
                return Result.Fail(filesInFolderResult.FailureReason);
            }
            var filesInFolder = filesInFolderResult.Value;
            filesToDelete.AddRange(filesInFolder);

            var blobId = new BlobId(Utils.FolderHelper.FolderPathToId(folderPath, Enum.GetName(FileType.Folder).ToLower())); //fileObject itself
            filesToDelete.Add(new FileObject(blobId));
            foreach (var file in filesToDelete)
            {
                if (file.FileType == FileType.Folder)
                {
                    var deleteFolderResult = await DeleteFolderByObject(file);
                    if (deleteFolderResult.Failed)
                    {
                        return Result.Fail(deleteFolderResult.FailureReason);
                    }
                }
                var deleteFileResult = await DeleteFileById(file.Id);
                if (deleteFileResult.Failed)
                {
                    return Result.Fail(deleteFileResult.FailureReason);
                }
            }
            return Result.Ok();
        }

        public async Task<Result> DeleteFileById(BlobId fileId)
        {
            _appInsights.LogInformation($"FileService | DeleteFileById: {fileId}", _oid);
            await _blobRepository.DeleteBlobAsync(fileId);
            return Result.Ok();
        }

        public async Task<Result<Stream>> DownloadFile(BlobId blobId)
        {
            _appInsights.LogInformation($"FileService | DownloadFile: {blobId}", _oid);
            var file = await _blobRepository.FetchFileObjectAsync(blobId);
            if (file == null)
            {
                return Result.Ok<Stream>(null);
            }
            if (file.FileType == FileType.Folder)
            {
                var zipFilesResult = await ZipFiles(new List<BlobId>() { blobId });
                if (zipFilesResult.Failed)
                {
                    return Result.Fail<Stream>(zipFilesResult.FailureReason);
                }
                var zipFiles = zipFilesResult.Value;
                return Result.Ok(zipFiles);
            }
            var downloadedFile = await _blobRepository.DownloadFileAsync(blobId);
            return Result.Ok(downloadedFile);
        }

        public async Task<Result<IEnumerable<FileObject>>> ListAllFolderFilesByType(FileType ftype, string folderPath = "")
        {
            _appInsights.LogInformation($"FileService | ListAllFolderFilesByType: {ftype} | {folderPath}", _oid);
            if (ftype == default)
            {
                return Result.Fail<IEnumerable<FileObject>>(Failure.InvalidInput);
            }
            var files = await _blobRepository.ListAllFilesByType(ftype, folderPath);
            return Result.Ok(files);
        }

        public async Task<Result<IEnumerable<FileObject>>> ListAllFolderObjectFilesByType(FileType ftype, FileObject folder)
        {
            _appInsights.LogDebug($"FileService | ListAllFolderObjectFilesByType: {ftype} | {folder.Id}", _oid);
            var isFolder = IsFolder(folder);
            if (isFolder.Failed)
            {
                return Result.Fail<IEnumerable<FileObject>>(isFolder.FailureReason);
            }
            return await ListAllFolderFilesByType(ftype, GetFolderNameAsPath(folder));
        }

        public async Task<Result<IEnumerable<FileObject>>> ListAllFilesByFolderPath(string folderPath = "", IEnumerable<FileType> ignoreTypeList = default)
        {
            _appInsights.LogInformation($"FileService | ListAllFilesByFolderPath: {folderPath} | {ignoreTypeList}", _oid);
            var filesList = new List<FileObject>();
            var allTypes = (IEnumerable<FileType>)Enum.GetValues(typeof(FileType));
            try
            {
                foreach (var ftype in allTypes.Where(t => t!= FileType.Invalid))
                {
                    if (ignoreTypeList == null || !ignoreTypeList.Contains(ftype))
                    {

                        var filesFromFolderResult = await ListAllFolderFilesByType(ftype, folderPath);
                        if (filesFromFolderResult.Success)
                        {
                            filesList.AddRange(filesFromFolderResult.Value);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Folder does not exists")
                {
                    return Result.Fail<IEnumerable<FileObject>>(Failure.FolderNotFound, folderPath);
                }
            }
            return Result.Ok<IEnumerable<FileObject>>(filesList);
        }

        public async Task<Result> UploadFile(FileType ftype, string fileName, Stream fileStream, string folder = "")
        {
            _appInsights.LogInformation($"FileService | UploadFile: {ftype} | {fileName} | {folder}", _oid);
            if (ftype == FileType.Invalid)
            {
                throw new ArgumentException(nameof(ftype));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result.Fail(Failure.InvalidInput);
            }
            try
            {
                await _blobRepository.UploadBinary(new BlobId($"{Enum.GetName(typeof(FileType), ftype).ToLower()}_{fileName}"), fileStream, folder);
            }
            catch (Exception e)
            {
                if (e.Message == "Folder does not exists")
                {
                    return Result.Fail(Failure.FolderNotFound, folder);
                }
            }
            return Result.Ok();
        }

        private async Task<Result<IEnumerable<FileObject>>> GetAllFilesRecursive(string folderPath)
        {
            _appInsights.LogDebug($"FileService | GetAllFilesRecursive: {folderPath}", _oid);
            var filesList = new List<FileObject>();
            var filesInFolderResult = await ListAllFilesByFolderPath(folderPath);
            if (filesInFolderResult.Failed)
            {
                _appInsights.LogError($"FileService | GetAllFilesRecursive Error: {folderPath}", _oid);
                return Result.Fail<IEnumerable<FileObject>>(filesInFolderResult.FailureReason);
            }
            filesList.AddRange(filesInFolderResult.Value);
            var filesListToReturn = new List<FileObject>();
            foreach (var file in filesList)
            {
                if (file.FileType == FileType.Folder)
                {
                    var filesResult = await GetAllFilesRecursive(GetFolderNameAsPath(file));
                    if (filesResult.Failed)
                    {
                        return Result.Fail<IEnumerable<FileObject>>(filesResult.FailureReason);
                    }
                    filesListToReturn.AddRange(filesResult.Value);
                }
                else
                {
                    filesListToReturn.Add(file);
                }
            }
            return Result.Ok<IEnumerable<FileObject>>(filesListToReturn);
        }

        private string getParentFolder(BlobId fileId)
        {
            _appInsights.LogDebug($"FileService | getParentFolder: {fileId}", _oid);
            var parts = fileId.ToString().Split('/');
            return parts.Length == 1 ? string.Empty : string.Join('/', parts.SkipLast(1));
        }

        public async Task<Result<Stream>> ZipFiles(IEnumerable<BlobId> fileIds)
        {
            _appInsights.LogInformation($"FileService | ZipFiles: {fileIds}", _oid);
            var allFiles = new List<FileObject>();
            foreach (var fileId in fileIds)
            {
                var item = await _blobRepository.FetchFileObjectAsync(fileId);
                if (item.FileType == FileType.Folder)
                {
                    var getAllFilesResult = await GetAllFilesRecursive(GetFolderNameAsPath(item));
                    if (getAllFilesResult.Failed)
                    {
                        return Result.Fail<Stream>(getAllFilesResult.FailureReason);
                    }
                    allFiles.AddRange(getAllFilesResult.Value);
                }
                else
                {
                    allFiles.Add(item);
                }
            }
            var parentFolder = getParentFolder(fileIds.FirstOrDefault());
            return await ZipFilesByObject(allFiles, parentFolder);
        }

        public async Task<Result<Stream>> ZipFilesByObject(IEnumerable<FileObject> files, string parentFolder)
        {
            _appInsights.LogInformation($"FileService | ZipFilesByObject: {files.Select(f => f.Id)}", _oid);
            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in files)
                {

                    var fileFullPath = Utils.FolderHelper.FileIdToPath(item.Id.ToString(), Enum.GetName(typeof(FileType), item.FileType));
                    if (!string.IsNullOrEmpty(parentFolder))
                    {
                        fileFullPath = fileFullPath.Substring(parentFolder.Length + 1, fileFullPath.Length - 1 - parentFolder.Length);
                    }
                    var zipArchiveEntry = archive.CreateEntry(fileFullPath, CompressionLevel.Fastest);
                    using (var entryStream = zipArchiveEntry.Open())
                    {
                        var downloadedFileResult = await DownloadFile(item.Id);
                        if (downloadedFileResult.Failed)
                        {
                            return Result.Fail<Stream>(downloadedFileResult.FailureReason);
                        }
                        using (var fileToCompressStream = downloadedFileResult.Value)
                        {
                            await fileToCompressStream.CopyToAsync(entryStream);
                        }
                    }
                }
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return Result.Ok<Stream>(memoryStream);

        }
    }
}
