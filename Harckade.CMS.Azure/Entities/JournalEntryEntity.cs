namespace Harckade.CMS.Azure.Entities
{
    public class JournalEntryEntity : GenericEntity
    {
        public string PreviousHash { get; set; }
        public string Hash { get; set; }
        public string UserEmail { get; set; }
        public string UserId { get; set; }
        public string ControllerMethod { get; set; }
        public string Description { get; set; }
    }
}
