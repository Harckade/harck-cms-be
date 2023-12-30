using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;

namespace Harckade.CMS.PrivateController
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Editor, UserRoles.Administrator })]
    public class PrivateControllerFunctions : BaseController
    {
        private IArticleService _articleService;
        private IArticleBackupService _articleBackupService;
        private IFileService _fileService;
        private IDtoArticleMapper _dtoArticleMapper;
        private IDtoArticleBackupMapper _dtoArticleBackupMapper;
        private IDtoFileObjectMapper _dtoFileObjectMapper;
        private ILogger<PrivateControllerFunctions> _appInsights;
        private IJournalService _journalService;
        private ObservabilityId _oid;

        public PrivateControllerFunctions(IArticleService articleService, IArticleBackupService articleBackupService, IFileService fileService, IDtoArticleMapper dtoArticleMapper, IDtoArticleBackupMapper dtoArticleBackupMapper, IDtoFileObjectMapper dtoFileObjectMapper, ILogger<PrivateControllerFunctions> appInsights, IJournalService journalService)
        {
            _oid = new ObservabilityId();
            _articleService = articleService;
            _articleService.UpdateOid(_oid);
            _articleBackupService = articleBackupService;
            _articleBackupService.UpdateOid(_oid);
            _fileService = fileService;
            _fileService.UpdateOid(_oid);
            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _dtoArticleMapper = dtoArticleMapper;
            _dtoArticleBackupMapper = dtoArticleBackupMapper;
            _dtoFileObjectMapper = dtoFileObjectMapper;
            _appInsights = appInsights;
        }

        private async Task<HttpResponseData> ExecuteMethod(Func<Task<HttpResponseData>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: PrivateControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        private HttpResponseData ExecuteMethod(Func<HttpResponseData> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: PrivateController Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        #region Articles
        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("ListAllArticles")]
        public async Task<HttpResponseData> ListAllArticles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function ListAllArticles executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetAll();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Where(e => e.MarkedAsDeleted != true).Select(e => _dtoArticleMapper.DocumentToDto(e)), req);
            });
        }

        [Function("AddUpdateArticle")]
        public async Task<HttpResponseData> AddUpdateArticle([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/articles")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function AddUpdateArticle executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                Azure.Dtos.ArticleDto tmpArticle = JsonConvert.DeserializeObject<Azure.Dtos.ArticleDto>(body);
                tmpArticle.HtmlContentIsLoaded = true;
                string lang = string.Empty;
                Language language = default;
                var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
                if (queryDictionary["lang"] != null)
                {
                    lang = queryDictionary["lang"];
                    lang = lang.ToUpper();
                    lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                    Enum.TryParse(lang, true, out language);
                }
                if (language == default)
                {
                    await _journalService.AddEntryToQueue(context, $"create new article");
                }
                else
                {
                    await _journalService.AddEntryToQueue(context, $"{tmpArticle.Id} | {tmpArticle.Name[language]} | ${language}");
                }
                var result = await _articleService.AddOrUpdateArticle(context, tmpArticle, language);
                if (result.Success)
                {
                    return JsonResponse.Get(_dtoArticleMapper.DocumentToDto(result.Value), req);
                }
                return FailResponse(result, req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetArticleById")]
        public async Task<HttpResponseData> GetArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles/{articleId:guid}")] HttpRequestData req, Guid articleId)
        {
            _appInsights.LogInformation($"CMS: Function GetArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetById(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(_dtoArticleMapper.DocumentToDto(result.Value), req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetArticleContentById")]
        public async Task<HttpResponseData> GetArticleContentById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles/{articleId:guid}/content")] HttpRequestData req, Guid articleId)
        {
            _appInsights.LogInformation($"CMS: Function GetArticleContentById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                string lang = string.Empty;
                Language language;
                var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
                if (queryDictionary["lang"] != null)
                {
                    lang = queryDictionary["lang"];
                    lang = lang.ToUpper();
                    lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                }
                if (string.IsNullOrWhiteSpace(lang) || !Enum.TryParse(lang, true, out language))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                var result = await _articleService.DownloadArticleBinaryById(articleId, language);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var article = result.Value;
                if (article == null)
                {
                    return JsonResponse.Get("", req);
                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Body = article;
                return response;
            });
        }

        [Function("PublishArticleById")]
        public async Task<HttpResponseData> PublishArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "cms/articles/{articleId:guid}")] HttpRequestData req, Guid articleId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function PublishArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{articleId}");
                var result = await _articleService.PublishUnpublish(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("DeleteArticleById")]
        public async Task<HttpResponseData> DeleteArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/articles/{articleId:guid}")] HttpRequestData req, Guid articleId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function DeleteArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{articleId}");
                await _articleService.MarkArticleAsDeletedById(articleId);
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("ListAllDeletedArticles")]
        public async Task<HttpResponseData> ListAllDeletedArticles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles/deleted")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function ListAllDeletedArticles executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetArticlesMarkedAsDeleted();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Select(e => _dtoArticleMapper.DocumentToDto(e)), req);
            });
        }

        [Function("RecoverDeletedArticleById")]
        public async Task<HttpResponseData> RecoverDeletedArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "cms/articles/{articleId:guid}/recover")] HttpRequestData req, Guid articleId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function RecoverDeletedArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{articleId}");
                var result = await _articleService.RecoverArticleFromDeletedById(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("PermanentlyDeleteArticleById")]
        public async Task<HttpResponseData> PermanentlyDeleteArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/articles/{articleId:guid}/permanent")] HttpRequestData req, Guid articleId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function PermanentlyDeleteArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{articleId}");
                var result = await _articleService.DeleteArticleById(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }
        #endregion

        #region Articles Backups
        [Function("GetArticleHistory")]
        public async Task<HttpResponseData> GetArticleHistory([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles/{articleId:guid}/{lang}/history")] HttpRequestData req, Guid articleId, string lang)
        {
            _appInsights.LogInformation("CMS: Function GetArticleHistory executed", _oid);
            return await ExecuteMethod(async () =>
            {
                Language language = default;
                lang = lang.ToUpper();
                lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                if (string.IsNullOrWhiteSpace(lang) || !Enum.TryParse(lang, true, out language))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                var result = await _articleBackupService.GetById(articleId, language);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                var orderedEntries = entries.OrderBy(e => e.ModificationDate);
                return JsonResponse.Get(orderedEntries.SkipLast(1).Select(e => _dtoArticleBackupMapper.DocumentToDto(e)), req);
            });
        }

        [Function("GetBackupArticleContentById")]
        public async Task<HttpResponseData> GetBackupArticleContentById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/articles/{articleId:guid}/{lang}/history/{timestamp}")] HttpRequestData req, Guid articleId, string lang, string timestamp)
        {
            _appInsights.LogInformation($"CMS: Function GetBackupArticleContentById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                Language language = default;
                DateTime modificationDate = default;
                lang = lang.ToUpper();
                lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                if (string.IsNullOrWhiteSpace(lang) || !Enum.TryParse(lang, true, out language))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (string.IsNullOrWhiteSpace(timestamp) || !DateTime.TryParse(timestamp, default, DateTimeStyles.AssumeUniversal, out modificationDate))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                var result = await _articleBackupService.DownloadArticleBackupBinaryByIdAndDate(articleId, language, modificationDate);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var content = result.Value;
                if (content == null)
                {
                    return JsonResponse.Get("", req);
                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Body = content;
                return response;
            });
        }

        [Function("RestoreArticleToBackup")]
        public async Task<HttpResponseData> RestoreArticleToBackup([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "cms/articles/{articleId:guid}/{lang}/history/{timestamp}")] HttpRequestData req, Guid articleId, string lang, string timestamp, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function RestoreArticleToBackup executed. articleId: {articleId}, lang: {lang}, timestamp: {timestamp}", _oid);
            return await ExecuteMethod(async () =>
            {
                Language language = default;
                DateTime modificationDate = default;
                lang = lang.ToUpper();
                lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                if (articleId == default)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                if (string.IsNullOrWhiteSpace(lang) || !Enum.TryParse(lang, true, out language))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (string.IsNullOrWhiteSpace(timestamp) || !DateTime.TryParse(timestamp, default, DateTimeStyles.AssumeUniversal, out modificationDate))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                await _journalService.AddEntryToQueue(context, $"{articleId} | {language} | ${modificationDate}");
                var result = await _articleService.RestoreBackupArticle(context, articleId, language, modificationDate);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(_dtoArticleMapper.DocumentToDto(result.Value), req);
            });
        }
        #endregion

        #region Files
        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("ListFiles")]
        public async Task<HttpResponseData> ListFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/files/{*path}")] HttpRequestData req, string path="")
        {
            _appInsights.LogInformation("CMS: Function ListFiles executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var folderPath = "";
                if (path != null)
                {
                    folderPath = path;
                }
                var result = await _fileService.ListAllFilesByFolderPath(folderPath);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(result.Value.Select(f => _dtoFileObjectMapper.DocumentToDto(f)), req);
            });
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/files/{*path}")] HttpRequestData req, FunctionContext context, string path="")
        {
            _appInsights.LogInformation("CMS: Function UploadFile executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var folderPath = "";
                if (path != null)
                {
                    folderPath = path;
                }
                var parsedFormBody = await MultipartFormDataParser.ParseAsync(req.Body);
                var file = parsedFormBody.Files[0];
                var prefix = folderPath;
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefix = $"{prefix}/";
                }
                await _journalService.AddEntryToQueue(context, $"{prefix}{file.FileName}");
                using (var stream = file.Data)
                {
                    FileType fileType = FileType.Binary;
                    if (file.ContentType.StartsWith("image/"))
                    {
                        fileType = FileType.Image;
                    }
                    else if (file.ContentType.StartsWith("video/"))
                    {
                        fileType = FileType.Video;
                    }
                    else if (file.ContentType.StartsWith("audio/"))
                    {
                        fileType = FileType.Audio;
                    }
                    else if (file.ContentType == "application/pdf")
                    {
                        fileType = FileType.Pdf;
                    }
                    var result = await _fileService.UploadFile(fileType, file.FileName, stream, folderPath);
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("AddFolder")]
        public async Task<HttpResponseData> AddFolder([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/files")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function AddFolder executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                Azure.Domain.Folder folder;
                try
                {
                    folder = new Azure.Domain.Folder(JsonConvert.DeserializeObject<Azure.Dtos.FolderDto>(body));
                }
                catch
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                var prefix = folder.ParentFolder;
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefix = $"{prefix}/";
                }
                await _journalService.AddEntryToQueue(context, $"{prefix}{folder.Name}");
                var result = await _fileService.AddFolder(folder.Name, folder.ParentFolder);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("DeleteFile")]
        public async Task<HttpResponseData> DeleteFile([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/files/{*path}")] HttpRequestData req, FunctionContext context, string path="")
        {
            _appInsights.LogInformation("CMS: Function DeleteFile executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var urlParts = path.Split('/');
                if (urlParts.Length < 1)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                await _journalService.AddEntryToQueue(context, $"{path}");

                var fileType = urlParts[urlParts.Length - 1].Split('_')[0];

                FileType ftype = FileType.Binary;
                try
                {
                    ftype = (FileType)Enum.Parse(typeof(FileType), fileType, true);
                }
                catch
                {
                    _appInsights.LogError("Function DownloadFile cannot parse file type", fileType);
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                if (ftype == FileType.Folder)
                {
                    var result = await _fileService.DeleteFolderByPath(Utils.FolderHelper.FileIdToPath(path, Enum.GetName(FileType.Folder)));
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                }
                else
                {
                    var result = await _fileService.DeleteFileById(new BlobId(path));
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("ZipFiles")]
        public async Task<HttpResponseData> ZipFiles([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/zip-files")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function ZipFiles executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                IEnumerable<string> filesToZip = (IEnumerable<string>)JsonConvert.DeserializeObject<IEnumerable<string>>(body);
                var result = await _fileService.ZipFiles(filesToZip.Select(f => new BlobId(f)));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var zipFile = result.Value;
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Body = zipFile;
                response.Headers.Add("Content-Type", "application/zip");
                return response;
            });
        }
        #endregion

        [Function("LaunchDeployment")]
        public async Task<HttpResponseData> LaunchDeployment([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/deploy")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function LaunchDeployment executed", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context);
                var deploymentLaunched = await _articleService.LaunchDeployment();
                if (deploymentLaunched.Failed)
                {
                    return FailResponse(deploymentLaunched, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Authorize(IsPublic = true)]
        [Function("RobotsTxt")]
        public async Task<string> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "robots.txt")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function GetRobotsTxt executed", _oid);
            var sb = new StringBuilder();
            sb.AppendLine("user-agent: *");
            sb.AppendLine("Disallow: /");
            return await Task.FromResult(sb.ToString());
        }
    }
}
