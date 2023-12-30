namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleHelperMapper
    {
        Entities.ArticleHelperEntity DomainToEntity(Domain.ArticleHelper article);
        Domain.ArticleHelper EntityToDomain(Entities.ArticleHelperEntity article);
    }
}
