namespace Harckade.CMS.Utils
{
    public static class MicrosoftGraph
    {
        public static string PrincipalNameToEmail(string principalName)
        {
            if (string.IsNullOrWhiteSpace(principalName) || !principalName.Contains('#'))
            {
                return string.Empty;
            }
            var auxString = principalName.Split('#')[0];
            var indexOfLastUnderScore = auxString.LastIndexOf('_');
            var auxArray = auxString.ToArray();
            auxArray[indexOfLastUnderScore] = '@';
            return new string(auxArray);
        }
    }
}
