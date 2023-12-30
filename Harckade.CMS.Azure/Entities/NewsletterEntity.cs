namespace Harckade.CMS.Azure.Entities
{
    public class NewsletterEntity : GenericEntity
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public DateTime SendDate { get; set; }
        public string ContentHash { get; set; }
    }
}
