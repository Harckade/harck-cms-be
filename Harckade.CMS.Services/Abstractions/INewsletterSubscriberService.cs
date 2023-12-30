using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Services.Abstractions
{
    public interface INewsletterSubscriberService : IServiceBase
    {
        Task<Result<IEnumerable<NewsletterSubscriber>>> GetSubscribers();
        Task<Result<IEnumerable<NewsletterSubscriber>>> GetSubscribersByLanguage(Language language);
        Task<Result<NewsletterSubscriber>> FindSubscriberById(Guid subscriberId);
        Task<Result<NewsletterSubscriber>> FindSubscriberByPersonalToken(string token);
        Task<Result<NewsletterSubscriber>> FindByEmail(string email, Language language);
        Task<Result<NewsletterSubscriber>> FindByDto(NewsletterSubscriberDto newsletterSubscriberDto);
        Task<Result> ConfirmEmailAddress(NewsletterSubscriber subscriber);
        Task<Result> RemoveSubscriberById(Guid subscriberId);
        Task<Result> RemoveSubscriberByEmail(string email, Language language);
        Task<Result> RemoveSubscriberByDto(NewsletterSubscriberDto newsletterSubscriberDto);
        Task<Result<NewsletterSubscriber>> AddSubscriber(string email, Language language = Language.None);
        Task<Result> SendConfirmationEmail(NewsletterSubscriptionTemplate template, string email, Language language, string content = "");
    }
}
