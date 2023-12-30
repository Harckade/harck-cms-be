using Harckade.CMS.Azure.Domain;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Harckade.CMS.SignalRController
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Editor, UserRoles.Administrator })]
    public class SignalRControllerFunctions : BaseController
    {
        private ObservabilityId _oid;
        private ILogger<SignalRControllerFunctions> _appInsights;

        public SignalRControllerFunctions(ILogger<SignalRControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
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
                _appInsights.LogError($"CMS: SignalRControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        private HttpResponseData ExecuteMethod(Func<HttpResponseData> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: SignalRController Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("signalRNegotiate")]
        public HttpResponseData Negotiate([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "cms/notifications/negotiate")] HttpRequestData req,
      [SignalRConnectionInfoInput(HubName = "harckadeEditor")] SignalRConnectionInfo connectionInfo)
        {
            return ExecuteMethod(() =>
            {
                return JsonResponse.Get(connectionInfo, req);
            });
        }

        [Function("signalRSendMessage")]
        [SignalROutput(HubName = "harckadeEditor")]
        public SignalRMessageAction signalRSendMessage([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/notifications/sendMessage")] HttpRequestData req)
        {
            try
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                Azure.Dtos.SignalRMessage message = JsonConvert.DeserializeObject<Azure.Dtos.SignalRMessage>(body);

                return new SignalRMessageAction("newMessage", new object[] { new { action = message.Action, page = message.Page, payload = JsonConvert.SerializeObject(message.Payload), randomId = message.RandomId } });
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: SignalRController Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
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