using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Harckade.CMS.Services
{
    public class JournalService : ServiceBase, IJournalService
    {
        private IJournalRepository _journalRepository;
        private QueueClient _queueClient;
        private readonly ILogger<JournalService> _appInsights;

        /// <summary>
        /// Initialize journal service
        /// </summary>
        /// <param name="journalRepository"></param>
        /// <param name="queueClient"></param>
        /// <param name="appInsights"></param>
        public JournalService(IJournalRepository journalRepository, QueueClient queueClient, ILogger<JournalService> appInsights)
        {
            _journalRepository = journalRepository;
            _queueClient = queueClient;
            _queueClient.CreateIfNotExists();
            _appInsights = appInsights;
            _oidIsSet = false;
        }

        #region Queues
        public async Task AddEntryToQueue(FunctionContext context, string description = "")
        {
            _appInsights.LogDebug($"JournalService | AddEntryToQueue", _oid);
            var user = GetUser(context);
            var entry = new JournalEntryQueue(user.Email, user.Id, context.FunctionDefinition == null && context.ToString().StartsWith("Mock<FunctionContext") ? "mock" : context.FunctionDefinition.Name, description);
            var serializedEntry = JsonConvert.SerializeObject(entry);
            var encodedEntry = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedEntry));
            await _queueClient.SendMessageAsync(encodedEntry);
        }

        public async Task FromQueueToStorage(string message, DateTimeOffset insertedOn)
        {
            _appInsights.LogDebug($"JournalService | FromQueueToStorage", _oid);
            var queueEntry = JsonConvert.DeserializeObject<JournalEntryQueue>(message);
            var lastEntry = await _journalRepository.GetLastEntry();
            var lastHash = "first";
            if (lastEntry != null)
            {
                if (string.IsNullOrWhiteSpace(lastEntry.Hash))
                {
                    throw new NullReferenceException(nameof(lastEntry.Hash));
                }
                lastHash = lastEntry.Hash;
            }
            var entry = new JournalEntry(lastHash, queueEntry, insertedOn);
            await _journalRepository.Insert(entry);
        }
        #endregion

        public async Task<Result<IEnumerable<JournalEntry>>> GetEntries(DateTimeOffset startDate = default, DateTimeOffset endDate = default)
        {
            _appInsights.LogInformation($"JournalService | GetEntries: {startDate} | {endDate}", _oid);
            if (startDate == default || startDate < DateTimeOffset.MinValue)
            {
                return Result.Fail<IEnumerable<JournalEntry>>(Azure.Enums.Failure.InvalidStartDate);
            }
            if (endDate == default)
            {
                return Result.Fail<IEnumerable<JournalEntry>>(Azure.Enums.Failure.InvalidEndDate);
            }
            if (endDate > DateTimeOffset.UtcNow.ToUniversalTime())
            {
                endDate = DateTimeOffset.UtcNow.ToUniversalTime();
            }
            var journalEntries = await _journalRepository.Get(startDate, endDate);
            return Result.Ok(journalEntries);
        }
    }
}
