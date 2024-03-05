using log4net;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace MonitorManagerTest
{
    public static class MonitorUpdaterManager
    {
        #region Constantes
        private const string MonitorServiceNameKey = "monitorsk";
        public const string UpdaterMonitorInstallationFolder = "monSelfUpdater";
        const string MonitorUpdatesPath = "\\tmp";
        public const string UpdaterMonitorFolder = "actualizaciones";
        public static string InstalledRollbackFilesPath = "\\tmp";
        private static ILog Log;
        #endregion
        #region Metodos

        #region Inicializa Logger
        public static void InitializeLogger()
        {
            if (log4net.LogManager.GetCurrentLoggers().Length == 0)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Logs";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string configFile = AppDomain.CurrentDomain.BaseDirectory.ToString() + "log4net.config";
                log4net.Config.XmlConfigurator.Configure(new FileInfo(configFile));
            }
            Log = log4net.LogManager.GetLogger(typeof(Program));
            Log.Info("Begin processing.");
        }
        #endregion

        public static void UpdateMonitor(string monitorFilesLocation, string installationFolder, string version)
        {
            try
            {
                Log.Info("Iniciando las actualizaciones al monitor...");
                try
                {
                    // Cerrar Google Chrome antes de actualizar
                    Process[] processes = Process.GetProcessesByName("chrome");
                    if (processes.Any())
                    {
                        Log.Info("Cerrando el monitor de actualizaciones...");
                        foreach (var proc in processes)
                        {
                            proc.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Ocurrió un error al intentar terminar el proceso del monitor de actualizaciones.", ex);
                }

                // Directorio de respaldo para los archivos
                var backupPath = System.IO.Path.Combine(MonitorUpdatesPath, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                if (!System.IO.Directory.Exists(backupPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(backupPath);
                    }
                    catch
                    {
                        backupPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), UpdaterMonitorFolder, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                        if (!System.IO.Directory.Exists(backupPath))
                        {
                            System.IO.Directory.CreateDirectory(backupPath);
                        }
                    }
                }

                // Actualización de los archivos
                var fileManager = new FileManager();
                var result = fileManager.UpdateFiles(monitorFilesLocation.Trim(new char[] { '"' }), installationFolder.Trim(new char[] { '"' }), backupPath);
                bool updateError = false;

                if (!string.IsNullOrEmpty(result))
                {
                    updateError = true;
                    Log.Error(result);
                    result = null;
                }

                // Realizar rollback en caso de error
                if (updateError)
                {
                    // Log.Info("Realizando rollback de las actualizaciones al monitor...");
                    result = fileManager.UpdateFiles(backupPath, installationFolder);
                    fileManager.RemoveDirectoryContents(backupPath);

                    if (!string.IsNullOrEmpty(result))
                    {
                        Log.Info($"MonitorUpdater : {result}");
                    }
                    else
                    {
                        Log.Info("Terminado rollback de las actualizaciones al monitor...");
                    }
                    return;
                }

                //Eliminar archivos y directorios temporales
                fileManager.RemoveDirectoryContents(backupPath);
                fileManager.RemoveDirectoryContents(monitorFilesLocation.Trim(new char[] { '"' }));
                System.IO.Directory.Delete(backupPath, true);
                System.IO.Directory.Delete(monitorFilesLocation.Trim(new char[] { '"' }), true);

                ReleaseUpdateMonitorTask();
                Log.Info("Actualizaciones al monitor terminadas...");

            }
            catch (Exception ex)
            {
                Log.Error("Ocurrió un error durante el proceso de actualización del Monitor.", ex);
            }
        }

        private static void ReleaseUpdateMonitorTask()
        {
            try
            {
                // Cerrar Google Chrome antes de actualizar
                Process[] processes = Process.GetProcessesByName("chrome");
                if (processes.Any())
                {
                    Log.Info("Cerrando el monitor de chrome...");
                    foreach (var proc in processes)
                    {
                        proc.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Ocurrió un error al intentar terminar el proceso del monitor de actualizaciones.", ex);
            }
        }

        #endregion
    }
}
