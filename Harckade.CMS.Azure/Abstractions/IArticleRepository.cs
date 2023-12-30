using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleRepository
    {
        Task InsertOrUpdate(Article article);
        Task<Article> FindById(Guid articleId);
        Task<IEnumerable<Article>> GetAll();
        Task<IEnumerable<Article>> GetAllPublished();
        Task<IEnumerable<Article>> GetArticlesMarkedAsDeleted();
        Task Delete(Guid articleId);
    }
}
