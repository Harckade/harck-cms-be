using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IServiceBase
    {
        /// <summary>
        /// Update observability ID
        /// </summary>
        /// <param name="oid"></param>
        /// <exception cref="ArgumentNullException"></exception>
        void UpdateOid(ObservabilityId oid);
    }
}
