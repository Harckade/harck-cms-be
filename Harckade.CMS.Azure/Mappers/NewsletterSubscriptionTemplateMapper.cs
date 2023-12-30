using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;

namespace Harckade.CMS.Azure.Mappers
{
    internal class NewsletterSubscriptionTemplateMapper : INewsletterSubscriptionTemplateMapper
    {
        public NewsletterSubscriptionTemplateEntity DomainToEntity(NewsletterSubscriptionTemplate confirmationEmailTemplate)
        {
            return new NewsletterSubscriptionTemplateEntity()
            {
                PartitionKey = "0",
                RowKey = ("0"),
                Subject = confirmationEmailTemplate.GetTitles(),
                Author = confirmationEmailTemplate.GetAuthor(),
                ETag = ETag.All
            };
        }

        public NewsletterSubscriptionTemplate EntityToDomain(NewsletterSubscriptionTemplateEntity confirmationEmailTemplate)
        {
            return new NewsletterSubscriptionTemplate(confirmationEmailTemplate);
        }
    }
}
