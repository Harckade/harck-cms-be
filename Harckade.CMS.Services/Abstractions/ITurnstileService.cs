using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Services.Abstractions
{
    public interface ITurnstileService : IServiceBase
    {
        /// <summary>
        /// Implemented in accordance with Cloudflare documentation
        /// https://developers.cloudflare.com/turnstile/get-started/server-side-validation
        /// </summary>
        /// <param name="token">token provided by the client</param>
        /// <param name="remoteip">The visitor's IP address</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<Result<TurnstileResponse>> ValidateTokenAsync(string token, string remoteip = null);
    }
}
