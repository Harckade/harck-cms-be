using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Mappers
{
    public class ArticleBackupMapper: IArticleBackupMapper
    {
        public ArticleBackupEntity DomainToEntity(ArticleBackup article)
        {
            var generatedId = Math.Abs(Convert.ToInt64(article.Id.ToString("N").Substring(0, 16), 16) % 5);
            var modificationDate = $"{article.ModificationDate.ToString("MM|dd|yyyy HH:mm:ss")}";
            return new ArticleBackupEntity()
            {
                RowKey = $"{article.Id.ToString("N")}_{article.Language}_{modificationDate}",
                PartitionKey = generatedId.ToString(),
                ETag = ETag.All,
                Name = article.Name,
                Author = article.Author,
                ModifiedBy = article.ModifiedBy,
                Description = article.Description,
                ImageUrl = article.ImageUrl,
                ImageDescription = article.ImageDescription,
                Tags = article.Tags != null && article.Tags.Any() ? JsonConvert.SerializeObject(article.Tags) : "",
                ContentId = article.ContentId != null ? article.ContentId : ""
            };
        }

        public ArticleBackup EntityToDomain(ArticleBackupEntity article)
        {
            return new ArticleBackup(article);
        }
    }
}
