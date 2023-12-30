using Harckade.CMS.Utils;

namespace Harckade.CMS.Azure.Domain
{
    public class BlobId
    {
        private string _id;
        public BlobId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            Validations.ValidateBlobName(id);
            _id = id;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                throw new ArgumentNullException("BlobId not initialized");
            }
            return _id;
        }
    }
}
