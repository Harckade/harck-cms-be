using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.JwtAuthorization.Middleware;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services
{
    public abstract class ServiceBase : IServiceBase
    {
        protected ObservabilityId _oid;
        protected bool _oidIsSet;

        /// <summary>
        /// Check if currently the service is runnign UnitTests in a test environment.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>True if it's unit test environment</returns>
        private bool isRunningTests(FunctionContext context)
        {
            return context.Features == null && context.ToString().StartsWith("Mock<FunctionContext");
        }

        /// <summary>
        /// Retrieve user who is performming the action.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected User GetUser(FunctionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (isRunningTests(context))
            {
                var userDto = new UserDto()
                {
                    Role = "administrator",
                    Name = "UnitTest",
                    Email = "unitest@harckade.com",
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001")
                };

                return new User(userDto);
            }
            var principalFeature = context.Features.Get<JwtPrincipalFeature>();
            var claims = principalFeature.Principal.Claims;
            var user = new User(claims);
            return user;
        }

        /// <summary>
        /// Check wheter observability ID was properly set.
        /// This ID is used to provide traceability and observability to the system through the logs.
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        protected void checkIfOidIsSet()
        {
            if (!_oidIsSet)
            {
                throw new ApplicationException("ObservabilityId has a default value.");
            }
        }

        public void UpdateOid(ObservabilityId oid)
        {
            if (oid.Value == default)
            {
                throw new ArgumentNullException(nameof(oid));
            }
            _oid = oid;
            _oidIsSet = true;
        }
    }
}