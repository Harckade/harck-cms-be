using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleHelperRepository
    {
        Task InsertOrUpdate(ArticleHelper article);
        Task<ArticleHelper> FindByTitle(string title, Language language);
        Task<ArticleHelper> FindByEncodedTitle(string encodedTitle);
        Task DeleteByEncodedTitle(string encodedTitle);
        Task DeleteByTitle(string title, Language language);
        Task<IEnumerable<ArticleHelper>> FindByArticle(Article article);
        Task<IEnumerable<ArticleHelper>> FetchAll();
    }
}
