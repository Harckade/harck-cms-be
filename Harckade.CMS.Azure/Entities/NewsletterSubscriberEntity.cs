namespace Harckade.CMS.Azure.Entities
{
    public class NewsletterSubscriberEntity : GenericEntity
    {
        public string EmailAddress { get; set; }
        public string Language { get; set; }
        public string PersonalToken { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public bool Confirmed { get; set; }
    }
}
