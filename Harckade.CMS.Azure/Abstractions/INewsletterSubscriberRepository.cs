using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterSubscriberRepository
    {
        Task InsertOrUpdateSubscriber(NewsletterSubscriber subscriber);
        Task DeleteSubscriber(Guid subscriberId);
        Task<IEnumerable<NewsletterSubscriber>> GetAll();
        Task<IEnumerable<NewsletterSubscriber>> GetAllByLanguage(Language language);
        Task<NewsletterSubscriber> FindByEmailAndLanguage(string email, Language language);
        Task<NewsletterSubscriber> FindById(Guid id);
        Task<NewsletterSubscriber> FindByPersonalToken(string personalToken);
    }
}
