using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Harckade.CMS.PublicController.Email
{
    public class ContactControllerFunctions: BaseController
    {
        private IEmailService _emailService;
        private ILogger<ContactControllerFunctions> _appInsights;
        private ObservabilityId _oid;

        public ContactControllerFunctions(IEmailService emailService, ILogger<ContactControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _emailService = emailService;
            _emailService.UpdateOid(_oid);
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
                _appInsights.LogError($"CMS: ContactControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }


        [Function("SendContactForm")]
        public async Task<HttpResponseData> SendContactForm([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function SendContactForm executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                ContactDto contactForm = (ContactDto)JsonConvert.DeserializeObject<ContactDto>(body);
                var result = await _emailService.SendEmailAsync(contactForm);
                if (result.Failed)
                {
                    return FailResponse(result, req);
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
