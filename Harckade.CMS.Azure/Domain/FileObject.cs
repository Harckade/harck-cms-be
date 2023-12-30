using Azure.Storage.Blobs.Models;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Utils;

namespace Harckade.CMS.Azure.Domain
{
    public class FileObject
    {
        public BlobId Id { get; private set; }
        public string Name { get; private set; }
        public FileType FileType { get; private set; }
        public DateTime? Timestamp { get; private set; }
        public long? Size { get; private set; }

        public bool IsLoaded { get; private set; }

        private void isValidId(BlobId id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
        }

        private void isValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Validations.ValidateFileName(name);
        }

        public FileObject(FileType fileType, string folder, BlobItem file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (fileType == FileType.Invalid)
            {
                throw new ArgumentException(nameof(fileType));
            }
            string typeName = Enum.GetName(typeof(FileType), fileType).ToLower();
            string folderPrefix = !string.IsNullOrWhiteSpace(folder) ? string.Concat(folder, '/') : string.Empty;
            var fileName = file.Name.Substring($"{folderPrefix}{typeName}".Length + 1);
            isValidName(fileName);
            var id = new BlobId($"{folderPrefix}{typeName}_{fileName}");
            Id = id;
            Name = fileName;
            Size = fileType == FileType.Folder ? 0 : file.Properties.ContentLength;
            Timestamp = file.Properties.CreatedOn.Value.UtcDateTime;
            FileType = fileType;
            IsLoaded = true;
        }

        public FileObject(BlobId blobId, BlobProperties properties)
        {
            isValidId(blobId);
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            var fileNameParts = blobId.ToString().Split('/');
            var fileTypeString = fileNameParts[fileNameParts.Length - 1].Split('_')[0];
            FileType fileType = (FileType)Enum.Parse(typeof(FileType), fileTypeString, true);
            if (fileType == FileType.Invalid)
            {
                throw new ArgumentException(nameof(fileType));
            }
            var fileName = fileNameParts[fileNameParts.Length - 1].Substring(fileTypeString.Length + 1);
            isValidName(fileName);
            Id = blobId;
            Name = fileName;
            Size = fileType == FileType.Folder ? 0 : properties.ContentLength;
            Timestamp = properties.CreatedOn.UtcDateTime;
            FileType = fileType;
            IsLoaded = true;
        }

        public FileObject(BlobId id)
        {
            isValidId(id);
            Id = id;
            IsLoaded = false;
        }
    }
}
