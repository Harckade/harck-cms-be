using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Utils;
using Microsoft.Graph;

namespace Harckade.CMS.Azure.Domain
{
    public class User
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public IEnumerable<UserRoles> Roles { get; private set; }
        public Guid Id { get; private set; }

        public User(UserDto user)
        {
            var role = (UserRoles)Enum.Parse(typeof(UserRoles), user.Role, true);
            Name = user.Name;
            Email = user.Email;
            Roles = new List<UserRoles>() { role };
            Id = user.Id;
        }

        public User(Microsoft.Graph.User user, IEnumerable<Microsoft.Graph.AppRole> appRoles)
        {
            var userAppRoles = user.AppRoleAssignments.Select(r => appRoles.Where(ar => ar.Id == r.AppRoleId).FirstOrDefault()).Where(r => r != null);
            var parsedRoles = userAppRoles.Select(r => (UserRoles)Enum.Parse(typeof(UserRoles), r.Value, true)).ToList();
            var email = MicrosoftGraph.PrincipalNameToEmail(user.UserPrincipalName);

            Name = user.DisplayName;
            Email = email;
            Roles = parsedRoles;
            Id = Guid.Parse(user.Id);
        }

        public User(IEnumerable<System.Security.Claims.Claim> claims)
        {
            var emailClaim = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            Email = emailClaim != null ? emailClaim.Value: claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn").Value;
            Name = claims.FirstOrDefault(c => c.Type == "name").Value;
            Id = Guid.Parse(claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        }

        public void Update(User originalUser, Invitation user)
        {
            var name = string.IsNullOrWhiteSpace(user.InvitedUser.DisplayName) ? originalUser.Email.Split('@')[0] : user.InvitedUser.DisplayName;
            var email = originalUser.Email;
            var roles = new List<UserRoles>() { originalUser.Roles.FirstOrDefault() };
            Update(name, email, roles);
            Id = Guid.Parse(user.InvitedUser.Id);
        }

        public void Update(User originalUser, Microsoft.Graph.User userAd)
        {
            Update(userAd.DisplayName, MicrosoftGraph.PrincipalNameToEmail(userAd.UserPrincipalName), new List<UserRoles>() { originalUser.Roles.FirstOrDefault() });
        }

        private void Update(string name, string email, IEnumerable<UserRoles> roles)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentNullException(nameof(email));
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (!roles.Any())
            {
                throw new ArgumentNullException(nameof(roles));
            }
            if (!Validations.IsValidEmail(email))
            {
                throw new ArgumentException("email is not valid");
            }
            Name = name;
            Email = email;
            Roles = roles;
        }
    }
}
