using log4net;
using Microsoft.Web.XmlTransform;
using System.Diagnostics;

namespace MonitorManagerTest
{
    public class FileUpdater
    {
        private static readonly ILog Log;
        #region Constants
        private const string DeleteCommandExtension = ".del";
        private const string AddCommandExtension = ".add";
        private const string UpdateCommandExtension = ".upd";
        private const string XdtMergeCommandExtension = ".xmrg";
        private const string ExecuteCommandExtension = ".exc";
        private const string ExecuteCommandExtensionInitial = ".eini";
        private const string ExecuteCommandExtensionEnd = ".eend";
        private const string ExecuteCommandParamsExtension = ".params";
        private const string CannotTransformXdtMessage = "No se puede realizar la transformación del archivo .";
        #endregion


        #region Methods


        private static void BackupFile(string backupDir, string targetFolder, string originalFileName, string command)
        {
            var backupFileName = originalFileName;
            var targetRelativeDirectory = string.Empty;


            if (File.Exists(originalFileName))
            {
                if (command == DeleteCommandExtension)
                {
                    command = UpdateCommandExtension;
                    targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
                }

                if (command == ExecuteCommandExtensionInitial || command == ExecuteCommandExtension || command == ExecuteCommandExtensionEnd || command == ExecuteCommandParamsExtension)
                {
                    backupFileName = Path.Combine(Path.GetDirectoryName(originalFileName), Path.GetFileNameWithoutExtension(originalFileName));
                    targetRelativeDirectory = targetFolder;
                }
            }
            else if (command == DeleteCommandExtension)
            {
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }


                File.WriteAllText(originalFileName, string.Empty);

                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else if (command == UpdateCommandExtension)
            {

                command = DeleteCommandExtension;
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }


                File.WriteAllText(originalFileName, string.Empty);

                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else
            {
                return;
            }


            var folderToBackupFile = Path.Combine(backupDir, targetRelativeDirectory);

            if (!Directory.Exists(folderToBackupFile))
            {
                Directory.CreateDirectory(folderToBackupFile);
            }

            File.Copy(originalFileName, Path.Combine(folderToBackupFile, Path.GetFileName(backupFileName)) + command, true);
        }


        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);


            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }


        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var createBackup = !string.IsNullOrEmpty(backupDir);

            if (createBackup)
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
            }

            try
            {
                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionInitial, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir, targetRelativeDirectory), ExecuteCommandExtensionInitial);
                });

                foreach (var file in Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) != ExecuteCommandExtensionInitial && Path.GetExtension(s) != ExecuteCommandExtensionEnd).ToList())
                {
                    var fileExtension = Path.GetExtension(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    var targetFileName = Path.Combine(targetFolder, targetRelativeDirectory ?? string.Empty, fileName ?? string.Empty);

                    switch (fileExtension.ToLower())
                    {
                        case AddCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, DeleteCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case XdtMergeCommandExtension:
                            if (createBackup && File.Exists(targetFileName))
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.MergeXDT(file, targetFileName);
                            break;
                        case UpdateCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case DeleteCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, AddCommandExtension);
                            }

                            this.RemoveFile(targetFileName);
                            break;
                        case ExecuteCommandExtension:
                            this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtension);
                            break;
                    }
                }

                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionEnd, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtensionEnd);
                });
            }
            catch (Exception ex)
            {
                if (createBackup)
                {
                    var errorMessage = ex.ToString();
                    var rollbackError = this.UpdateFiles(backupDir, targetFolder);
                    return errorMessage + (string.IsNullOrEmpty(rollbackError) ? string.Empty : Environment.NewLine + "Rollback Error =>" + Environment.NewLine + rollbackError);
                }
                Log.Error("Error UpdateFiles: " + ex.Message);
                return ex.ToString();
            }
            return string.Empty;
        }

        private void ExecuteBats(string file, string fileName, string targetRelativeDirectory, bool createBackup, string backupDir, string extension)
        {
            // Comprobamos que el archivo .bat exista
            if (!File.Exists(file))
            {
                Log.Info("El archivo .bat no existe.");
                return;
            }

            // Creamos la ruta completa al archivo .bat
            string batPath = Path.Combine(targetRelativeDirectory, fileName);
            try
            {
                // Copiamos el archivo .bat si se requiere crear un respaldo
                if (createBackup)
                {
                    string backupFilePath = Path.Combine(backupDir, fileName + extension);
                    File.Copy(file, backupFilePath, true);
                    Log.Info($"Se ha creado un respaldo en: {backupFilePath}");
                }

                // Creamos el proceso para ejecutar el archivo .bat
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    WorkingDirectory = targetRelativeDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();

                    // Capturamos la salida estándar y de errores
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // Esperamos a que el proceso termine

                    int exitCode = process.ExitCode; // Código de salida

                    // Mostramos la salida y los errores
                    Log.Info($"Salida:{output}");
                    Log.Info($"Errores:{error}");
                    Log.Info("Código de salida: " + exitCode);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al ejecutar el archivo .bat: " + ex.Message);
            }
        }


        private void CopyFile(string sourceFile, string targetFile)
        {
            try
            {
                // Verificamos si el archivo fuente existe
                if (!File.Exists(sourceFile))
                {
                    Log.Info("El archivo fuente no existe.");
                    return;
                }

                // Copiamos el archivo al destino
                File.Copy(sourceFile, targetFile, true); // El tercer parámetro es para sobrescribir si el archivo de destino ya existe
                Log.Info($"Archivo copiado de {sourceFile} a {targetFile}.");
            }
            catch (Exception ex)
            {
               Log.Error("Error al copiar el archivo: " + ex.Message);
            }
        }

        private void RemoveFile(string targetFile)
        {
            try
            {
                // Verificamos si el archivo existe antes de intentar eliminarlo
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                    Log.Info($"Archivo {targetFile} eliminado correctamente.");
                }
                else
                {
                    Log.Info($"El archivo {targetFile} no existe.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al eliminar el archivo: " + ex.Message);
            }
        }


        private void MergeXDT(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                using (var target = new XmlTransformableDocument())
                {
                    target.PreserveWhitespace = true;
                    target.Load(targetFile);

                    using (var xdt = new XmlTransformation(sourceFile))
                    {
                        if (xdt.Apply(target))
                        {
                            target.Save(targetFile);
                        }
                        else
                        {
                            throw new XmlTransformationException(string.Format(CannotTransformXdtMessage, sourceFile));
                        }
                    }
                }
            }
        }

        #endregion
    }
}
