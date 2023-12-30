using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoArticleMapper : IDtoArticleMapper
    {
        public ArticleDto DocumentToDto(Article article)
        {
            return new ArticleDto()
            {
                Description = FilterEmptyValues((Dictionary<Enums.Language, string>)article.Description),
                Id = article.Id,
                Name = FilterEmptyValues((Dictionary<Enums.Language, string>)article.Name),
                NameNoDiacritics = FilterEmptyValues((Dictionary<Enums.Language, string>)article.NameNoDiacritics),
                Author = FilterEmptyValues((Dictionary<Enums.Language, string>)article.Author),
                Timestamp = article.Timestamp.ToUniversalTime(),
                ImageUrl = FilterEmptyValues((Dictionary<Enums.Language, string>)article.ImageUrl),
                ImageDescription = FilterEmptyValues((Dictionary<Enums.Language, string>)article.ImageDescription),
                PublishDate = article.PublishDate,
                Published = article.Published,
                Tags = FilterEmptyValues((Dictionary<Enums.Language, IEnumerable<string>>)article.Tags),
                MarkedAsDeleted = article.MarkedAsDeleted,
                MarkedAsDeletedDate = article.MarkedAsDeleted == true ? article.MarkedAsDeletedDate : default,
                HtmlContentIsLoaded = false
            };
        }

        private Dictionary<Enums.Language, string> FilterEmptyValues(Dictionary<Enums.Language, string> dictionary)
        {
            return dictionary?.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                             .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private Dictionary<Enums.Language, IEnumerable<string>> FilterEmptyValues(Dictionary<Enums.Language, IEnumerable<string>> dictionary)
        {
            return dictionary?.Where(kv => kv.Value != null && kv.Value.Any())
                             .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public ArticleDto DocumentToDto(ArticleBackup backupArticle, Enums.Language lang, string content)
        {
            if (lang == default)
            {
                throw new System.ArgumentException(nameof(lang));
            }
            return new ArticleDto()
            {
                Author = new Dictionary<Enums.Language, string>() { { lang, backupArticle.Author } },
                Name = new Dictionary<Enums.Language, string>() { { lang, backupArticle.Name } },
                Description = new Dictionary<Enums.Language, string>() { { lang, backupArticle.Description } },
                Id = backupArticle.Id,
                ImageUrl = new Dictionary<Enums.Language, string>() { { lang, backupArticle.ImageUrl } },
                ImageDescription = new Dictionary<Enums.Language, string>() { { lang, backupArticle.ImageDescription } },
                Tags = new Dictionary<Enums.Language, IEnumerable<string>>() { { lang, backupArticle.Tags } },
                HtmlContent = new Dictionary<Enums.Language, string>() { { lang, content } },
                HtmlContentIsLoaded = true
            };
        }

        public Article DtoToDocument(ArticleDto article)
        {
            return new Article(article);
        }
    }
}
