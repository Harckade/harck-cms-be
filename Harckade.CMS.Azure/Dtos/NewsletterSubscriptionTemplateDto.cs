using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Dtos
{
    public class NewsletterSubscriptionTemplateDto
    {
        public Dictionary<Language, string> Subject { get; set; }
        public Dictionary<Language, string> Author { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<Language, string> HtmlContent { get; set; }
    }
}
