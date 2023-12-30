using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IArticleService : IServiceBase
    {
        /// <summary>
        /// Retrieve an article by identifier
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Article</returns>
        Task<Result<Article>> GetById(Guid articleId);
        /// <summary>
        /// Publish or unpublish an article.
        /// If article is published it will be unpublished.
        /// If article is unpublished it will be published.
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> PublishUnpublish(Guid articleId);
        /// <summary>
        /// List all existing articles. 
        /// That includes articles that are not published or are marked as deleted.
        /// </summary>
        /// <returns>A collection of articles</returns>
        Task<Result<IEnumerable<Article>>> GetAll();
        /// <summary>
        /// Add a new article or update an existing one.
        /// To upload the content of the article use UploadArticleBinary method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tmpArticle">Article received from client</param>
        /// <param name="lang">article language</param>
        /// <param name="skipBackup">When set to true, no backup is created</param>
        /// <returns>Article</returns>
        Task<Result<Article>> AddOrUpdateArticle(FunctionContext context, ArticleDto tmpArticle, Language lang = default, bool skipBackup = false);
        /// <summary>
        /// Target article will be removed, including backups.
        /// </summary>
        /// <param name="article"></param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteArticle(Article article);
        /// <summary>
        /// Target article will be removed, including backups.
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> DeleteArticleById(Guid articleId);
        /// <summary>
        /// Article will be marked as deleted.
        /// Article, its backups and contents will remain intact.
        /// </summary>
        /// <param name="article">Article identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> MarkArticleAsDeleted(Article article);
        /// <summary>
        /// Article will be marked as deleted.
        /// Article, its backups and contents will remain intact.
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> MarkArticleAsDeletedById(Guid articleId);
        /// <summary>
        /// If an article is marked as deleted, its state will be changed and it will be no longer considered deleted.
        /// </summary>
        /// <param name="article">Target article</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> RecoverArticleFromDeleted(Article article);
        /// <summary>
        /// If an article is marked as deleted, its state will be changed and it will be no longer considered deleted.
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> RecoverArticleFromDeletedById(Guid articleId);
        /// <summary>
        /// List all articles that are marked as deleted.
        /// </summary>
        /// <returns>A collection of articles</returns>
        Task<Result<IEnumerable<Article>>> GetArticlesMarkedAsDeleted();
        /// <summary>
        /// Retrieve 1-1000 last articles.
        /// </summary>
        /// <param name="n"></param>
        /// <returns>A collection of articles</returns>
        Task<Result<IEnumerable<Article>>> GetLast(int n);
        /// <summary>
        /// Upload article's content
        /// </summary>
        /// <param name="article">Target article</param>
        /// <param name="binary">Article's content as stream</param>
        /// <param name="lang">Article language</param>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> UploadArticleBinary(Article doc, Stream binary, Language lang);
        /// <summary>
        /// Retrieve article's content for a specific language.
        /// </summary>
        /// <param name="article">Target article object</param>
        /// <param name="lang">Article language</param>
        /// <returns>Content stream</returns>
        Task<Result<Stream>> DownloadArticleBinary(Article article, Language lang);
        /// <summary>
        /// Retrieve article's content for a specific language.
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <param name="lang">Article language</param>
        /// <returns>Content stream</returns>
        Task<Result<Stream>> DownloadArticleBinaryById(Guid articleId, Language lang);
        /// <summary>
        /// Retrieve an article by providing its title in a specific language
        /// </summary>
        /// <param name="title">Article title</param>
        /// <param name="language">Article language</param>
        /// <returns>Article</returns>
        Task<Result<Article>> GetByTitle(string title, Language lang);
        /// <summary>
        /// List only articles that are marked as published.
        /// </summary>
        /// <returns>A collection of articles</returns>
        Task<Result<IEnumerable<Article>>> GetAvailableArticles();
        /// <summary>
        /// Restore an article to a previous state.
        /// Everythig will be restored to the specified point of time, including content.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="articleId">Article identifier</param>
        /// <param name="lang">Article language</param>
        /// <param name="modificationDate">Backup restoration state date</param>
        /// <returns>Restored article</returns>
        Task<Result<Article>> RestoreBackupArticle(FunctionContext context, Guid articleId, Language lang, DateTime modificationDate);
        /// <summary>
        /// Launch blog's deployment.
        /// </summary>
        /// <returns>Result.Ok if succeeded. Result.Fail if something goes wrong</returns>
        Task<Result> LaunchDeployment();
    }
}
