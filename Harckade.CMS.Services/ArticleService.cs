using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace Harckade.CMS.Services
{
    public class ArticleService : ServiceBase, IArticleService
    {
        private readonly IConfiguration _configuration;
        private IArticleRepository _articleRepository;
        private IFileService _fileService;
        private IArticleBackupService _articleBackupService;
        private ISettingsRepository _settingsRepository;
        private IArticleHelperRepository _articleHelperRepository;
        private IBlobRepository _blobRepository;
        private IDtoArticleMapper _dtoArticleMapper;
        private readonly ILogger<ArticleService> _appInsights;

        public ArticleService(IConfiguration configuration, IArticleBackupService articleBackupService, IArticleRepository articleRepository, IArticleHelperRepository articleHelperRepository, IDtoArticleMapper dtoArticleMapper, IBlobRepository blobRepository, IFileService fileService, ISettingsRepository settingsRepository, ILogger<ArticleService> appInsights)
        {
            _configuration = configuration;
            _articleBackupService = articleBackupService;
            _articleRepository = articleRepository;
            _articleHelperRepository = articleHelperRepository;
            _blobRepository = blobRepository;
            _fileService = fileService;
            _settingsRepository = settingsRepository;
            _dtoArticleMapper = dtoArticleMapper;
            _appInsights = appInsights;
            _oidIsSet = false;
        }

        /// <summary>
        /// Update article name's properties for the the auxiliar repositories that are used to retrieve an article by title.
        /// </summary>
        /// <param name="article">Target article</param>
        private async Task AddOrUpdateAuxiliar(Article article)
        {
            _appInsights.LogDebug($"ArticleService | AddOrUpdateAuxiliar: {article.Id}", _oid);
            await _articleRepository.InsertOrUpdate(article);
            var articleHelpers = await _articleHelperRepository.FindByArticle(article);
            foreach (var title in article.NameNoDiacritics)
            {
                var articleHelperName = articleHelpers.FirstOrDefault(d => d.Language == title.Key);
                string base64title = "";
                if (!string.IsNullOrWhiteSpace(title.Value) && articleHelperName != null)
                {
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(title.Value);
                    base64title = $"{Convert.ToBase64String(plainTextBytes).Replace("/", "ç")}_{title.Key}";
                }
                if (!string.IsNullOrWhiteSpace(title.Value) && (articleHelperName == null || articleHelperName.EncodedTitle != base64title))
                {
                    if (articleHelperName != null)
                    {
                        await _articleHelperRepository.DeleteByEncodedTitle(articleHelperName.EncodedTitle);
                    }
                    articleHelperName = new ArticleHelper(title.Value, article.Id, title.Key);
                    await _articleHelperRepository.InsertOrUpdate(articleHelperName);
                }
            }
        }

        /// <summary>
        /// Check if an article with a specific name already existis for a given language
        /// </summary>
        /// <param name="art">Article provided by client</param>
        /// <param name="lang">Target language</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        private async Task<Result> CheckArticleWithSameName(ArticleDto art, Language lang)
        {
            _appInsights.LogDebug($"ArticleService | CheckArticleWithSameName: {art.Id} | {lang}", _oid);
            var articleWithSameName = await GetByTitle(art.NameNoDiacritics[lang], lang);
            if (articleWithSameName.Success && articleWithSameName.Value.Id != art.Id)
            {
                return Result.Fail(Failure.DuplicateArticleTitle, $"{lang}");
            }
            if (articleWithSameName.Failed && articleWithSameName.FailureReason != Failure.ArticleNotFound)
            {
                return Result.Fail(articleWithSameName.FailureReason);
            }
            return Result.Ok();
        }

        /// <summary>
        /// Update system's state to inform later the user that a new deployment is necessary.
        /// </summary>
        /// <param name="requiresDeployment"></param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        private async Task<Result> UpdateSettingDeploymentInfo(bool requiresDeployment = false)
        {
            var settings = await _settingsRepository.Get();
            settings.UpdateDeploymentInfo(requiresDeployment);
            await _settingsRepository.InsertOrUpdate(settings, false);
            return Result.Ok();
        }

        public async Task<Result<Article>> AddOrUpdateArticle(FunctionContext context, ArticleDto tmpArticle, Language lang = default, bool skipBackup = false)
        {
            _appInsights.LogInformation($"ArticleService | AddOrUpdateArticle: {tmpArticle.Id} | {lang} | {skipBackup}", _oid);
            bool isNewArticle = false;

            Article article;
            tmpArticle.InitNoDiacritics();
            var settings = await _settingsRepository.Get();
            if (tmpArticle.Id != default)
            {
                var retrievedArticle = await GetById(tmpArticle.Id);
                if (retrievedArticle.Failed)
                {
                    return Result.Fail<Article>(retrievedArticle.FailureReason);
                }
                article = retrievedArticle.Value;
                if (article.MarkedAsDeleted == true)
                {
                    return Result.Fail<Article>(Failure.NotPossibleEditDeletedArticle);
                }
                if (lang == default)
                {
                    return Result.Fail<Article>(Failure.UndefinedLanguage);
                }
                var articleWithSameNameCheck = await CheckArticleWithSameName(tmpArticle, lang);
                if (articleWithSameNameCheck.Failed)
                {
                    return Result.Fail<Article>(articleWithSameNameCheck.FailureReason);
                }
            }
            else
            {
                isNewArticle = true;
                skipBackup = true;
                article = new Article(tmpArticle);
                foreach (var title in tmpArticle.NameNoDiacritics)
                {
                    if (title.Key == Language.None)
                    {
                        continue;
                    }
                    var articleWithSameNameCheck = await CheckArticleWithSameName(tmpArticle, title.Key);
                    if (articleWithSameNameCheck.Failed)
                    {
                        return Result.Fail<Article>(articleWithSameNameCheck.FailureReason, Enum.GetName(title.Key));
                    }
                }
            }
            var user = GetUser(context);
            if (!isNewArticle)
            {
                var shouldCreateNewBackupEntry = false;
                if (tmpArticle.Description.ContainsKey(lang) && article.Description[lang] != tmpArticle.Description[lang])
                {
                    article.UpdateDescriptionForLanguage(tmpArticle.Description[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }
                if (tmpArticle.Name.ContainsKey(lang) && article.Name[lang] != tmpArticle.Name[lang])
                {
                    article.UpdateNameForLanguage(tmpArticle.Name[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }

                if (tmpArticle.ImageUrl.ContainsKey(lang) && article.ImageUrl[lang] != tmpArticle.ImageUrl[lang])
                {
                    article.UpdateImageUrlForLanguage(tmpArticle.ImageUrl[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }
                if (tmpArticle.ImageDescription.ContainsKey(lang) &&  article.ImageDescription[lang] != tmpArticle.ImageDescription[lang])
                {
                    article.UpdateImageDescriptionForLanguage(tmpArticle.ImageDescription[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }
                if (tmpArticle.Tags.ContainsKey(lang) && JsonConvert.SerializeObject(article.Tags[lang]) != JsonConvert.SerializeObject(tmpArticle.Tags[lang]))
                {
                    article.UpdateTagsForLanguage(tmpArticle.Tags[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }
                if (tmpArticle.Author.ContainsKey(lang) && article.Author[lang] != tmpArticle.Author[lang])
                {
                    article.UpdateAuthorForLanguage(tmpArticle.Author[lang], lang);
                    shouldCreateNewBackupEntry = true;
                }

                string htmlCode = tmpArticle.HtmlContent == null || string.IsNullOrWhiteSpace(tmpArticle.HtmlContent[lang]) ? string.Empty : JsonConvert.SerializeObject(tmpArticle.HtmlContent[lang]);
                DateTime modificationTime = DateTime.UtcNow.ToUniversalTime();
                var articleBackup = new ArticleBackup(article.Id, modificationTime, article.Name[lang], lang, article.Description[lang], article.ImageUrl[lang], article.ImageDescription[lang], article.Tags[lang], article.Author[lang], user);
                
                var articleHash = Utils.Hash.Sha256(htmlCode);
                if (article.ContentHash == null || !article.ContentHash.Any() || articleHash != article.ContentHash[lang])
                {
                    shouldCreateNewBackupEntry = true;
                    using (var stream = Utils.Html.GenerateStreamFromString(htmlCode))
                    {
                        await UploadArticleBinary(article, stream, lang);
                    }
                    if (skipBackup == false)
                    {
                        using (var stream = Utils.Html.GenerateStreamFromString(htmlCode))
                        {
                            await _articleBackupService.UploadArticleBackupBinary(articleBackup, stream, lang);
                        }
                        articleBackup.UpdateContentId();
                    }
                }
                else
                {
                    if (skipBackup == false)
                    {
                        var previousBackups = await _articleBackupService.GetById(article.Id, lang);
                        if (previousBackups.Failed)
                        {
                            return Result.Fail<Article>(previousBackups.FailureReason);
                        }
                        articleBackup.UpdateContentIdUsingBackups(previousBackups.Value);
                    }
                }
                article.UpdateContentHashForLanguage(articleHash, lang);

                if (shouldCreateNewBackupEntry && skipBackup == false)
                {
                    await _articleBackupService.AddArticleBackup(context, articleBackup);
                }
                if (article.Published == true)
                {
                    await UpdateSettingDeploymentInfo(true);
                    await LaunchDeployment();
                }
            }
            else
            {
                if (skipBackup == false || isNewArticle)
                {
                    foreach (var _lang in settings.Languages)
                    {
                        var _name = article.Name.ContainsKey(_lang) ? article.Name[_lang] : "";
                        var _author = article.Author.ContainsKey(_lang) ? article.Author[_lang] : "";
                        var _description = article.Description.ContainsKey(_lang) ? article.Description[_lang] : "";
                        var _imageUrl = article.ImageUrl.ContainsKey(_lang) ? article.ImageUrl[_lang] : "";
                        var _imageDescription = article.ImageDescription.ContainsKey(_lang) ? article.ImageDescription[_lang] : "";
                        var _tags = article.Tags.ContainsKey(_lang) ? article.Tags[_lang] : new List<string>();
                        DateTime modificationTime = DateTime.UtcNow.ToUniversalTime();
                        var articleBackup = new ArticleBackup(article.Id, modificationTime, _name, _lang, _description, _imageUrl, _imageDescription, _tags, _author, user);
                        await _articleBackupService.AddArticleBackup(context, articleBackup);
                    }
                }
            }
            await AddOrUpdateAuxiliar(article);
            return Result.Ok<Article>(article);
        }

        public async Task<Result> DeleteArticle(Article article)
        {
            _appInsights.LogDebug($"ArticleService | DeleteArticle: {article.Id}", _oid);
            return await DeleteArticleById(article.Id);
        }

        public async Task<Result> DeleteArticleById(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleService | DeleteArticletById: {articleId}", _oid);
            var retrievedArticle = await GetById(articleId);
            if (retrievedArticle.Failed)
            {
                return Result.Fail(retrievedArticle.FailureReason);
            }
            var article = retrievedArticle.Value;
            if (article.MarkedAsDeleted == false)
            {
                return Result.Fail(Failure.NotMarkedAsDeleted);
            }
            var contetExistForLanguageList = article.ContentHash.Where(hash => !string.IsNullOrWhiteSpace(hash.Value)).Select(hash => hash.Key);
            foreach (var lang in contetExistForLanguageList)
            {
                await _fileService.DeleteFileById(new BlobId($"{article.Id}_{lang}"));
            }
            
            await _articleRepository.Delete(articleId);

            var articleHelpers = await _articleHelperRepository.FindByArticle(article);
            foreach (var articleHelper in articleHelpers)
            {
                await _articleHelperRepository.DeleteByEncodedTitle(articleHelper.EncodedTitle);
            }

            foreach (var lang in contetExistForLanguageList)
            {
                var backupContentFiles = await _articleBackupService.GetById(article.Id, lang);
                if (backupContentFiles.Success && backupContentFiles.Value.Any())
                {
                    foreach (var backupFile in backupContentFiles.Value)
                    {
                        var backupId = $"{article.Id}_{lang}_{backupFile.ModificationDate.ToString("MM/dd/yyyy HH:mm:ss")}";
                        await _fileService.DeleteFileById(new BlobId(backupId));
                    }
                }
            }

            var deleteArticleBackups = await _articleBackupService.DeleteArticleBackupById(articleId);
            if (deleteArticleBackups.Failed)
            {
                return Result.Fail(deleteArticleBackups.FailureReason);
            }
            if (article.PublishDate != default)
            {
                await LaunchDeployment();
            }
            return Result.Ok();
        }

        public async Task<Result<Stream>> DownloadArticleBinary(Article article, Language lang)
        {
            _appInsights.LogDebug($"ArticleService | DownloadArticleBinary: {article.Id} | {lang}", _oid);
            return await DownloadArticleBinaryById(article.Id, lang);
        }

        public async Task<Result<Stream>> DownloadArticleBinaryById(Guid articleId, Language lang)
        {
            _appInsights.LogInformation($"ArticleService | DownloadArticleBinaryById: {articleId} | {lang}", _oid);
            if (articleId == default || lang == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput);
            }
            return Result.Ok(await _blobRepository.DownloadFileAsync(new BlobId($"{articleId}_{lang}")));
        }

        public async Task<Result<IEnumerable<Article>>> GetAll()
        {
            _appInsights.LogInformation($"ArticleService | GetAll", _oid);
            var entries = await _articleRepository.GetAll();
            return Result.Ok<IEnumerable<Article>>(entries.OrderByDescending(a => a.PublishDate));
        }

        public async Task<Result<IEnumerable<Article>>> GetAvailableArticles()
        {
            _appInsights.LogInformation($"ArticleService | GetAvailableArticles", _oid);
            var settings = await _settingsRepository.Get();
            var entries = await _articleRepository.GetAllPublished();
            var filteredEntries = new List<Article>();

            return Result.Ok<IEnumerable<Article>>(entries.Select(article => article.FilterResultsBySettings(settings)).OrderByDescending(a => a.PublishDate));
        }

        public async Task<Result<Article>> GetById(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleService | GetById: {articleId}", _oid);
            if (articleId == default)
            {
                return Result.Fail<Article>(Failure.InvalidInput, nameof(articleId));

            }
            var article = await _articleRepository.FindById(articleId);
            if (article == null)
            {
                return Result.Fail<Article>(Failure.ArticleNotFound);
            }
            return Result.Ok(article);
        }

        public async Task<Result> PublishUnpublish(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleService | PublishUnpublish: {articleId}", _oid);
            var retrievedArticle = await GetById(articleId);
            if (retrievedArticle.Failed)
            {
                return Result.Fail(retrievedArticle.FailureReason);
            }
            var article = retrievedArticle.Value;
            article.PublishUnpublish();
            await AddOrUpdateAuxiliar(article);
            await UpdateSettingDeploymentInfo(true);
            await LaunchDeployment();
            return Result.Ok();
        }

        public async Task<Result<Article>> GetByTitle(string title, Language language)
        {
            _appInsights.LogInformation($"ArticleService | GetByTitle: {title} | {language}", _oid);
            if (language == default)
            {
                return Result.Fail<Article>(Failure.InvalidInput, nameof(language));
            }
            var articleHelper = await _articleHelperRepository.FindByTitle(title.ToLowerInvariant(), language);
            if (articleHelper == null)
            {
                return Result.Fail<Article>(Failure.ArticleNotFound);
            }
            return await GetById(articleHelper.Reference);
        }

        public async Task<Result<IEnumerable<Article>>> GetLast(int n)
        {
            _appInsights.LogInformation($"ArticleService | GetLast: {n}", _oid);
            if (n > 1000)
            {
                return Result.Fail<IEnumerable<Article>>(Failure.InvalidInput, "You can only retrieve last 1000 entries");
            }
            var results = await GetAll();
            if (results.Failed)
            {
                return Result.Fail<IEnumerable<Article>>(results.FailureReason);
            }
            return Result.Ok<IEnumerable<Article>>(results.Value.OrderByDescending(r => r.Timestamp).Take(n).ToList());
        }

        public async Task<Result> UploadArticleBinary(Article article, Stream binary, Language lang)
        {
            _appInsights.LogInformation($"ArticleService | UploadArticleBinary: {article.Id} | {lang}", _oid);
            if (article.Id == default || lang == default)
            {
                return Result.Fail(Failure.InvalidInput);
            }
            await _blobRepository.UploadBinary(new BlobId($"{article.Id}_{lang}"), binary);
            return Result.Ok();
        }

        public async Task<Result> MarkArticleAsDeleted(Article article)
        {
            _appInsights.LogDebug($"ArticleService | MarkArticleAsDeleted: {article.Id}", _oid);
            return await MarkArticleAsDeletedById(article.Id);
        }

        public async Task<Result> MarkArticleAsDeletedById(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleService | MarkArticleAsDeletedById: {articleId}", _oid);
            var retrievedArticle = await GetById(articleId);
            if (retrievedArticle.Failed)
            {
                return Result.Fail(retrievedArticle.FailureReason);
            }
            var article = retrievedArticle.Value;
            if (article.Published)
            {
                article.PublishUnpublish();
                await UpdateSettingDeploymentInfo(true);
                await LaunchDeployment();
            }
            article.MarkAsDeleted();
            await AddOrUpdateAuxiliar(article);
            return Result.Ok();
        }

        public async Task<Result> RecoverArticleFromDeleted(Article article)
        {
            _appInsights.LogDebug($"ArticleService | RecoverArticleFromDeleted: {article.Id}", _oid);
            return await RecoverArticleFromDeletedById(article.Id);
        }

        public async Task<Result> RecoverArticleFromDeletedById(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleService | RecoverArticleFromDeletedById: {articleId}", _oid);
            var retrievedArticle = await GetById(articleId);
            if (retrievedArticle.Failed)
            {
                return Result.Fail(retrievedArticle.FailureReason);
            }
            var article = retrievedArticle.Value;
            article.UndoMarkAsDeleted();
            await AddOrUpdateAuxiliar(article);
            return Result.Ok();
        }

        public async Task<Result<IEnumerable<Article>>> GetArticlesMarkedAsDeleted()
        {
            _appInsights.LogInformation($"ArticleService | GetArticlesMarkedAsDeleted", _oid);
            var entries = await _articleRepository.GetArticlesMarkedAsDeleted();
            return Result.Ok(entries.OrderByDescending(a => a.MarkedAsDeletedDate).Reverse());
        }

        public async Task<Result<Article>> RestoreBackupArticle(FunctionContext context, Guid articleId, Language lang, DateTime modificationDate)
        {
            _appInsights.LogInformation($"ArticleService | RestoreBackupArticle: {articleId} | {lang} | {modificationDate}", _oid);
            var backupArticle = await _articleBackupService.GetByIdAndDate(articleId, lang, modificationDate);
            if (backupArticle.Failed)
            {
                return Result.Fail<Article>(backupArticle.FailureReason);
            }
            if (backupArticle.Value == null)
            {
                return Result.Fail<Article>(Failure.BackupNotFound, $"{lang} {modificationDate}");
            }
            var contentStream = await _articleBackupService.DownloadArticleBackupBinary(backupArticle.Value);
            if (contentStream.Failed)
            {
                return Result.Fail<Article>(contentStream.FailureReason);
            }
            var content = string.Empty;
            if (contentStream.Value != null)
            {
                StreamReader reader = new StreamReader(contentStream.Value);
                content = reader.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\""))
                {
                    content = content.Substring(1, content.Length - 2);
                }
            }
            var tmpArticle = _dtoArticleMapper.DocumentToDto(backupArticle.Value, lang, content);
            var updatedArticle = await AddOrUpdateArticle(context, tmpArticle, lang, true);
            if (updatedArticle.Failed)
            {
                return Result.Fail<Article>(updatedArticle.FailureReason);
            }
            return Result.Ok(updatedArticle.Value);
        }

        public async Task<Result> LaunchDeployment()
        {
            _appInsights.LogInformation($"ArticleService | LaunchDeployment", _oid);
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Harck-CMS", "1.0"));
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", _configuration["GIT_TOKEN"]);

            var payload = new
            {
                event_type = "backend_automation",
                client_payload = new { branch = _configuration["GIT_BRANCH"] }
            };

            var response = await httpClient.PostAsJsonAsync($"https://api.github.com/repos/{_configuration["DISPATCH_REPO"]}/dispatches", payload);
            if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                return Result.Fail(Failure.DeploymentLaunchFailed);
            }
            _appInsights.LogInformation($"ArticleService | LaunchDeployment: update settings deployment information", _oid);
            await UpdateSettingDeploymentInfo(false);
            return Result.Ok();
        }
    }
}
