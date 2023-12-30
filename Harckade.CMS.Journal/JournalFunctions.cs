using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.Journal
{
    public class JournalFunctions
    {
        private IJournalService _journalService;
        private ILogger<JournalFunctions> _appInsights;
        private ObservabilityId _oid;

        public JournalFunctions(IJournalService journalService, ILogger<JournalFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _appInsights = appInsights;
        }


        [Function("JournalFunction")]
        public void Run([QueueTrigger("journal")] string message, FunctionContext context)
        {
            _appInsights.LogInformation($"Queue trigger function processed: JournalFunctions", _oid);
            if (!context.BindingContext.BindingData.ContainsKey("InsertionTime"))
            {
                _appInsights.LogError($"Queue trigger JournalFunctions: InsertionTime not found", _oid);
                throw new System.ArgumentException("InsertionTime");
            }
            var dateAsString = context.BindingContext.BindingData["InsertionTime"].ToString().Replace("\"", "");
            System.DateTimeOffset insertedOn = System.DateTimeOffset.Parse(dateAsString);
            _journalService.FromQueueToStorage(message, insertedOn);
        }
    }
}
