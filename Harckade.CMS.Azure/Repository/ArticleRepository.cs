using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public class ArticleRepository : BaseRepository, IArticleRepository
    {
        private IArticleMapper _mapper;
        public ArticleRepository(IConfiguration configuration) : base(configuration, "articles")
        {
            _mapper = new ArticleMapper();
        }

        public async Task InsertOrUpdate(Article article)
        {
            var articleEntity = _mapper.DomainToEntity(article);
            var existingEntity = await FindById(article.Id);
            if(existingEntity == null)
            {
                await _tableClient.AddEntityAsync(articleEntity);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(articleEntity, ETag.All, TableUpdateMode.Replace);
            }
        }

        public async Task<Article> FindById(Guid articleId)
        {
            try
            {
                var generatedId = Math.Abs(Convert.ToInt64(articleId.ToString("N").Substring(0, 16), 16) % 5);
                var result = await _tableClient.GetEntityAsync<ArticleEntity>($"{generatedId}", articleId.ToString("N"));
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

        public async Task Delete(Guid articleId)
        {
            var entry = await FindById(articleId);
            if (entry != null)
            {
                var generatedId = Math.Abs(Convert.ToInt64(articleId.ToString("N").Substring(0, 16), 16) % 5);
                await _tableClient.DeleteEntityAsync(generatedId.ToString(), articleId.ToString("N"));
            }
        }

        public async Task<IEnumerable<Article>> GetAll()
        {
            return await fetchAsyncArticles($"PartitionKey ne ''");
        }

        public async Task<IEnumerable<Article>> GetAllPublished()
        {
            return await fetchAsyncArticles($"PartitionKey ne '' and Published eq true");
        }

        private async Task<IEnumerable<Article>> fetchAsyncArticles(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<ArticleEntity>(filter: query);
            var entries = new List<ArticleEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<ArticleEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (ArticleEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(e => _mapper.EntityToDomain(e)).OrderBy(r => r.Timestamp);
        }

        public async Task<IEnumerable<Article>> GetArticlesMarkedAsDeleted()
        {
            return await fetchAsyncArticles($"PartitionKey ne '' and MarkedAsDeleted eq true");
        }
    }
}
