namespace Harckade.CMS.Azure.Entities
{
    public class SettingsEntity : GenericEntity
    {
        public string Languages { get; set; }
        public string DefaultLanguage { get; set; }
        public DateTime LastDeploymentDate { get; set; }
        public bool RequiresDeployment { get; set; }
    }
}
