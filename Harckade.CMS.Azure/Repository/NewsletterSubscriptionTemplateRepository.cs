using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public class NewsletterSubscriptionTemplateRepository : BaseRepository, INewsletterSubscriptionTemplateRepository
    {
        private INewsletterSubscriptionTemplateMapper _mapper;
        public NewsletterSubscriptionTemplateRepository(IConfiguration configuration) : base(configuration, "newslettersubscriptiontemplate")
        {
            _mapper = new NewsletterSubscriptionTemplateMapper();
        }


        public async Task<NewsletterSubscriptionTemplate> GetTemplate()
        {
            try
            {
                var result = await _tableClient.GetEntityAsync<NewsletterSubscriptionTemplateEntity>("0", "0");
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

        public async Task InsertOrUpdateTemplate(NewsletterSubscriptionTemplate subscriptionTemplate)
        {
            var templateEntity = _mapper.DomainToEntity(subscriptionTemplate);
            var existingEntity = await GetTemplate();
            if (existingEntity == null)
            {
                await _tableClient.AddEntityAsync(templateEntity);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(templateEntity, ETag.All, TableUpdateMode.Replace);
            }
        }
    }
}
