using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;


namespace Harckade.CMS.JwtAuthorization.Middleware
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly JwtSecurityTokenHandler _tokenValidator;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        public AuthenticationMiddleware(IConfiguration configuration)
        {
            var authority = configuration["AuthenticationAuthority"];
            var audience = configuration["AuthenticationClientId"];
            _tokenValidator = new JwtSecurityTokenHandler();
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = audience
            };
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
        }

        public async Task Invoke(
            FunctionContext context,
            FunctionExecutionDelegate next)
        {
            //Function is public and do not require any kind of authorization
            if (AuthorizationMiddleware.IsPublic(context))
            {
                await next(context);
                return;
            }

            string token = GetJwtToken(context);

            if (!_tokenValidator.CanReadToken(token))
            {
                // Token is malformed
                context.SetStatusCode(HttpStatusCode.Unauthorized);
                return;
            }

            // Get OpenID Connect metadata
            var validationParameters = _tokenValidationParameters.Clone();
            var openIdConfig = await _configurationManager.GetConfigurationAsync(default);
            validationParameters.ValidIssuer = openIdConfig.Issuer;
            validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

            try
            {
                // Validate token
                var principal = _tokenValidator.ValidateToken(token, validationParameters, out _);

                // Set principal + token in Features collection
                // They can be accessed from here later in the call chain
                context.Features.Set(new JwtPrincipalFeature(principal, token));
                try
                {
                    await next(context);
                }
                catch
                {
                    context.SetStatusCode(HttpStatusCode.InternalServerError);
                    return;
                }
            }
            catch
            {
                // Token is not valid (expired etc.)
                context.SetStatusCode(HttpStatusCode.Unauthorized);
                return;
            }
        }

        private static string GetJwtToken(FunctionContext context)
        {
            var req = context.GetHttpRequestData();
            if (!req.Headers.Contains("Authorization"))
            {
                return string.Empty;
            }
            string authorizationHeader = req.Headers.GetValues("Authorization").FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return authorizationHeader.Substring("Bearer ".Length).Trim();
        }
    }
}
