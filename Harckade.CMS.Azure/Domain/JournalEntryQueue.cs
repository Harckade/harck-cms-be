using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class JournalEntryQueue
    {
        [JsonProperty]
        public string UserEmail { get; set; }
        [JsonProperty]
        public Guid UserId { get; set; }
        [JsonProperty]
        public string ControllerMethod { get; set; }
        [JsonProperty]
        public string Description { get; set; }

        public JournalEntryQueue(string userEmail, Guid userId, string controllerMethod, string description)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                throw new ArgumentNullException(nameof(userEmail));
            }
            if (userId == default)
            {
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrEmpty(controllerMethod))
            {
                throw new ArgumentNullException(nameof(controllerMethod));
            }
            UserEmail = userEmail;
            UserId = userId;
            ControllerMethod = controllerMethod;
            Description = description;
        }
    }
}
