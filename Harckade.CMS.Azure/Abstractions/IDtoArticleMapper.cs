using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoArticleMapper
    {
        ArticleDto DocumentToDto(Article article);
        ArticleDto DocumentToDto(ArticleBackup backupArticle, Enums.Language lang, string content);
        Article DtoToDocument(ArticleDto article);
    }
}
