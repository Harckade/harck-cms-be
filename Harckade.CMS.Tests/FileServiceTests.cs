using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Harckade.CMS.Tests
{
    [TestClass]
    public class FileServiceTests
    {
        IFileService _fileService;
        private string _prefix;
        private string _parentFolder = "parentFolder";
        private string _subFolder = "subFolder";

        [TestInitialize]
        public void init()
        {
            var connectionString = "UseDevelopmentStorage=true";
            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
            services.AddScoped<IFileService, FileService>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            _fileService = serviceProvider.GetService<IFileService>();
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "_").Replace(":", "_")}";
            _parentFolder = $"{_prefix}_{_parentFolder}";
            _subFolder = $"{_prefix}_{_subFolder}";
        }

        [TestMethod]
        public async Task ListAllUrlsFromRoot()
        {
            var result = await _fileService.ListAllFilesByFolderPath();
            Assert.IsTrue(result.Success);
            var files = result.Value;
            Assert.IsNotNull(files);
            Assert.IsTrue(files.Any());
        }

        [TestMethod]
        public async Task<FileObject> AddFolderToRoot()
        {
            await _fileService.AddFolder(_prefix);
            var rootFoldersResult = await _fileService.ListAllFolderFilesByType(Azure.Enums.FileType.Folder);
            Assert.IsTrue(rootFoldersResult.Success);
            var rootFolders = rootFoldersResult.Value;
            var newFolder = rootFolders.Where(f => f.Name == _prefix).FirstOrDefault();
            Assert.IsNotNull(newFolder);
            Assert.IsNotNull(newFolder);
            Assert.IsTrue(newFolder.FileType == Azure.Enums.FileType.Folder);
            return newFolder;
        }

        [TestMethod]
        public async Task<FileObject> AddFolderToSubFolder()
        {
            var createFolder = await _fileService.AddFolder(_parentFolder);
            Assert.IsTrue(createFolder.Success);
            createFolder = await _fileService.AddFolder(_subFolder, _parentFolder);
            Assert.IsTrue(createFolder.Success);
            var rootFoldersResult = await _fileService.ListAllFolderFilesByType(Azure.Enums.FileType.Folder);
            Assert.IsTrue(rootFoldersResult.Success);
            var rootFolders = rootFoldersResult.Value;
            var newFolder = rootFolders.Where(f => f.Name == _parentFolder).FirstOrDefault();
            Assert.IsNotNull(newFolder);
            var parentFolderResult = await _fileService.ListAllFolderFilesByType(Azure.Enums.FileType.Folder, _parentFolder);
            Assert.IsTrue(parentFolderResult.Success);
            var parentFolder = parentFolderResult.Value;
            var subfolder = parentFolder.Where(f => f.Name == _subFolder).FirstOrDefault();
            Assert.IsNotNull(subfolder);
            Assert.IsTrue(subfolder.FileType == Azure.Enums.FileType.Folder);
            return newFolder;
        }

        [TestMethod]
        public async Task DeleteFolderByPath()
        {
            await AddFolderToRoot();
            Thread.Sleep(3600);
            var deleteOperationResult = await _fileService.DeleteFolderByPath(_prefix);
            Assert.IsTrue(deleteOperationResult.Success);
        }

        [TestMethod]
        public async Task DeleteFolderByObject()
        {
            var folder = await AddFolderToRoot();
            Thread.Sleep(3600);
            var deleteOperationResult = await _fileService.DeleteFolderByObject(folder);
            Assert.IsTrue(deleteOperationResult.Success);
        }

        [TestMethod]
        public async Task ListAllFolderFilesByType()
        {
            await AddFolderToSubFolder();
            Thread.Sleep(3600);
            var result = await _fileService.ListAllFolderFilesByType(Azure.Enums.FileType.Folder, _parentFolder);
            Assert.IsTrue(result.Success);
            var folderFiles = result.Value;
            Assert.IsNotNull(folderFiles);
            Assert.IsTrue(folderFiles.Any());
        }

        [TestMethod]
        public async Task ListAllFolderObjectFilesByType()
        {
            var folder = await AddFolderToSubFolder();
            Thread.Sleep(3600);
            var result = await _fileService.ListAllFolderObjectFilesByType(Azure.Enums.FileType.Folder, folder);
            Assert.IsTrue(result.Success);
            var folderFiles = result.Value;
            Assert.IsNotNull(folderFiles);
            Assert.IsTrue(folderFiles.Any());
        }

        [TestMethod]
        public async Task ListAllFilesByFolderPath()
        {
            await AddFolderToSubFolder();
            Thread.Sleep(3600);
            var result = await _fileService.ListAllFilesByFolderPath(_parentFolder);
            Assert.IsTrue(result.Success);
            var folderFiles = result.Value;
            Assert.IsNotNull(folderFiles);
            Assert.IsTrue(folderFiles.Any());
        }

        [TestMethod]
        public async Task UploadFile()
        {
            await AddFolderToRoot();
            Thread.Sleep(3600);
            var path = Path.Combine(Environment.CurrentDirectory, "test_files\\article.txt");
            var content = System.IO.File.ReadAllText(path);
            Assert.IsNotNull(content);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(content));
            using (var stream = Html.GenerateStreamFromString(content))
            {
                var result = await _fileService.UploadFile(Azure.Enums.FileType.Binary, "testFile.txt", stream, _prefix);
                Assert.IsTrue(result.Success);
            }
        }

        [TestMethod]
        public async Task DownloadFile()
        {
            await UploadFile();
            Thread.Sleep(3600);
            var result = await _fileService.DownloadFile(new BlobId($"{_prefix}/binary_testFile.txt"));
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Value);
            Assert.IsTrue(result.Value.Length > 0);
        }

        [TestMethod]
        public async Task DeleteFileById()
        {
            await UploadFile();
            Thread.Sleep(3600);
            var result = await _fileService.DeleteFileById(new BlobId($"{_prefix}/binary_testFile.txt"));
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task ZipFiles()
        {
            await UploadFile();
            Thread.Sleep(3600);
            var result = await _fileService.ZipFiles(new List<BlobId>() { new BlobId($"{_prefix}/binary_testFile.txt") });
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Value);
            Assert.IsTrue(result.Value.Length > 0);
        }

        [TestMethod]
        public async Task ZipFilesByObject()
        {
            await UploadFile();
            Thread.Sleep(3600);
            var uploadedFiles = await _fileService.ListAllFilesByFolderPath(_prefix);
            Assert.IsTrue(uploadedFiles.Success);
            var file = uploadedFiles.Value.FirstOrDefault();
            Assert.IsNotNull(file);
            var result = await _fileService.ZipFilesByObject(new List<FileObject>() { file }, _prefix);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Value);
            Assert.IsTrue(result.Value.Length > 0);
        }

        [TestMethod]
        public async Task ClearAfterTests()
        {
            var allFolders = await _fileService.ListAllFolderFilesByType(Azure.Enums.FileType.Folder);
            Assert.IsTrue(allFolders.Success);
            foreach (var folder in allFolders.Value)
            {
                if (folder.Name.StartsWith("unit_test"))
                {
                    var result = await _fileService.DeleteFolderByObject(folder);
                    Assert.IsTrue(result.Success);
                }
            }
        }
    }
}
