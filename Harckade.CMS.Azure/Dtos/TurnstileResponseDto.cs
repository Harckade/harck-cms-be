using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Dtos
{
    public class TurnstileResponseDto
    {
        public bool Success { get; set; }
        public DateTime? ChallengeTs { get; set; }
        public string Hostname { get; set; }
        public IList<string>? ErrorCodes { get; set; }
        public string Action { get; set; }
    }
}