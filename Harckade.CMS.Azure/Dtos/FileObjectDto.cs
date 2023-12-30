namespace Harckade.CMS.Azure.Dtos
{
    public class FileObjectDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public DateTime? Timestamp { get; set; }
        public long? Size { get; set; }
    }
}
