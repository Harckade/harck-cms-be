using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterSubscriptionTemplateRepository
    {
        Task InsertOrUpdateTemplate(NewsletterSubscriptionTemplate subscriptionTemplate);
        Task<NewsletterSubscriptionTemplate> GetTemplate();
    }
}
