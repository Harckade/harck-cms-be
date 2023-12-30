using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoNewsletterSubscriberMapper : IDtoNewsletterSubscriberMapper
    {
        public NewsletterSubscriberDto DocumentToDto(NewsletterSubscriber subscriber)
        {
            return new NewsletterSubscriberDto()
            {
                EmailAddress = subscriber.EmailAddress,
                Language = Enum.GetName(typeof(Language), subscriber.Language),
                PersonalToken = String.Empty,
                SubscriptionDate = subscriber.SubscriptionDate,
                Confirmed = subscriber.Confirmed,
                Id = subscriber.Id
            };
        }
    }
}
