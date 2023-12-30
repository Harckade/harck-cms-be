namespace Harckade.CMS.Azure.Dtos
{
    public class ArticleBackupDto
    {
        public Guid Id { get; set; }
        public DateTime ModificationDate { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ImageDescription { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string ModifiedBy { get; set; }
        public string Author { get; set; }
    }
}
