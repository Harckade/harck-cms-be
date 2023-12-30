using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using System.Security.Cryptography;

namespace Harckade.CMS.Azure.Domain
{
    public class NewsletterSubscriber
    {
        public Guid Id { get; private set; }
        public string EmailAddress { get; private set; }
        public Language Language { get; private set; }
        public string PersonalToken { get; private set; }
        public bool Confirmed { get; private set; }

        public DateTime SubscriptionDate { get; set; }

        private int GenerateRandomNumber()
        {
            int minValue = 10000000;
            int maxValue = 99999999;
            DateTime currentDate = DateTime.Now;
            int seed = currentDate.GetHashCode();

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] seedBytes = BitConverter.GetBytes(seed);
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);

                int generatedValue = BitConverter.ToInt32(seedBytes, 0) ^ BitConverter.ToInt32(randomBytes, 0);
                return Math.Abs(generatedValue % (maxValue - minValue + 1)) + minValue;
            }
        }

        public NewsletterSubscriber(NewsletterSubscriberEntity newsletterSubscriberEntity)
        {
            Id = Guid.ParseExact(newsletterSubscriberEntity.RowKey, "N");
            EmailAddress = newsletterSubscriberEntity.EmailAddress;
            Language = (Language)Enum.Parse(typeof(Language), newsletterSubscriberEntity.Language);
            PersonalToken = newsletterSubscriberEntity.PersonalToken;
            SubscriptionDate = newsletterSubscriberEntity.SubscriptionDate;
            Confirmed = newsletterSubscriberEntity.Confirmed;
        }

        public NewsletterSubscriber(string emailAddress, Language language)
        {
            Id = Guid.NewGuid();
            PersonalToken = Utils.Hash.Sha512($"{Id}{GenerateRandomNumber()}");
            EmailAddress = emailAddress;
            Language = language;
            SubscriptionDate = DateTime.UtcNow;
            Confirmed = false;
        }

        public void UpdateConfirmed()
        {
            Confirmed = true;
        }
    }
}
