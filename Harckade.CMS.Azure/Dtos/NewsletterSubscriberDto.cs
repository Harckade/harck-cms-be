using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Dtos
{
    public class NewsletterSubscriberDto
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public string Language { get; set; }
        public string PersonalToken { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public bool Confirmed { get; set; }
    }
}
