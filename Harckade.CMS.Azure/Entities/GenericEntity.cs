using Azure;
using Azure.Data.Tables;

namespace Harckade.CMS.Azure.Entities
{
    public abstract class GenericEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
