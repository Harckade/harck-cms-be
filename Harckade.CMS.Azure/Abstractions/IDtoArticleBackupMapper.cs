using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoArticleBackupMapper
    {
        ArticleBackupDto DocumentToDto(ArticleBackup article);
        ArticleBackup DtoToDocument(ArticleBackupDto article);
    }
}
