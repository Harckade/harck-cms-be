namespace Harckade.CMS.Azure.Entities
{
    public class ArticleBackupEntity : GenericEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ImageDescription { get; set; }
        public string Tags { get; set; }
        public string ContentId { get; set; }
        public string Author { get; set; }
        public string ModifiedBy { get; set; }
    }
}
