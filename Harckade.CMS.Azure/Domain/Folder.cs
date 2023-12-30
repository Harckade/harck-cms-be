using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Domain
{
    public class Folder
    {
        public string Name { get; private set; }
        public string ParentFolder { get; private set; }

        public Folder(FolderDto folder)
        {
            if (folder == null)
            {
                throw new System.ArgumentNullException(nameof(folder));
            }
            if (string.IsNullOrWhiteSpace(folder.Name))
            {
                throw new System.ArgumentException(nameof(folder.Name));
            }
            Name = folder.Name;
            ParentFolder = folder.ParentFolder;
        }
    }
}
