using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Harckade.CMS.PublicController.Newsletter
{
    public class PublicNewsletterControllerFunctions : BaseController
    {
        private INewsletterSubscriberService _newsletterSubscriberService;
        private ILogger<PublicNewsletterControllerFunctions> _appInsights;
        private ObservabilityId _oid;

        public PublicNewsletterControllerFunctions(INewsletterSubscriberService newsletterSubscriberService, ILogger<PublicNewsletterControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _newsletterSubscriberService = newsletterSubscriberService;
            _newsletterSubscriberService.UpdateOid(_oid);
            _appInsights = appInsights;
        }

        private async Task<HttpResponseData> ExecuteMethod(Func<Task<HttpResponseData>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: PublicNewsletterControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("SubscribeToNewsletter")]
        public async Task<HttpResponseData> SubscribeToNewsletter([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "newsletter")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function SubscribeToNewsletter executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                NewsletterSubscriberDto subscriber = (NewsletterSubscriberDto)JsonConvert.DeserializeObject<NewsletterSubscriberDto>(body);

                string lang = string.Empty;
                Language language = default;
                if (!string.IsNullOrWhiteSpace(subscriber.Language))
                {
                    lang = subscriber.Language;
                    lang = lang.ToUpper();
                    lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                    Enum.TryParse(lang, true, out language);
                }
                if (language == default)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                var result = await _newsletterSubscriberService.AddSubscriber(subscriber.EmailAddress, language);
                if (result.Failed)
                {
                    if (result.FailureReason == Failure.UserAlreadyExists)
                    {
                        return req.CreateResponse(HttpStatusCode.OK);
                    }
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("ConfirmNewsletterEmail")]
        public async Task<HttpResponseData> ConfirmNewsletterEmail([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "newsletter/confirm")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function ConfirmNewsletterEmail executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                NewsletterSubscriberDto subscriber = (NewsletterSubscriberDto)JsonConvert.DeserializeObject<NewsletterSubscriberDto>(body);
                var tmpSubscriberResult = await _newsletterSubscriberService.FindSubscriberByPersonalToken(subscriber.PersonalToken);
                if (tmpSubscriberResult.Failed)
                {
                    return FailResponse(tmpSubscriberResult, req);
                }
                var tmpSubscriber = tmpSubscriberResult.Value;
                if (tmpSubscriber.EmailAddress == subscriber.EmailAddress)
                {
                    var result = await _newsletterSubscriberService.ConfirmEmailAddress(tmpSubscriber);
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("UnsubscribeNewsletter")]
        public async Task<HttpResponseData> UnsubscribeNewsletter([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "newsletter/unsubscribe")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function UnsubscribeNewsletter executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                NewsletterSubscriberDto subscriber = (NewsletterSubscriberDto)JsonConvert.DeserializeObject<NewsletterSubscriberDto>(body);
                var tmpSubscriberResult = await _newsletterSubscriberService.FindSubscriberByPersonalToken(subscriber.PersonalToken);
                if (tmpSubscriberResult.Failed)
                {
                    return FailResponse(tmpSubscriberResult, req);
                }
                var tmpSubscriber = tmpSubscriberResult.Value;
                if (tmpSubscriber.EmailAddress == subscriber.EmailAddress)
                {
                    var result = await _newsletterSubscriberService.RemoveSubscriberById(tmpSubscriber.Id);
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("RobotsTxt")]
        public async Task<string> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "robots.txt")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function GetRobotsTxt executed", _oid);
            var sb = new StringBuilder();
            sb.AppendLine("user-agent: *");
            sb.AppendLine("Disallow: /");
            return await Task.FromResult(sb.ToString());
        }
    }
}
