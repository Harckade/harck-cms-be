namespace Harckade.CMS.Azure.Abstractions
{
    public interface IArticleBackupMapper
    {
        Entities.ArticleBackupEntity DomainToEntity(Domain.ArticleBackup article);
        Domain.ArticleBackup EntityToDomain(Entities.ArticleBackupEntity article);
    }
}
