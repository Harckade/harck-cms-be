using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoNewsletterMapper
    {
        NewsletterDto DocumentToDto(Newsletter message);
        Newsletter DtoToDocument(NewsletterDto message);
    }
}
