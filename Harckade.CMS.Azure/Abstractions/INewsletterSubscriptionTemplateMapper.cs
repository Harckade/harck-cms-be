namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterSubscriptionTemplateMapper
    {
        Entities.NewsletterSubscriptionTemplateEntity DomainToEntity(Domain.NewsletterSubscriptionTemplate confirmationEmailTemplate);
        Domain.NewsletterSubscriptionTemplate EntityToDomain(Entities.NewsletterSubscriptionTemplateEntity confirmationEmailTemplate);
   
    }
}
