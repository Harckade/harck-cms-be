using Harckade.CMS.JwtAuthorization.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;
using System.Reflection;
using System.Security.Claims;

namespace Harckade.CMS.JwtAuthorization.Middleware
{
    public class AuthorizationMiddleware : IFunctionsWorkerMiddleware
    {
        private const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

        public async Task Invoke(
            FunctionContext context,
            FunctionExecutionDelegate next)
        {
            //Function is public, skip token validation
            if (IsPublic(context))
            {
                await next(context);
                return;
            }

            var principalFeature = context.Features.Get<JwtPrincipalFeature>();
            if (!AuthorizePrincipal(context, principalFeature.Principal))
            {
                context.SetStatusCode(HttpStatusCode.Forbidden);
                return;
            }

            await next(context);
        }

        private static bool AuthorizePrincipal(FunctionContext context, ClaimsPrincipal principal)
        {
            // This authorization implementation was made
            // for Azure AD. Your identity provider might differ.
            if (principal.HasClaim(c => c.Type == ScopeClaimType))
            {
                // Request made with delegated permissions, check scopes and user roles
                return AuthorizeDelegatedPermissions(context, principal);
            }

            // Request made with application permissions, check app roles
            context.SetStatusCode(HttpStatusCode.Unauthorized);
            return false;
        }

        private static bool AuthorizeDelegatedPermissions(FunctionContext context, ClaimsPrincipal principal)
        {
            var targetMethod = context.GetTargetFunctionMethod();

            var (acceptedScopes, acceptedUserRoles) = GetAcceptedScopesAndUserRoles(targetMethod);

            var userRoles = principal.FindAll(ClaimTypes.Role);
            var userHasAcceptedRole = userRoles.Any(ur => acceptedUserRoles.Contains(ur.Value, StringComparer.OrdinalIgnoreCase));

            // Scopes are stored in a single claim, space-separated
            var callerScopes = (principal.FindFirst(ScopeClaimType)?.Value ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var callerHasAcceptedScope = callerScopes.Any(cs => acceptedScopes.Contains(cs, StringComparer.OrdinalIgnoreCase));

            // This app requires both a scope and user role
            // when called with scopes, so we check both
            return userHasAcceptedRole && callerHasAcceptedScope;
        }

        private static List<T> GetEnumList<T>()
        {
            T[] array = (T[])Enum.GetValues(typeof(T));
            List<T> list = new List<T>(array);
            return list;
        }

        public static (List<string> scopes, List<string> userRoles) GetAcceptedScopesAndUserRoles(MethodInfo targetMethod)
        {
            var attributes = GetCustomAttributesOnClassAndMethod<AuthorizeAttribute>(targetMethod);
            // If scopes A and B are allowed at class level,
            // and scope A is allowed at method level,
            // then only scope A can be allowed.
            // This finds those common scopes and
            // user roles on the attributes.
            var allScopes = GetEnumList<Scopes>().Select(s => Enum.GetName(typeof(Scopes), s));
            var acceptedScopes = attributes.Select(a => a.Scopes).FirstOrDefault().AsEnumerable();
            var scopes = acceptedScopes?.Where(s => allScopes.Contains(Enum.GetName(typeof(Scopes), s))).Select(scope => scope.ToString()).ToList();

            var allUserRoles = GetEnumList<UserRoles>().Select(r => Enum.GetName(typeof(UserRoles), r));
            var acceptedRoles = attributes.Select(a => a.UserRoles).FirstOrDefault().AsEnumerable();
            var userRoles = acceptedRoles?.Where(r => allUserRoles.Contains(Enum.GetName(typeof(UserRoles), r))).Select(role => role.ToString()).ToList();
            return (scopes, userRoles);
        }

        public static bool IsPublic(FunctionContext context)
        {
            var targetMethod = context.GetTargetFunctionMethod();
            var attributes = AuthorizationMiddleware.GetCustomAttributesOnClassAndMethod<AuthorizeAttribute>(targetMethod);
            return attributes.Select(a => a.IsPublic).FirstOrDefault();
        }

        private static List<T> GetCustomAttributesOnClassAndMethod<T>(MethodInfo targetMethod)
            where T : Attribute
        {
            var methodAttributes = targetMethod.GetCustomAttributes<T>();
            var classAttributes = targetMethod.DeclaringType.GetCustomAttributes<T>();
            return methodAttributes.Concat(classAttributes).ToList();
        }
    }
}
