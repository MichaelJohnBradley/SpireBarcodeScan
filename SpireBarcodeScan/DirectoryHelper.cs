using System;
using System.IO;
using System.Linq;

namespace SpireBarcodeScan
{
    public static class DirectoryHelper
    {
        /// <summary>
        /// Get all files with matching extension in the designated folder/subfolders
        /// </summary>                     
        public static string[] GetFiles(string folder, bool searchSubFolders, string extension,int? numberToFetch)
        {
            if (numberToFetch == null)
            {
                return Directory.GetFiles(folder, extension, searchSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            var d = new DirectoryInfo(folder);
            return d.GetFiles(extension, searchSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).OrderBy(x => x.CreationTimeUtc).Take(numberToFetch.Value).Select(x => x.FullName).ToArray();
        }
        /// <summary>
        /// Move a file from one folder to another
        /// </summary>       
        public static bool MoveFile(string oldLocation, string newLocation)
        {
            var moved = false;
            var newPath = Path.GetDirectoryName(newLocation);
            if (newPath != null)
            {
                Directory.CreateDirectory(newPath);
            }

            if (!File.Exists(newLocation))
            {
                File.Move(oldLocation, newLocation);
                moved = true;
            }
            else
            {
                //file already exists                
            }
            return moved;
        }
        /// <summary>
        /// Create a string to be used to help naming folders
        /// </summary>       
        public static string CreateFolderString(string type)
        {
            var folderString = "NewFolder";
            if (type == "date")
            {
                var now = DateTime.UtcNow;
                folderString = $"{now:yyyyMMdd}";
            }
            return folderString;
        }

        public static DirectoryInfo CreateFolder(string location)
        {
            try
            {
                var info = Directory.CreateDirectory(location);
                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create the folder: {location}");
                return null;
            }
        }
    }
}
