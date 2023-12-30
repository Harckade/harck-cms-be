using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;

namespace Harckade.CMS.Azure.Mappers
{
    public class JournalMapper : IJournalMapper
    {
        public JournalEntryEntity DomainToEntity(JournalEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }
            return new JournalEntryEntity()
            {
                PartitionKey = entry.PartitionKey.ToString(),
                RowKey = entry.ReversedTicks,
                UserEmail = entry.UserEmail,
                UserId = $"{entry.UserId}",
                ControllerMethod = entry.ControllerMethod,
                PreviousHash = entry.PreviousHash,
                Description = entry.Description,
                Hash = entry.Hash
            };
        }

        public JournalEntry EntityToDomain(JournalEntryEntity entry)
        {
            if (entry == null)
            {
                return null;
            }
            return new JournalEntry(entry);
        }
    }
}
