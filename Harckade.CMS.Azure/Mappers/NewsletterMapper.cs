using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;

namespace Harckade.CMS.Azure.Mappers
{
    public class NewsletterMapper : INewsletterMapper
    {
        public NewsletterEntity DomainToEntity(Newsletter newsletter)
        {
            var generatedId = Math.Abs(Convert.ToInt64(newsletter.Id.ToString("N").Substring(0, 16), 16) % 5);
            return new NewsletterEntity
            {
                PartitionKey = generatedId.ToString(),
                RowKey = newsletter.Id.ToString("N"),
                Author = newsletter.GetAuthor(),
                Name = newsletter.GetTitles(),
                SendDate = newsletter.SendDate != default ? newsletter.SendDate.ToUniversalTime() : new DateTime(1601, 1, 1).ToUniversalTime(),
                Timestamp = newsletter.Timestamp,
                ContentHash = newsletter.ContentHash != null ? newsletter.GetContentHash() : "",
                ETag = ETag.All
            };
        }

        public Newsletter EntityToDomain(NewsletterEntity newsletter)
        {
            return new Newsletter(newsletter);
        }
    }
}
