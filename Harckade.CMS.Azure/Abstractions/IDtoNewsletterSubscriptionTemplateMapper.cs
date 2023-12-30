using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoNewsletterSubscriptionTemplateMapper
    {
        NewsletterSubscriptionTemplateDto DocumentToDto(NewsletterSubscriptionTemplate subscriptionTemplate);
        NewsletterSubscriptionTemplate DtoToDocument(NewsletterSubscriptionTemplateDto subscriptionTemplate);
    }
}
