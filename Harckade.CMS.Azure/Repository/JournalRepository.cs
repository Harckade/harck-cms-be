using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Harckade.CMS.Azure.Repository
{
    public class JournalRepository : BaseRepository, IJournalRepository
    {
        private IJournalMapper _mapper;

        public JournalRepository(IConfiguration configuration) : base(configuration, "journal")
        {
            _mapper = new JournalMapper();
        }

        public async Task Insert(JournalEntry entry)
        {
            var journalEntryEntity = _mapper.DomainToEntity(entry);
            await _tableClient.AddEntityAsync(journalEntryEntity);
        }

        private async Task<IEnumerable<JournalEntry>> fetchAsyncArticles(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<JournalEntryEntity>(filter: query);
            var entries = new List<JournalEntryEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<JournalEntryEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (JournalEntryEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(e => _mapper.EntityToDomain(e)).OrderBy(r => r.ReversedTicks);
        }

        public async Task<IEnumerable<JournalEntry>> Get(DateTimeOffset startDate = default, DateTimeOffset endDate = default)
        {
            var query = $"PartitionKey ne ''";
            if (startDate != default)
            {
                query = $"{query} and Timestamp ge datetime'{startDate.UtcDateTime.ToString("yyyy-MM-ddThh:mm:ssZ", CultureInfo.InvariantCulture)}'";
            }
            if (endDate != default)
            {
                query = $"{query} and Timestamp le datetime'{endDate.UtcDateTime.ToString("yyyy-MM-ddThh:mm:ssZ", CultureInfo.InvariantCulture)}'";
            }
            return await fetchAsyncArticles(query);
        }

        public async Task<JournalEntry> GetLastEntry()
        {
            var queryResultsFilter = _tableClient.QueryAsync<JournalEntryEntity>(filter: "PartitionKey ne ''", maxPerPage: 1);

            await foreach (Page<JournalEntryEntity> page in queryResultsFilter.AsPages(null, 1))
            {
                return _mapper.EntityToDomain(page.Values.FirstOrDefault());
            }
            return null;
        }
    }
}
