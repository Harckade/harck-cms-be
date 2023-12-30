using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class Settings
    {
        public IEnumerable<Language> Languages { get; private set; }
        public Language DefaultLanguage { get; private set; }
        public DateTime LastDeploymentDate { get; private set; }
        public bool RequiresDeployment { get; private set; }

        private void setDefaultLanguage(string defaultLanguage)
        {
            if (string.IsNullOrWhiteSpace(defaultLanguage))
            {
                throw new ArgumentNullException(nameof(defaultLanguage));
            }

            Language language;
            if (!Enum.TryParse(defaultLanguage, true, out language))
            {
                throw new InvalidCastException(nameof(defaultLanguage));
            }
            DefaultLanguage = language;
        }

        private void setLanguages(IEnumerable<string> languages)
        {
            if (!languages.Any() || languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }
            Languages = languages.Select(lang =>
            {
                Language l;
                if (!Enum.TryParse(lang, true, out l))
                {
                    throw new InvalidCastException(nameof(lang));
                }
                return l;
            });
        }

        public Settings(SettingsDto settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            setDefaultLanguage(settings.DefaultLanguage);
            setLanguages(settings.Languages);
        }

        public Settings(SettingsEntity settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var languages = settings == null ? new List<string>() : JsonConvert.DeserializeObject<IEnumerable<string>>(settings.Languages);
            var defaultLanguage = settings == null || settings.DefaultLanguage == null ? string.Empty : JsonConvert.DeserializeObject<string>(settings.DefaultLanguage);
            setDefaultLanguage(defaultLanguage);
            setLanguages(languages);
            LastDeploymentDate = settings.LastDeploymentDate;
            RequiresDeployment = settings.RequiresDeployment;
        }

        public void UpdateDefaultLanguage(Language newDefaultLanguage, IEnumerable<Language> languages = null)
        {
            if (languages != null && languages.Any())
            {
                Languages = languages;
            }
            if (newDefaultLanguage == Language.None)
            {
                throw new ArgumentNullException(nameof(newDefaultLanguage));
            }
            if (!Languages.Contains(newDefaultLanguage))
            {
                throw new InvalidOperationException("Choose a default language within the range of languages you have defined.");
            }
            DefaultLanguage = newDefaultLanguage;
        }

        public void UpdateDeploymentInfo(bool requiresDeployment = true)
        {
            if (RequiresDeployment == true && requiresDeployment == false)
            {
                LastDeploymentDate = DateTime.UtcNow.ToUniversalTime();
            }
            RequiresDeployment = requiresDeployment;
        }
    }
}
