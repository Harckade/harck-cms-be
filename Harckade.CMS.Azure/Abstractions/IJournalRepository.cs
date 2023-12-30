using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IJournalRepository
    {
        Task Insert(JournalEntry entry);
        Task<JournalEntry> GetLastEntry();
        Task<IEnumerable<JournalEntry>> Get(DateTimeOffset startDate = default, DateTimeOffset endDate = default);
    }
}
