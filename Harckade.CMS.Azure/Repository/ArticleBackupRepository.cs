using Azure;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public class ArticleBackupRepository : BaseRepository, IArticleBackupRepository
    {
        private IArticleBackupMapper _mapper;
        public ArticleBackupRepository(IConfiguration configuration) : base(configuration, "articlesbackup")
        {
            _mapper = new ArticleBackupMapper();
        }

        public async Task<IEnumerable<ArticleBackup>> FindAllById(Guid articleId)
        {
            var generatedId = Math.Abs(Convert.ToInt64(articleId.ToString("N").Substring(0, 16), 16) % 5);
            return await fetchAsyncEntries($"PartitionKey eq '{generatedId}' and RowKey ge '{articleId.ToString("N")}_' and RowKey lt '{articleId.ToString("N")}_~'");
        }

        public async Task Delete(Guid articleId)
        {
            var entries = await FindAllById(articleId);
            foreach (var entry in entries)
            {
                var entity = _mapper.DomainToEntity(entry);
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }
        }

        private async Task<IEnumerable<ArticleBackup>> fetchAsyncEntries(string query)
        {
            var queryResultsFilter = _tableClient.QueryAsync<ArticleBackupEntity>(filter: query);
            var entries = new List<ArticleBackupEntity>();
            var continuationToken = string.Empty;
            await foreach (Page<ArticleBackupEntity> page in queryResultsFilter.AsPages(continuationToken))
            {
                foreach (ArticleBackupEntity entity in page.Values)
                {
                    entries.Add(entity);
                }
                continuationToken = page.ContinuationToken;
            }
            return entries.Select(r => _mapper.EntityToDomain(r)).OrderBy(r => r.ModificationDate);
        }

        public async Task<IEnumerable<ArticleBackup>> FindById(Guid articleId, Language language)
        {
            var generatedId = Math.Abs(Convert.ToInt64(articleId.ToString("N").Substring(0, 16), 16) % 5);
            var lang = Enum.GetName(language);
            var articleIdStr = articleId.ToString("N");
            var startsWith = $"{articleIdStr}_{lang}_";
            var endsWith = $"{articleIdStr}_{lang.Remove(lang.Length - 1, 1) + (char)(((int)lang[lang.Length - 1]) + 1)}_";
            return await fetchAsyncEntries($"PartitionKey eq '{generatedId}' and RowKey ge '{startsWith}' and RowKey lt '{endsWith}~'");
        }

        public async Task Insert(ArticleBackup article)
        {
            var articleEntity = _mapper.DomainToEntity(article);
            await _tableClient.AddEntityAsync(articleEntity);
        }
    }
}
