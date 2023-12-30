using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Utils;

namespace Harckade.CMS.Azure.Domain
{
    public class JournalEntry
    {
        public int PartitionKey { get; private set; }
        public string ReversedTicks { get; private set; }
        public string PreviousHash { get; private set; }
        public string Hash { get; private set; }
        public string UserEmail { get; private set; }
        public Guid UserId { get; private set; }
        public string ControllerMethod { get; private set; }
        public string Description { get; private set; }

        public JournalEntry(JournalEntryEntity entry)
        {
            UserEmail = entry.UserEmail;
            UserId = Guid.Parse(entry.UserId);
            ControllerMethod = entry.ControllerMethod;
            Description = entry.Description;
            Hash = entry.Hash;
            PreviousHash = entry.PreviousHash;
            PartitionKey = Int32.Parse(entry.PartitionKey);
            ReversedTicks = entry.RowKey;
        }

        public JournalEntry(string previousHash, JournalEntryQueue queueEntry, DateTimeOffset? insertedOn)
        {
            PartitionKey = insertedOn.Value.UtcDateTime.Month > 6 ? insertedOn.Value.UtcDateTime.Month - 1 - 6 : insertedOn.Value.UtcDateTime.Month - 1;
            ReversedTicks = string.Format("{0:D19}", DateTimeOffset.MaxValue.Ticks - insertedOn.Value.UtcDateTime.Ticks);
            if (string.IsNullOrEmpty(previousHash))
            {
                throw new ArgumentNullException(nameof(previousHash));
            }
            if (string.IsNullOrEmpty(queueEntry.UserEmail))
            {
                throw new ArgumentNullException(nameof(queueEntry.UserEmail));
            }
            if (queueEntry.UserId == default)
            {
                throw new ArgumentNullException(nameof(queueEntry.UserId));
            }
            if (string.IsNullOrEmpty(queueEntry.ControllerMethod))
            {
                throw new ArgumentNullException(nameof(queueEntry.ControllerMethod));
            }
            UserEmail = queueEntry.UserEmail;
            UserId = queueEntry.UserId;
            ControllerMethod = queueEntry.ControllerMethod;
            Description = queueEntry.Description;
            PreviousHash = previousHash;
            Hash = Harckade.CMS.Utils.Hash.Sha512($"{PartitionKey}{ReversedTicks}{PreviousHash}{UserEmail}{UserId}{ControllerMethod}{Description}");
        }
    }
}
