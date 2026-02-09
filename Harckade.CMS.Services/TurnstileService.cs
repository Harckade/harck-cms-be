using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace Harckade.CMS.Services
{
    public class TurnstileService : ServiceBase, ITurnstileService
    {
        private IConfiguration _configuration;
        private ILogger<TurnstileService> _appInsights;
        private const string _siteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
        private string _secretKey;

        public TurnstileService(ILogger<TurnstileService> appInsights, IConfiguration configuration){
            _configuration = configuration;
            _appInsights = appInsights;
            _oidIsSet = false;

            var secretKey = _configuration["secretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentNullException(nameof(secretKey));
            }
            _secretKey = secretKey;
        }

        public async Task<Result<TurnstileResponse>> ValidateTokenAsync(string token, string remoteip = null)
        {
            var parameters = new Dictionary<string, string>
            {
                { "secret", _secretKey },
                { "response", token }
            };

            if (!string.IsNullOrEmpty(remoteip))
            {
                parameters.Add("remoteip", remoteip);
            }

            var postContent = new FormUrlEncodedContent(parameters);

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Harck-CMS", "1.1"));
                var response = await httpClient.PostAsync(_siteverifyUrl, postContent);
                var stringContent = await response.Content.ReadAsStringAsync();
                return Result.Ok<TurnstileResponse>(TurnstileResponse(JsonConvert.DeserializeObject<TurnstileResponseDto>(stringContent)));
            }
            catch (Exception ex)
            {
                return Result.Fail<Article>(new TurnstileResponse("internal-error"));
            }
        }

    }
}
