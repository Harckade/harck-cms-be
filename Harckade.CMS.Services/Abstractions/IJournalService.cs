using Harckade.CMS.Azure.Domain;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IJournalService : IServiceBase
    {
        /// <summary>
        /// Add an entry to the Journal queue. Eventually the entry will be processed and added to the journal repository
        /// </summary>
        /// <param name="context"></param>
        /// <param name="description">A detailed description of the action to log</param>
        Task AddEntryToQueue(FunctionContext context, string description = "");
        /// <summary>
        /// Process message from the queue and add an entry to the journal repository.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="insertedOn"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        Task FromQueueToStorage(string message, DateTimeOffset insertedOn);
        /// <summary>
        /// Retrieve journal logs. This method only returns the actions that were performed by users.
        /// For boservability logs, visit Azure portal
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>A collection of Journal entries</returns>
        Task<Result<IEnumerable<JournalEntry>>> GetEntries(DateTimeOffset startDate = default, DateTimeOffset endDate = default);
    }
}
