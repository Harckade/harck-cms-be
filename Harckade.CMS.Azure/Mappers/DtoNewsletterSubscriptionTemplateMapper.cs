using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoNewsletterSubscriptionTemplateMapper : IDtoNewsletterSubscriptionTemplateMapper
    {
        public NewsletterSubscriptionTemplateDto DocumentToDto(NewsletterSubscriptionTemplate subscriptionTemplate)
        {
            return new NewsletterSubscriptionTemplateDto()
            {
                Subject = FilterEmptyValues((Dictionary<Enums.Language, string>)subscriptionTemplate.Subject),
                Author = FilterEmptyValues((Dictionary<Enums.Language, string>)subscriptionTemplate.Author),
                Timestamp = subscriptionTemplate.Timestamp.ToUniversalTime(),
            };
        }

        private Dictionary<Enums.Language, string> FilterEmptyValues(Dictionary<Enums.Language, string> dictionary)
        {
            return dictionary?.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                             .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public NewsletterSubscriptionTemplate DtoToDocument(NewsletterSubscriptionTemplateDto subscriptionTemplate)
        {
            return new NewsletterSubscriptionTemplate(subscriptionTemplate);
        }
    }
}
