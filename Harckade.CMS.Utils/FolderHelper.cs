namespace Harckade.CMS.Utils
{
    public static class FolderHelper
    {
        public static string FolderPathToId(string folderPath, string folderTypeString)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath));
            }
            var folderParts = folderPath.Split('/');
            var folderId = $"{folderTypeString}_{folderPath}";
            if (folderParts.Length > 1)
            {
                folderId = "";
                for (var i = 0; i < folderParts.Length - 1; i++)
                {
                    folderId = $"{folderId}{(!string.IsNullOrWhiteSpace(folderId) ? "/" : "")}{folderParts[i]}";
                }
                folderId = $"{folderId}/{folderTypeString}_{folderParts[folderParts.Length - 1]}";
            }
            return folderId;
        }

        public static string FileIdToPath(string fileId, string fileTypeString)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }
            var fileParts = fileId.Split('/');
            var fileName = fileParts[fileParts.Length - 1].Substring(fileTypeString.Length + 1);
            return $"{String.Join('/', fileParts.Take(fileParts.Length - 1))}{(fileParts.Length > 1 ? "/" : "")}{fileName}";
        }
    }
}