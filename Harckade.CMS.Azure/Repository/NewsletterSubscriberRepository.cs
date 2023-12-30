using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;
using Language = Harckade.CMS.Azure.Enums.Language;

namespace Harckade.CMS.Azure.Repository
{
    public class NewsletterSubscriberRepository : BaseRepository, INewsletterSubscriberRepository
    {
        private INewsletterSubscriberMapper _mapper;
        public NewsletterSubscriberRepository(IConfiguration configuration) : base(configuration, "newslettersubscribers")
        {
            _mapper = new NewsletterSubscriberMapper();
        }

        public async Task DeleteSubscriber(Guid subscriberId)
        {
            var entry = await FindById(subscriberId);
            if (entry != null)
            {
                var generatedId = Math.Abs(Convert.ToInt64(subscriberId.ToString("N").Substring(0, 16), 16) % 5);
                await _tableClient.DeleteEntityAsync(generatedId.ToString(), subscriberId.ToString("N"));
            }
        }

        public async Task InsertOrUpdateSubscriber(NewsletterSubscriber subscriber)
        {
            var subscriberEntity = _mapper.DomainToEntity(subscriber);
            var existingEntity = await FindById(subscriber.Id);
            if (existingEntity == null)
            {
                await _tableClient.AddEntityAsync(subscriberEntity);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(subscriberEntity, ETag.All, TableUpdateMode.Replace);
            }
        }

        public async Task<NewsletterSubscriber> FindById(Guid subscriberId)
        {
            try
            {
                var generatedId = Math.Abs(Convert.ToInt64(subscriberId.ToString("N").Substring(0, 16), 16) % 5);
                var result = await _tableClient.GetEntityAsync<NewsletterSubscriberEntity>($"{generatedId}", subscriberId.ToString("N"));
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

        public async Task<IEnumerable<NewsletterSubscriber>> GetAll()
        {
            return await fetchAsyncClients($"PartitionKey ne ''");
        }

        private async Task<IEnumerable<NewsletterSubscriber>> fetchAsyncClients(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<NewsletterSubscriberEntity>(filter: query);
            var entries = new List<NewsletterSubscriberEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<NewsletterSubscriberEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (NewsletterSubscriberEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(e => _mapper.EntityToDomain(e));
        }

        public async Task<IEnumerable<NewsletterSubscriber>> GetAllByLanguage(Language language)
        {
            return await fetchAsyncClients($"PartitionKey ne '' and Language eq '{Enum.GetName(typeof(Language), language)}'");
        }

        public async Task<NewsletterSubscriber> FindByEmailAndLanguage(string email, Language language)
        {
            try
            {
                var clients = await fetchAsyncClients($"EmailAddress eq '{email}' and Language eq '{Enum.GetName(typeof(Language), language)}'");
                return clients.FirstOrDefault();
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

        public async Task<NewsletterSubscriber> FindByPersonalToken(string personalToken)
        {
            try
            {
                var clients = await fetchAsyncClients($"PersonalToken eq '{personalToken}'");
                return clients.FirstOrDefault();
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
    }
}
