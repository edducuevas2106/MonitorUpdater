using log4net;

namespace MonitorManagerTest
{
    public class FileManager
    {
        private static readonly ILog Log;
        #region Constants
        private const string UpdatePackageFileNameFormat = "{0}.zip";

        #endregion


        #region Methods

        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var fileUpdater = new FileUpdater();
            return fileUpdater.UpdateFiles(sourceFolder, targetFolder, backupDir);
        }

        public string RemoveDirectoryContents(string directory, string directoryToFilter = "", bool remove = false)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (!directoryInfo.Exists)
            {
                return string.Empty;
            }

            foreach (var file in directoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
            {
                try
                {
                    if (string.IsNullOrEmpty(directoryToFilter) || !file.FullName.Contains(@"\" + directoryToFilter + @"\"))
                    {
                        file.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("Algunos archivos no pudieron ser limpiados", ex);
                }
            }

            try
            {
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    if (string.IsNullOrEmpty(directoryToFilter) || (!dir.FullName.Contains(@"\" + directoryToFilter + @"\") && !dir.FullName.EndsWith(@"\" + directoryToFilter)))
                    {
                        dir.Delete(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Algunos directorios no pudieron ser limpiados", ex);
            }

            try
            {
                if (remove)
                {
                    directoryInfo.Refresh();
                    if (directoryInfo.GetDirectories().Length == 0)
                    {
                        directoryInfo.Delete(true);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error("Algunos directorios no pudieron ser limpiados", ex);
            }

            return string.Empty;
        }

        #endregion Methods
    }
}
