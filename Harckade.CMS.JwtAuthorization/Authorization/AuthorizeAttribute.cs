namespace Harckade.CMS.JwtAuthorization.Authorization
{
    /// <summary>
    /// Set at Function class or method level to
    /// set what scopes/user roles/app roles are
    /// required in requests.
    /// </summary>
    /// <remarks>
    /// If you do not specify app roles, calls
    /// without user context will fail.
    /// Same goes for scopes/user roles;
    /// calls with user context will fail if
    /// both are not specified.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute
    {
        /// <summary>
        /// Defines which scopes (aka delegated permissions)
        /// are accepted. In this sample these
        /// must be combined with <see cref="UserRoles"/>.
        /// </summary>
        public Scopes[] Scopes { get; set; } = Array.Empty<Scopes>();
        /// <summary>
        /// Defines which user roles are accepted.
        /// Must be combined with <see cref="Scopes"/>.
        /// </summary>
        public UserRoles[] UserRoles { get; set; } = Array.Empty<UserRoles>();
        public bool IsPublic { get; set; } = false;
    }
}
