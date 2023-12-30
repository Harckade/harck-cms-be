namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleMapper
    {
        Entities.ArticleEntity DomainToEntity(Domain.Article article);
        Domain.Article EntityToDomain(Entities.ArticleEntity article);
    }
}
