using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.Services
{
    public class ArticleBackupService : ServiceBase, IArticleBackupService
    {
        private IArticleBackupRepository _articleBackupRepository;
        private IBlobRepository _blobRepository;
        private readonly ILogger<ArticleBackupService> _appInsights;

        public ArticleBackupService(IArticleBackupRepository articleBackupRepository, IBlobRepository blobRepository, ILogger<ArticleBackupService> appInsights)
        {
            _articleBackupRepository = articleBackupRepository;
            _blobRepository = blobRepository;
            _appInsights = appInsights;
            _oidIsSet = false;
        }

        public async Task<Result> AddArticleBackup(FunctionContext context, ArticleBackup articleBackup)
        {
            _appInsights.LogInformation($"ArticleBackupService | AddArticleBackup: {articleBackup.Id}", _oid);
            if (articleBackup.Id == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(articleBackup.Id));
            }
            await _articleBackupRepository.Insert(articleBackup);
            return Result.Ok();
        }

        public async Task<Result> DeleteArticleBackupById(Guid articleId)
        {
            _appInsights.LogInformation($"ArticleBackupService | DeleteArticletById: {articleId}", _oid);
            if (articleId == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(articleId));
            }
            await _articleBackupRepository.Delete(articleId);
            return Result.Ok();
        }

        public async Task<Result<Stream>> DownloadArticleBackupBinary(ArticleBackup articleBackup)
        {
            _appInsights.LogInformation($"ArticleBackupService | DownloadArticleBackupBinary: {articleBackup.Id}", _oid);
            return await DownloadArticleBackupBinaryByIdAndDate(articleBackup.Id, articleBackup.Language, articleBackup.ModificationDate);
        }

        public async Task<Result<Stream>> DownloadArticleBackupBinaryByIdAndDate(Guid articleId, Language lang, DateTime modificationDate)
        {
            _appInsights.LogInformation($"ArticleBackupService | DownloadArticleBackupBinaryByIdAndDate: {articleId} | {lang} | {modificationDate}", _oid);
            if (articleId == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput, nameof(articleId));
            }
            if (lang == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput, nameof(lang));
            }
            if (modificationDate == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput, nameof(modificationDate));
            }
            var blob = (await _blobRepository.DownloadFileAsync(new BlobId($"{articleId}_{lang}_{modificationDate.ToString("MM/dd/yyyy HH:mm:ss")}")));
            return Result.Ok(blob);
        }

        public async Task<Result<IEnumerable<ArticleBackup>>> GetById(Guid articleId, Language lang)
        {
            _appInsights.LogInformation($"ArticleBackupService | GetById: {articleId} | {lang}", _oid);
            if (articleId == default)
            {
                return Result.Fail<IEnumerable<ArticleBackup>>(Failure.InvalidInput, nameof(articleId));
            }
            if (lang == default)
            {
                return Result.Fail<IEnumerable<ArticleBackup>>(Failure.InvalidInput, nameof(lang));
            }
            var articles = await _articleBackupRepository.FindById(articleId, lang);
            return Result.Ok(articles);
        }

        public async Task<Result<ArticleBackup>> GetByIdAndDate(Guid articleId, Language lang, DateTime modificationDate)
        {
            _appInsights.LogInformation($"ArticleBackupService | GetByIdAndDate: {articleId} | {lang} | modificationDate", _oid);
            var result = await GetById(articleId, lang);
            if (result.Failed)
            {
                return Result.Fail<ArticleBackup>(result.FailureReason);
            }
            var articles = result.Value;
            if (articles.Any())
            {
                return Result.Ok(articles.FirstOrDefault(art => art.ModificationDate == modificationDate));
            }
            return Result.Ok<ArticleBackup>(null);
        }

        public async Task<Result> UploadArticleBackupBinary(ArticleBackup articleBackup, Stream binary, Language lang)
        {
            _appInsights.LogInformation($"ArticleBackupService | UploadArticleBackupBinary: {articleBackup.Id} | {lang}", _oid);
            if (articleBackup.Id == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(articleBackup.Id));
            }
            if (articleBackup.Language == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(articleBackup.Language));
            }
            if (articleBackup.ModificationDate == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(articleBackup.ModificationDate));
            }
            await _blobRepository.UploadBinary(new BlobId($"{articleBackup.Id}_{lang}_{articleBackup.ModificationDate.ToString("MM/dd/yyyy HH:mm:ss")}"), binary);
            return Result.Ok();
        }
    }
}
