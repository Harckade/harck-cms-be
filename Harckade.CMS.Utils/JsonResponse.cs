using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;

namespace Harckade.CMS.Utils
{
    public static class JsonResponse
    {
        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static HttpResponseData Get(object obj, HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Body = GenerateStreamFromString(JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
            response.Headers.Add("Content-type", "application/json; charset=utf-8");
            return response;
        }
    }
}
