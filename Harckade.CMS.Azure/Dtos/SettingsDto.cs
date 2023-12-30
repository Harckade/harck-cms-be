namespace Harckade.CMS.Azure.Dtos
{
    public class SettingsDto
    {
        public IEnumerable<string> Languages { get; set; }
        public string DefaultLanguage { get; set; }
        public DateTime? LastDeploymentDate { get; set; }
        public bool RequiresDeployment { get; set; }
    }
}
