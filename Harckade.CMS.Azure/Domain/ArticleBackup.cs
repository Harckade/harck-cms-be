using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class ArticleBackup
    {
        public Guid Id { get; private set; }
        public DateTime ModificationDate { get; private set; }
        public string Name { get; private set; }
        public string Author { get; private set; }
        public string ModifiedBy { get; private set; }
        public Language Language { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string Description { get; private set; }
        public string ImageUrl { get; private set; }
        public string ImageDescription { get; private set; }
        public IEnumerable<string> Tags { get; private set; }
        public string ContentId { get; private set; }

        private void setArticleValues(Guid id, DateTime modificationDate, string name, Language lang, string description, string imageUrl, string imageDescription, IEnumerable<string> tags, string author, string modifiedBy, DateTime timestamp = default)
        {
            Id = id;
            ModificationDate = modificationDate;
            Name = name;
            Language = lang;
            Description = description;
            ImageUrl = imageUrl;
            ImageDescription = imageDescription;
            Tags = tags;
            ModifiedBy = modifiedBy;
            Author = author;
            if (timestamp != default)
            {
                Timestamp = timestamp;
            }
        }

        public ArticleBackup(Guid id, DateTime modificationDate, string name, Language lang, string description, string imageUrl, string imageDescription, IEnumerable<string> tags, string author, User modifiedBy)
        {
            setArticleValues(id, modificationDate, name, lang, description, imageUrl, imageDescription, tags, author, $"{modifiedBy.Name} | {modifiedBy.Email}");
        }

        public ArticleBackup(ArticleBackupEntity articleEntity)
        {
            if (articleEntity == null)
            {
                throw new ArgumentNullException(nameof(articleEntity));
            }
            var rowKeyElements = articleEntity.RowKey.Split("_");
            if (rowKeyElements == null || rowKeyElements.Length != 3)
            {
                throw new ArgumentException($"{nameof(ArticleBackupEntity)}, wrong RowKey");
            }
            Guid rowKey = Guid.ParseExact(rowKeyElements[0], "N");
            if (rowKey == default)
            {
                throw new ArgumentException(nameof(rowKey));
            }

            Language language = (Language)Enum.Parse(typeof(Language), rowKeyElements[1], true);
            if (language == default)
            {
                throw new ArgumentException(nameof(language));
            }
            DateTime modificationDate = DateTime.Parse(rowKeyElements[2].Replace("|", "/"));
            if (modificationDate == default)
            {
                throw new ArgumentException(nameof(modificationDate));
            }
            var tags = string.IsNullOrWhiteSpace(articleEntity.Tags) ? new List<string>() : (IEnumerable<string>)JsonConvert.DeserializeObject<IEnumerable<string>>(articleEntity.Tags);
            setArticleValues(rowKey, modificationDate, articleEntity.Name, language, articleEntity.Description, articleEntity.ImageUrl, articleEntity.ImageDescription, tags, articleEntity.Author, articleEntity.ModifiedBy, articleEntity.Timestamp.Value.UtcDateTime);
            ContentId = articleEntity.ContentId;
        }

        public ArticleBackup(ArticleBackupDto articleBackupDto)
        {
            if (articleBackupDto == null)
            {
                throw new ArgumentNullException(nameof(articleBackupDto));
            }
            Language language;
            Enum.TryParse<Language>(articleBackupDto.Language, true, out language);
            if (language == default)
            {
                throw new ArgumentException($"Failed parsing of ${nameof(articleBackupDto.Language)}");
            }
            var tags = articleBackupDto.Tags;
            setArticleValues(articleBackupDto.Id, articleBackupDto.ModificationDate.ToUniversalTime(), articleBackupDto.Name, language, articleBackupDto.Description, articleBackupDto.ImageUrl, articleBackupDto.ImageDescription, tags, articleBackupDto.Author, articleBackupDto.ModifiedBy, articleBackupDto.Timestamp.ToUniversalTime());
        }

        public void UpdateContentId()
        {
            ContentId = $"{Id.ToString("N")}_{Language}_{ModificationDate.ToString().Replace("/", "|")}";
        }

        public void UpdateContentIdUsingBackups(IEnumerable<ArticleBackup> backups)
        {
            if (!backups.Any())
            {
                throw new ArgumentNullException(nameof(backups));
            }
            var article = backups.OrderByDescending(previousBackup => previousBackup.ModificationDate).Where(art => art.ContentId != null && art.ContentId == $"{art.Id.ToString("N")}_{art.Language}_{art.ModificationDate.ToString().Replace("/", "|")}").FirstOrDefault();
            if (article != null)
            {
                ContentId = article.ContentId;
            }
        }
    }
}
