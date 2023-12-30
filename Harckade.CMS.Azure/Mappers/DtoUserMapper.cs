using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.JwtAuthorization.Authorization;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoUserMapper : IDtoUserMapper
    {
        public UserDto DocumentToDto(User user)
        {
            var role = user.Roles.Contains(UserRoles.Administrator) ? UserRoles.Administrator :
                       user.Roles.Contains(UserRoles.Editor) ? UserRoles.Editor : UserRoles.Viewer;
            return new UserDto()
            {
                Name = user.Name,
                Email = user.Email,
                Role = Enum.GetName(typeof(UserRoles), role),
                Id = user.Id
            };
        }

        public User DtoToDocument(UserDto user)
        {
            return new User(user);
        }
    }
}
