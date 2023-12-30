using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoArticleBackupMapper : IDtoArticleBackupMapper
    {
        public ArticleBackupDto DocumentToDto(ArticleBackup article)
        {
            return new ArticleBackupDto()
            {
                Description = article.Description,
                Id = article.Id,
                Name = article.Name,
                ModificationDate = article.ModificationDate.ToUniversalTime(),
                Timestamp = article.Timestamp.ToUniversalTime(),
                ImageUrl = article.ImageUrl,
                ImageDescription = article.ImageDescription,
                Tags = article.Tags,
                Author = article.Author,
                ModifiedBy = article.ModifiedBy,
                Language = Enum.GetName(typeof(Language), article.Language)
            };
        }

        public ArticleBackup DtoToDocument(ArticleBackupDto article)
        {
            return new ArticleBackup(article);
        }
    }
}
