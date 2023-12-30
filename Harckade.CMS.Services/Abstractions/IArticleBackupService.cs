using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IArticleBackupService : IServiceBase
    {
        /// <summary>
        /// List all article's backups for a specific language
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <param name="lang">Article language</param>
        /// <returns>A collection of articles backups</returns>
        Task<Result<IEnumerable<ArticleBackup>>> GetById(Guid articleId, Language lang);
        /// <summary>
        /// Retrieve an article backup for a specific language and date(no content)
        /// </summary>
        /// <param name="articleId"></param>
        /// <param name="lang"></param>
        /// <param name="modificationDate"></param>
        /// <returns>Article backup</returns>
        Task<Result<ArticleBackup>> GetByIdAndDate(Guid articleId, Language lang, DateTime modificationDate);
        /// <summary>
        /// Delete an article backup. All backups related to a specific ID will be removed
        /// </summary>
        /// <param name="articleId">Article identiefier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteArticleBackupById(Guid articleId);
        /// <summary>
        /// The article backup will be uploaded to the repository so it can be used later to return an article to a specific state.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="articleBackup"></param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> AddArticleBackup(FunctionContext context, ArticleBackup articleBackup);
        /// <summary>
        /// Upload article's backup content for a specific language to the repository
        /// </summary>
        /// <param name="articleBackup"></param>
        /// <param name="binary">Content stream</param>
        /// <param name="lang">Content and article language</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> UploadArticleBackupBinary(ArticleBackup articleBackup, Stream binary, Language lang);
        /// <summary>
        /// Download content of an article backup
        /// </summary>
        /// <param name="articleBackup"></param>
        /// <returns>Article's content as Stream</returns>
        Task<Result<Stream>> DownloadArticleBackupBinary(ArticleBackup articleBackup);
        /// <summary>
        /// Download content of an article backup for a specific date and language
        /// </summary>
        /// <param name="articleId"></param>
        /// <param name="lang">article language</param>
        /// <param name="modificationDate">backup date</param>
        /// <returns>Article's content as Stream</returns>
        Task<Result<Stream>> DownloadArticleBackupBinaryByIdAndDate(Guid articleId, Language lang, DateTime modificationDate);
    }
}
