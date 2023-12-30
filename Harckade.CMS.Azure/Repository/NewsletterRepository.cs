using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public class NewsletterRepository : BaseRepository, INewsletterRepository
    {
        private INewsletterMapper _mapper;
        public NewsletterRepository(IConfiguration configuration) : base(configuration, "newsletter")
        {
            _mapper = new NewsletterMapper();
        }

        public async Task DeleteNewsletter(Guid newsletterId)
        {
            var entry = await FindById(newsletterId);
            if (entry != null)
            {
                var generatedId = Math.Abs(Convert.ToInt64(newsletterId.ToString("N").Substring(0, 16), 16) % 5);
                await _tableClient.DeleteEntityAsync(generatedId.ToString(), newsletterId.ToString("N"));
            }
        }

        public async Task InsertOrUpdateNewsletter(Newsletter newsletter)
        {
            var newsletterEntity = _mapper.DomainToEntity(newsletter);
            var existingEntity = await FindById(newsletter.Id);
            if (existingEntity == null)
            {
                await _tableClient.AddEntityAsync(newsletterEntity);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(newsletterEntity, ETag.All, TableUpdateMode.Replace);
            }
        }


        public async Task<Newsletter> FindById(Guid newsletterId)
        {
            try
            {
                var generatedId = Math.Abs(Convert.ToInt64(newsletterId.ToString("N").Substring(0, 16), 16) % 5);
                var result = await _tableClient.GetEntityAsync<NewsletterEntity>($"{generatedId}", newsletterId.ToString("N"));
                return _mapper.EntityToDomain(result);
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("The specified resource does not exist"))
                {
                    return null;
                }
                throw new Exception(e.Message);
            }
        }


        public async Task<IEnumerable<Newsletter>> GetAll()
        {
            return await fetchAsyncNewsletters($"PartitionKey ne ''");
        }

        private async Task<IEnumerable<Newsletter>> fetchAsyncNewsletters(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<NewsletterEntity>(filter: query);
            var entries = new List<NewsletterEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<NewsletterEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (NewsletterEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(e => _mapper.EntityToDomain(e)).OrderBy(r => r.Timestamp);
        }

    }
}
