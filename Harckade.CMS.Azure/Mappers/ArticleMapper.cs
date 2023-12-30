using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Mappers
{
    public class ArticleMapper : IArticleMapper
    {
        public ArticleEntity DomainToEntity(Article article)
        {
            var generatedId = Math.Abs(Convert.ToInt64(article.Id.ToString("N").Substring(0, 16), 16) % 5);

            return new ArticleEntity()
            {
                RowKey = article.Id.ToString("N"),
                PartitionKey = generatedId.ToString(),
                ETag = ETag.All,
                Name = article.Name != null ? article.GetTitles() : "",
                ContentHash = article.ContentHash != null ? article.GetContentHash() : "",
                NameNoDiacritics = article.NameNoDiacritics != null ? article.GetTitlesNoDiacritics() : "",
                Author = article.Name != null ? article.GetAuthor() : "",
                Description = article.Description != null ? article.GetDescriptions() : "",
                ImageUrl = article.ImageUrl != null ? article.GetImageUrls() : "",
                ImageDescription = article.ImageDescription != null ? article.GetImageDescriptions() : "",
                Published = article.Published,
                PublishDate = article.PublishDate != default ? article.PublishDate.ToUniversalTime() : new DateTime(1601, 1, 1).ToUniversalTime(),
                Tags = article.Tags != null ? article.GetTags() : "",
                MarkedAsDeleted = article.MarkedAsDeleted != default ? article.MarkedAsDeleted : false,
                MarkedAsDeletedDate = article.MarkedAsDeletedDate != default ? article.MarkedAsDeletedDate.ToUniversalTime() : new DateTime(1601, 1, 1).ToUniversalTime()
            };
        }

        public Article EntityToDomain(ArticleEntity article)
        {
            return new Article(article);
        }
    }
}
