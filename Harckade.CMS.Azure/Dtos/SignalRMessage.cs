using Newtonsoft.Json.Linq;

namespace Harckade.CMS.Azure.Dtos
{
    public class SignalRMessage
    {
        public string Action { get; set; }
        public string Page { get; set; }
        public JObject Payload { get; set; }
        public Guid RandomId { get; set; }
    }
}
