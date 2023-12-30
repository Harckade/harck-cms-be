using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoNewsletterMapper : IDtoNewsletterMapper
    {
        public NewsletterDto DocumentToDto(Newsletter message)
        {
            return new NewsletterDto()
            {
                Id = message.Id,
                Name = FilterEmptyValues((Dictionary<Enums.Language, string>)message.Name),
                Author = FilterEmptyValues((Dictionary<Enums.Language, string>)message.Author),
                Timestamp = message.Timestamp.ToUniversalTime(),
                SendDate = message.SendDate.ToUniversalTime()
            };
        }

        private Dictionary<Enums.Language, string> FilterEmptyValues(Dictionary<Enums.Language, string> dictionary)
        {
            return dictionary?.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                             .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public Newsletter DtoToDocument(NewsletterDto message)
        {
            return new Newsletter(message);
        }
    }
}
