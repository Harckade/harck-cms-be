using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoNewsletterSubscriberMapper
    {
        NewsletterSubscriberDto DocumentToDto(NewsletterSubscriber subscriber);
    }
}
