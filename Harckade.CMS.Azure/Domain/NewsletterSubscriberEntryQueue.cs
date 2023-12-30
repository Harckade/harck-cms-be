using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class NewsletterSubscriberEntryQueue
    {
        [JsonProperty]
        public string EmailAddress { get; set; }
        [JsonProperty]
        public Language Language { get; set; }

        public NewsletterSubscriberEntryQueue(string emailAddress, Language language)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                throw new ArgumentNullException(nameof(emailAddress));
            }
            else if (!Utils.Validations.IsValidEmail(emailAddress))
            {
                throw new ArgumentException(nameof(emailAddress));
            }
            if (language == default)
            {
                throw new ArgumentNullException(nameof(language));
            }
            EmailAddress = emailAddress;
            Language = language;
        }
    }
}
