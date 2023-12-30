using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Domain
{
    public class ArticleHelper
    {
        public string Id { get; private set; }
        public string EncodedTitle { get; private set; }
        public Guid Reference { get; private set; }
        public Language Language { get; private set; }
        public ArticleHelper(string title, Guid reference, Language language)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentNullException(nameof(title));
            }
            if (reference == default)
            {
                throw new ArgumentNullException(nameof(reference));
            }
            if (language == default)
            {
                throw new ArgumentException(nameof(language));
            }
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(title);
            //Replace '/' with 'ç' because of the TableStorage naming format
            EncodedTitle = $"{removeCedilha(Convert.ToBase64String(plainTextBytes))}_{Enum.GetName(language)}";
            Id = $"{(int)EncodedTitle[0] % 5}";
            Reference = reference;
            Language = language;
        }

        public ArticleHelper(ArticleHelperEntity articleHelperEntity)
        {
            if (articleHelperEntity == null)
            {
                throw new ArgumentNullException(nameof(articleHelperEntity));
            }
            Guid reference = Guid.Empty;
            if (!Guid.TryParse(articleHelperEntity.Reference, out reference))
            {
                throw new ArgumentNullException(nameof(reference));
            }

            Id = articleHelperEntity.PartitionKey;
            EncodedTitle = removeCedilha(articleHelperEntity.RowKey);
            Reference = reference;
            Language = (Language)Enum.Parse(typeof(Language), articleHelperEntity.Language);
        }

        private string removeCedilha(string str)
        {
            return str.Replace("/", "ç");
        }
    }
}
