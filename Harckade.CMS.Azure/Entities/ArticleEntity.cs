namespace Harckade.CMS.Azure.Entities
{
    public class ArticleEntity : GenericEntity
    {
        public string Name { get; set; }
        public string NameNoDiacritics { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ImageDescription { get; set; }
        public string ContentHash { get; set; }
        public DateTime PublishDate { get; set; }
        public bool Published { get; set; }
        public string Tags { get; set; }
        public bool MarkedAsDeleted { get; set; }
        public DateTime MarkedAsDeletedDate { get; set; }
    }
}
