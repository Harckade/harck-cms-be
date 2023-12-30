using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Dtos
{
    public class NewsletterDto
    {
        public Guid Id { get; set; }
        public Dictionary<Language, string> Name { get; set; }
        public Dictionary<Language, string> Author { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime SendDate { get; set; }
        public Dictionary<Language, string> HtmlContent { get; set; }
    }
}
