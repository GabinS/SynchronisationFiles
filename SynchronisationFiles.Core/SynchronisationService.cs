using SynchronisationFiles.Diagnostics;
using System;
using System.IO;
using System.Linq;

namespace SynchronisationFiles.Core
{
    public class SynchronisationService
    {
        #region Fields

        /// <summary>
        /// Détermine si le service est en cours d'exécution.
        /// </summary>
        private bool _IsRunning;
        /// <summary>
        /// Détermine si le service est en pause.
        /// </summary>
        private bool _IsPaused;
        /// <summary>
        /// Premier  dossier à synchroniser.
        /// </summary>
        private string _SourceDirectoryPath;
        /// <summary>
        /// Second dossier à synchroniser.
        /// </summary>
        private string _TargetDirectoryPath;
        /// <summary>
        /// Methode de synchronisation
        /// </summary>
        private string _SynchronisationMethode;
        /// <summary>
        /// Ecouteur du répertoire principale.
        /// </summary>
        private FileSystemWatcher _WatcherSourceDirectory;
        /// <summary>
        /// Ecouteur du répertoire secondaire.
        /// </summary>
        private FileSystemWatcher _WatcherTargetDirectory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="SynchronisationService"/>.
        /// </summary>
        /// <param name="SourceDirectoryPath">Dossier source des fichiers à synchroniser.</param>
        /// <param name="TargetDirectoryPath">Dossier cible des fichiers à synchroniser.</param>
        public SynchronisationService(string SourceDirectoryPath, string TargetDirectoryPath, string SynchronisationMethode)
        {

            _SynchronisationMethode = SynchronisationMethode;

            switch (_SynchronisationMethode)
            {
                case "TwoWaySourceWon":
                    _SourceDirectoryPath = SourceDirectoryPath;
                    _TargetDirectoryPath = TargetDirectoryPath;
                    break;

                case "TwoWayTargetWon":
                    _SourceDirectoryPath = TargetDirectoryPath;
                    _TargetDirectoryPath = SourceDirectoryPath;
                    break;
                default:
                    _SourceDirectoryPath = SourceDirectoryPath;
                    _TargetDirectoryPath = TargetDirectoryPath;
                    break;
            }

            try
            {
                Directory.CreateDirectory(_SourceDirectoryPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de créer / ouvrir le dossier source" + _SourceDirectoryPath, ex);
            }

            try
            {
                Directory.CreateDirectory(_TargetDirectoryPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de créer / ouvrir le dossier cible" + _TargetDirectoryPath, ex);
            }
        }

        #endregion

        #region Methods

        #region Service management

        /// <summary>
        /// Démarre le service.
        /// </summary>
        public void Start()
        {
            if (!_IsRunning)
            {
                _IsRunning = true;

                InitialiseSyncrhonisationDirectories();

                _WatcherSourceDirectory = new FileSystemWatcher(_SourceDirectoryPath);
                _WatcherSourceDirectory.Created += _Watcher_Created;
                _WatcherSourceDirectory.Renamed += _Watcher_Renamed;
                _WatcherSourceDirectory.Deleted += _Watcher_Deleted;
                _WatcherSourceDirectory.Changed += _Watcher_Changed;
                _WatcherSourceDirectory.IncludeSubdirectories = true;
                _WatcherSourceDirectory.EnableRaisingEvents = true;

                if (_SynchronisationMethode != "OneWay")
                {
                    _WatcherTargetDirectory = new FileSystemWatcher(_TargetDirectoryPath);
                    _WatcherTargetDirectory.Created += _Watcher_Created;
                    _WatcherTargetDirectory.Renamed += _Watcher_Renamed;
                    _WatcherTargetDirectory.Deleted += _Watcher_Deleted;
                    _WatcherTargetDirectory.Changed += _Watcher_Changed;
                    _WatcherTargetDirectory.IncludeSubdirectories = true;
                    _WatcherTargetDirectory.EnableRaisingEvents = true;
                }
            }
        }

        /// <summary>
        /// Arrête le service.
        /// </summary>
        public void Stop()
        {
            if (_IsRunning)
            {
                _WatcherSourceDirectory.Dispose();
                _WatcherSourceDirectory = null;

                _WatcherTargetDirectory.Dispose();
                _WatcherTargetDirectory = null;

                _IsRunning = false;
                _IsPaused = false;
            }
        }

        /// <summary>
        /// Met le service en pause.
        /// </summary>
        public void Pause()
        {
            if (_IsRunning && !_IsPaused)
            {
                _IsPaused = true;
                _WatcherSourceDirectory.EnableRaisingEvents = false;
                _WatcherTargetDirectory.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Reprend le service.
        /// </summary>
        public void Resume()
        {
            if (_IsRunning && _IsPaused)
            {
                _WatcherSourceDirectory.EnableRaisingEvents = true;
                _WatcherTargetDirectory.EnableRaisingEvents = true;
                _IsPaused = false;
            }
        }

        #endregion

        #region Core

        /// <summary>
        /// Syncrhonise les deux répertoires en fonction de la méthode de synchronisation
        /// </summary>
        private void InitialiseSyncrhonisationDirectories()
        {
            SyncrhonisationDirectories(_SourceDirectoryPath, _TargetDirectoryPath);
            if (_SynchronisationMethode != "OneWay")
            {
                SyncrhonisationDirectories(_TargetDirectoryPath, _SourceDirectoryPath);
                Loggers.WriteInformation($"Synchronisation entre les répertoires '{_SourceDirectoryPath}' et '{_TargetDirectoryPath}' terminée");
            }
            else
            {
                Loggers.WriteInformation($"Synchronisation du répertoire '{_SourceDirectoryPath}' vers '{_TargetDirectoryPath}' terminée");
            }
        }

        /// <summary>
        /// Synchronise deux répertoires donnés
        /// </summary>
        /// <param name="path1">Chemin source</param>
        /// <param name="path2">Chemin cible</param>
        private void SyncrhonisationDirectories(string path1, string path2)
        {
            DirectoryInfo directory = new DirectoryInfo(path1);

            directory.GetFiles().ToList().ForEach(f => {
                if (File.Exists($"{path2}\\{f.Name}"))
                {
                    if (!FileEquals(f.FullName, $"{path2}\\{f.Name}"))
                    {
                        Loggers.WriteInformation($"Modification du fichier '{f.Name}' dans {path2}");
                        File.Delete($"{path2}\\{f.Name}");
                        File.Copy(f.FullName, $"{path2}\\{f.Name}");
                    }
                }
                else
                {
                    Loggers.WriteInformation($"Copie du fichier '{f.Name}' vers {path2}");
                    File.Copy(f.FullName, $"{path2}\\{f.Name}");
                }
            });

            directory.GetDirectories().ToList().ForEach(d => {
                string targetDirectoryPath = d.FullName.Replace(path1, path2);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    Loggers.WriteInformation($"Création du dossier '{d.Name}' dans {path2}");
                    Directory.CreateDirectory(targetDirectoryPath);
                }
                SyncrhonisationDirectories(d.FullName, targetDirectoryPath);
            });
        }

        /// <summary>
        /// Compare le contenue de 2 fichiers 
        /// </summary>
        /// <param name="path1">chemin du premier fichier à comparer</param>
        /// <param name="path2">chemin du deuxième fichier à comparer</param>
        /// <returns>true si les 2 fichier sont identique</returns>
        private bool FileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Active l'écoute des répertoires afin d'activer les événements
        /// </summary>
        private void ActivateWatchers()
        {
            if(!_WatcherSourceDirectory.EnableRaisingEvents) { _WatcherSourceDirectory.EnableRaisingEvents = true; }
            if (!_WatcherTargetDirectory.EnableRaisingEvents) { _WatcherTargetDirectory.EnableRaisingEvents = true; }
        }

        /// <summary>
        /// Inactive l'écoute des répertoires afin d'activer les événements
        /// </summary>
        private void InactivateWatchers()
        {
            if (_WatcherSourceDirectory.EnableRaisingEvents) { _WatcherSourceDirectory.EnableRaisingEvents = false; }
            if (_WatcherTargetDirectory.EnableRaisingEvents) { _WatcherTargetDirectory.EnableRaisingEvents = false; }
        }

        /// <summary>
        /// Retourne le chemin complet du repertoire ciblé
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        /// <returns>Chemin complet du répertoire cible</returns>
        private string GetCurrentTagetDirectory(object sender, RenamedEventArgs e)
        {
            string path = e.FullPath.Replace($"\\{e.Name}", "");
            return sender == _WatcherSourceDirectory ? path.Replace(_SourceDirectoryPath, _TargetDirectoryPath) : path.Replace(_TargetDirectoryPath, _SourceDirectoryPath);
        }

        /// <summary>
        /// Retourne le chemin complet du repertoire ciblé
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        /// <returns>Chemin complet du répertoire cible</returns>
        private string GetCurrentTagetDirectory(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath.Replace($"\\{e.Name}", "");
            return sender == _WatcherSourceDirectory ? path.Replace(_SourceDirectoryPath, _TargetDirectoryPath) : path.Replace(_TargetDirectoryPath, _SourceDirectoryPath);
        }

        #region Events

        /// <summary>
        /// Méthode appelée lorsqu'un fichier ou dossier est créé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string targetDirectory = GetCurrentTagetDirectory(sender, e);

            //Si c'est un dossier
            if (Path.GetExtension(e.FullPath) == String.Empty)
            {
                if (!Directory.Exists($"{targetDirectory}\\{e.Name}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Dossier créé : {e.FullPath}");
                        InactivateWatchers();
                        Loggers.WriteInformation($"Copie du dossier '{e.Name}' vers {targetDirectory}");
                        Directory.CreateDirectory($"{targetDirectory}\\{e.Name}");
                        ActivateWatchers();

                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }
            else
            {
                if (!File.Exists($"{targetDirectory}\\{e.Name}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Fichier créé : {e.FullPath}");
                        InactivateWatchers();
                        Loggers.WriteInformation($"Copie du fichier '{e.Name}' vers {targetDirectory}");
                        File.Copy(e.FullPath, $"{targetDirectory}\\{e.Name}");
                        ActivateWatchers();
                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }

        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier ou dossier est renommé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string targetDirectory = GetCurrentTagetDirectory(sender, e);

            //Si c'est un dossier
            if (Path.GetExtension(e.FullPath) == String.Empty)
            {
                if (Directory.Exists($"{targetDirectory}\\{e.OldName}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Dossier renommé: {e.FullPath}");
                        Loggers.WriteInformation($"Renomage du dossier '{e.OldName}' en '{e.Name}' dans {targetDirectory}");
                        Directory.Move($"{targetDirectory}\\{e.OldName}", $"{targetDirectory}\\{e.Name}");
                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }
            else
            {
                if (File.Exists($"{targetDirectory}\\{e.OldName}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Fichier renommé: {e.FullPath}");
                        Loggers.WriteInformation($"Renomage du fichier '{e.OldName}' en '{e.Name}' dans {targetDirectory}");
                        File.Move($"{targetDirectory}\\{e.OldName}", $"{targetDirectory}\\{e.Name}");
                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }

        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier ou dossier est supprimé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string targetDirectory = GetCurrentTagetDirectory(sender, e);

            //Si c'est un dossier
            if (Path.GetExtension(e.FullPath) == String.Empty)
            {
                if (Directory.Exists($"{targetDirectory}\\{e.Name}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Dossier supprimé: {e.FullPath}");
                        Loggers.WriteInformation($"Suppression du dossier '{e.Name}' dans {targetDirectory}");
                        DirectoryInfo directory = new DirectoryInfo($"{targetDirectory}\\{e.Name}");

                        //Vide le contenue du dossier target avant de le supprimer
                        directory.GetFiles().ToList().ForEach(f => f.Delete());
                        directory.GetDirectories().ToList().ForEach(d => d.Delete(true));

                        Directory.Delete($"{targetDirectory}\\{e.Name}");

                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }
            else
            {
                if (File.Exists($"{targetDirectory}\\{e.Name}"))
                {
                    try
                    {
                        Loggers.WriteInformation($"Fichier supprimé: {e.FullPath}");
                        Loggers.WriteInformation($"Suppression du fichier '{e.Name}' dans {targetDirectory}");
                        File.Delete($"{targetDirectory}\\{e.Name}");
                    }
                    catch (Exception ex)
                    {
                        Loggers.WriteError(ex.ToString());
                    }
                }
            }


        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est modifier dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string targetDirectory = GetCurrentTagetDirectory(sender, e);

            if (File.Exists($"{targetDirectory}\\{e.Name}"))
            {
                Loggers.WriteInformation($"Fichier modifié: {e.FullPath}");
                try
                {
                    InactivateWatchers();
                    Loggers.WriteInformation($"Modification du fichier '{e.Name}' dans {targetDirectory}");
                    File.Delete($"{targetDirectory}\\{e.Name}");
                    File.Copy(e.FullPath, $"{targetDirectory}\\{e.Name}");
                    ActivateWatchers();
                }
                catch (Exception ex)
                {
                    Loggers.WriteError(ex.ToString());
                }
            }
        }

        #endregion

        #endregion

        #endregion

    }
}
