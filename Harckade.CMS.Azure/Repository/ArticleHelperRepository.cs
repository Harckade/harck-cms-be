using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Harckade.CMS.Azure.Repository
{
    public class ArticleHelperRepository : BaseRepository, IArticleHelperRepository
    {
        private IArticleHelperMapper _mapper;
        public ArticleHelperRepository(IConfiguration configuration) : base(configuration, "articleshelpers")
        {
            _mapper = new ArticleHelperMapper();
        }

        public async Task InsertOrUpdate(ArticleHelper article)
        {
            var articleHelperEntity = _mapper.DomainToEntity(article);
            var existingEntity = await FindByEncodedTitle(article.EncodedTitle);
            if (existingEntity == null)
            {
                await _tableClient.AddEntityAsync(articleHelperEntity);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(articleHelperEntity, ETag.All, TableUpdateMode.Replace);
            }
        }

        public async Task<IEnumerable<ArticleHelper>> FindByArticle(Article article)
        {
            return await fetchAsyncArticles($"Reference eq '{article.Id}'");
        }

        public async Task<ArticleHelper> FindByTitle(string title, Language language)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(title);
            string encodedTitle = $"{Convert.ToBase64String(plainTextBytes).Replace("/", "ç")}_{Enum.GetName(language)}";
            return await FindByEncodedTitle(encodedTitle);
        }

        public async Task<ArticleHelper> FindByEncodedTitle(string encodedTitle)
        {
            try
            {
                var result = await _tableClient.GetEntityAsync<ArticleHelperEntity>($"{(int)encodedTitle[0] % 5}", encodedTitle);
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

        public async Task DeleteByTitle(string title, Language language)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes($"{title}_{Enum.GetName(language)}");
            string encodedTitle = Convert.ToBase64String(plainTextBytes);
            await DeleteByEncodedTitle(encodedTitle);
        }

        public async Task DeleteByEncodedTitle(string encodedTitle)
        {
            var entry = await FindByEncodedTitle(encodedTitle);
            if (entry != null)
            {
                await _tableClient.DeleteEntityAsync($"{(int)encodedTitle[0] % 5}", encodedTitle);
            }
        }


        public async Task<IEnumerable<ArticleHelper>> FetchAll()
        {
            return await fetchAsyncArticles($"RowKey ne ''");
        }

        private async Task<IEnumerable<ArticleHelper>> fetchAsyncArticles(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<ArticleHelperEntity>(filter: query);
            var entries = new List<ArticleHelperEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<ArticleHelperEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (ArticleHelperEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(e => _mapper.EntityToDomain(e));
        }
    }
}
