using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Mappers
{
    public class ArticleHelperMapper: IArticleHelperMapper
    {
        public ArticleHelperEntity DomainToEntity(ArticleHelper doc)
        {
            return new ArticleHelperEntity()
            {
                PartitionKey = doc.Id,
                RowKey = $"{doc.EncodedTitle.Replace("/", "ç")}",
                Reference = $"{doc.Reference}",
                Language = Enum.GetName(typeof(Language), doc.Language)
            };
        }
        public ArticleHelper EntityToDomain(ArticleHelperEntity doc)
        {
            return new ArticleHelper(doc);
        }
    }
}
