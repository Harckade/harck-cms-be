using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterRepository
    {
        Task InsertOrUpdateNewsletter(Newsletter newsletter);
        Task DeleteNewsletter(Guid newsletterId);
        Task<Newsletter> FindById(Guid newsletterId);
        Task<IEnumerable<Newsletter>> GetAll();
    }
}