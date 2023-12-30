using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.Newsletter
{
    public class NewsletterFunctions
    {
        private INewsletterService _newsletterService;
        private INewsletterSubscriberService _newsletterSubscriberService;
        private ILogger<NewsletterFunctions> _appInsights;
        private ObservabilityId _oid;

        public NewsletterFunctions(INewsletterService newsletterService, INewsletterSubscriberService newsletterSubscriberService, ILogger<NewsletterFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _newsletterService = newsletterService;
            _newsletterService.UpdateOid(_oid);
            _newsletterSubscriberService = newsletterSubscriberService;
            _newsletterSubscriberService.UpdateOid(_oid);
            _appInsights = appInsights;
        }

        [Function("NewsletterFunctions")]
        public void Run([QueueTrigger("newsletter")] string newsletterEntry)
        {
            _appInsights.LogInformation($"Queue trigger function processed: NewsletterFunctions", _oid);
            _newsletterService.ProcessEntryFromQueue(newsletterEntry);
        }
    }
}
