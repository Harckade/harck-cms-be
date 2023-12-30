using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public abstract class BaseRepository
    {
        protected TableClient _tableClient;

        public BaseRepository(IConfiguration configuration, string tableName)
        {
            _tableClient = new TableClient(configuration["AzureWebJobsStorage"], tableName);
            _tableClient.CreateIfNotExists();
        }
    }
}
