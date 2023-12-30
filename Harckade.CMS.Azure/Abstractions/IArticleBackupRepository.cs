using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleBackupRepository
    {
        Task Insert(ArticleBackup article);
        Task<IEnumerable<ArticleBackup>> FindById(Guid articleId, Language language);
        Task<IEnumerable<ArticleBackup>> FindAllById(Guid articleId);
        Task Delete(Guid articleId);
    }
}
