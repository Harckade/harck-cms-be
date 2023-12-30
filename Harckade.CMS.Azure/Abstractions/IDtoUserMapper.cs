using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoUserMapper
    {
        UserDto DocumentToDto(User user);
        User DtoToDocument(UserDto user);
    }
}
