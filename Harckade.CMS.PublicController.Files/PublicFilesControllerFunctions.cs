using System.Globalization;
using System.Net;
using System.Text;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.PublicController.Files
{
    public class PublicFilesControllerFunctions : BaseController
    {
        private IFileService _fileService;
        private ILogger<PublicFilesControllerFunctions> _appInsights;
        private ObservabilityId _oid;

        public PublicFilesControllerFunctions(IFileService fileService, ILogger<PublicFilesControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _fileService = fileService;
            _fileService.UpdateOid(_oid);
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
                _appInsights.LogError($"CMS: PublicFilesControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("DownloadFile")]
        public async Task<HttpResponseData> DownloadFile([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/{*path}")] HttpRequestData req, string path = "")
        {
            _appInsights.LogInformation("Function DownloadFile executed", _oid);
            return await ExecuteMethod(async () =>
            {
                if (path == null)
                {
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }
                var urlParts = path.Split('/');
                if (urlParts.Length < 1)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var fileType = urlParts[urlParts.Length - 1].Split('_')[0];
                FileType ftype = FileType.Binary;
                try
                {
                    ftype = (FileType)Enum.Parse(typeof(FileType), fileType, true);
                }
                catch
                {
                    _appInsights.LogError($"Function DownloadFile cannot parse file type: {fileType}", _oid);
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                var file = await _fileService.DownloadFile(new BlobId(path));
                if (file.Failed)
                {
                    return FailResponse(file, req);
                }
                if (file.Value == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                if (ftype == FileType.Image)
                {
                    response.Headers.Add("Expires", DateTime.Now.AddDays(30).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", DateTimeFormatInfo.InvariantInfo));
                    response.Headers.Add("Cache-Control", "max-age=3600");
                }
                response.Body = file.Value;
                return response;
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
