namespace Harckade.CMS.Azure.Abstractions
{
    public interface IJournalMapper
    {
        Entities.JournalEntryEntity DomainToEntity(Domain.JournalEntry entry);
        Domain.JournalEntry EntityToDomain(Entities.JournalEntryEntity entry);
    }
}
