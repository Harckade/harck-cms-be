using Harckade.CMS.Azure.Dtos;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Harckade.CMS.Azure.Domain
{
    /*
    built based on documentation available at https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
    success	Boolean indicating if validation was successful
    challenge_ts	ISO timestamp when the challenge was solved
    hostname	Hostname where the challenge was served
    error-codes	Array of error codes (if validation failed)
    action	Custom action identifier from client-side
    cdata	Custom data payload from client-side
    */
    public class TurnstileResponse
    {
        public bool Success { get; private set; }
        public DateTime ChallengeTs { get; private set; }
        public string Hostname { get; private set; }
        public IList<string> ErrorCodes { get; private set; }
        public string Action { get; private set; }

        private bool isNullDate(DateTime date)
        {
            return date == default || date == new DateTime(1601, 1, 1);
        }

        public TurnstileResponse(TurnstileResponseDto response){
            Success = response.Success;
            if (isNullDate(response.ChallengeTs)){
                 throw new ArgumentException(nameof(response.ChallengeTs));
            }
            ChallengeTs = response.ChallengeTs;
            if (response.Hostname.Length > 100){
                throw new ArgumentException(nameof(response.Hostname));
            }
            Hostname = response.Hostname;
            var errorCodes = response.ErrorCodes == null || response.Success ? new List<string>()   : response.ErrorCodes.ToList();

            if (errorCodes.Any()){
                var possibleValues = new List<string>(){"missing-input-secret", "invalid-input-secret", "missing-input-response", "invalid-input-response", "bad-request", "timeout-or-duplicate", "internal-error"};
                foreach(var code in errorCodes){
                    if (!possibleValues.Contains(code)){
                          throw new ArgumentException(nameof(response.ErrorCodes));
                    }
                }
            }
            ErrorCodes = errorCodes;
            if (string.IsNullOrWhiteSpace(response.Action) || response.Action.Length > 100){
                throw new ArgumentException(nameof(response.Action));
            }
            Action = response.Action;
        }

        public TrunstileResponse(string errorCode){
            Success = false;
            var errorCodes = new List<string>();
            errorCodes.Add(errorCode);
            ErrorCodes = errorCodes;
        }
    }
}
