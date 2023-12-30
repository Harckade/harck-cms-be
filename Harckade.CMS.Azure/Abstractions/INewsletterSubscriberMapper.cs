namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterSubscriberMapper
    {
        Entities.NewsletterSubscriberEntity DomainToEntity(Domain.NewsletterSubscriber subscriber);
        Domain.NewsletterSubscriber EntityToDomain(Entities.NewsletterSubscriberEntity subscriber);
    }
}
