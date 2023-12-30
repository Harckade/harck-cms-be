using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoJournalMapper: IDtoJournalMapper
    {
        public JournalEntryDto DocumentToDto(JournalEntry entry)
        {
            return new JournalEntryDto()
            {
                ControllerMethod = entry.ControllerMethod,
                Description = entry.Description,
                UserEmail = entry.UserEmail,
                UserId = entry.UserId,
                TimeStamp = new DateTime(DateTime.MaxValue.Ticks - Int64.Parse(entry.ReversedTicks))
            };
        }
    }
}
