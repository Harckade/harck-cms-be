namespace Harckade.CMS.Azure.Dtos
{
    public class JournalEntryDto
    {
        public string UserEmail { get; set; }
        public Guid UserId { get; set; }
        public string ControllerMethod { get; set; }
        public string Description { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
