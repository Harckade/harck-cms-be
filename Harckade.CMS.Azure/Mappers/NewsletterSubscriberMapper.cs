using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Mappers
{
    public class NewsletterSubscriberMapper : INewsletterSubscriberMapper
    {
        public NewsletterSubscriberEntity DomainToEntity(NewsletterSubscriber subscriber)
        {
            var generatedId = Math.Abs(Convert.ToInt64(subscriber.Id.ToString("N").Substring(0, 16), 16) % 5);

            return new NewsletterSubscriberEntity()
            {
                PartitionKey = generatedId.ToString(),
                RowKey = subscriber.Id.ToString("N"),
                EmailAddress = subscriber.EmailAddress,
                Language = Enum.GetName(typeof(Language), subscriber.Language),
                PersonalToken = subscriber.PersonalToken,
                SubscriptionDate = subscriber.SubscriptionDate,
                Confirmed = subscriber.Confirmed,
                ETag = ETag.All
            };
        }

        public NewsletterSubscriber EntityToDomain(NewsletterSubscriberEntity subscriber)
        {
            return new NewsletterSubscriber(subscriber);
        }
    }
}
