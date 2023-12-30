using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Dtos
{
    public class ArticleDto
    {
        public Guid Id { get; set; }
        public Dictionary<Language, string> Name { get; set; }
        public Dictionary<Language, string> NameNoDiacritics { get; set; }
        public Dictionary<Language, string> Author { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<Language, string> Description { get; set; }
        public Dictionary<Language, string> ImageUrl { get; set; }
        public Dictionary<Language, string> ImageDescription { get; set; }
        public DateTime PublishDate { get; set; }
        public bool Published { get; set; }
        public Dictionary<Language, IEnumerable<string>> Tags { get; set; }
        public bool MarkedAsDeleted { get; set; }
        public DateTime MarkedAsDeletedDate { get; set; }
        public Dictionary<Language, string> HtmlContent { get; set; }
        public bool HtmlContentIsLoaded { get; set; }

        private IList<Language> cyrillicLanguages = new List<Language>() {
            Language.Ru, Language.Uk, Language.Be, Language.Bg, Language.Kk, Language.Ky, Language.Mk, Language.Mn, Language.Sr, Language.Tg, Language.Uz, Language.Ab, Language.Av, Language.Ce, Language.Cv, Language.Os, Language.Tt
        };
        public void InitNoDiacritics()
        {
            Dictionary<Language, string> noDiacriticsName = new Dictionary<Language, string>();
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                var name = !Name.ContainsKey(lang) ? string.Empty : Name[lang];
                var title = cyrillicLanguages.Contains(lang) ? NickBuhro.Translit.Transliteration.CyrillicToLatin(name) : name;
                noDiacriticsName.Add(lang, Diacritics.Extensions.StringExtensions.RemoveDiacritics(title.ToLowerInvariant().Trim()).Replace(" ", "-").Replace("`", "").Replace("?", "").Replace("!", "").Replace("&", "and").Replace(":", "").Replace(".", ""));
            }
            NameNoDiacritics = noDiacriticsName;
        }
    }
}
