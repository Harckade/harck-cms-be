using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class NewsletterEntryQueue
    {
        [JsonProperty]
        public Guid NewsletterId { get; set; }
        [JsonProperty]
        public Language Language { get; set; }
        [JsonProperty]
        public string EmailTo { get; set; }


        public NewsletterEntryQueue(Guid newsletterId, Language language, string emailTo)
        {
            if (string.IsNullOrEmpty(emailTo))
            {
                throw new ArgumentNullException(nameof(emailTo));
            }
            if (newsletterId == default)
            {
                throw new ArgumentNullException(nameof(newsletterId));
            }
            if (language == default)
            {
                throw new ArgumentNullException(nameof(language));
            }
            NewsletterId = newsletterId;
            Language = language;
            EmailTo = emailTo;
        }
    }
}
