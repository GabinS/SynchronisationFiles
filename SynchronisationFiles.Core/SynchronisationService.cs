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
        /// <param name="SourceDirectoryPath">Dossier principale des fichiers à synchroniser.</param>
        /// <param name="TargetDirectoryPath">Dossier secondaire des fichiers à synchroniser.</param>
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

                _WatcherSourceDirectory = new FileSystemWatcher(_SourceDirectoryPath);
                _WatcherSourceDirectory.Created += _Watcher_Created;
                _WatcherSourceDirectory.Renamed += _Watcher_Renamed;
                _WatcherSourceDirectory.Deleted += _Watcher_Deleted;
                _WatcherSourceDirectory.Changed += _Watcher_Changed;
                _WatcherSourceDirectory.EnableRaisingEvents = true;

                if (_SynchronisationMethode != "OneWay")
                {
                    _WatcherTargetDirectory = new FileSystemWatcher(_TargetDirectoryPath);
                    _WatcherTargetDirectory.Created += _Watcher_Created;
                    _WatcherTargetDirectory.Renamed += _Watcher_Renamed;
                    _WatcherTargetDirectory.Deleted += _Watcher_Deleted;
                    _WatcherTargetDirectory.Changed += _Watcher_Changed;
                    _WatcherTargetDirectory.EnableRaisingEvents = true;
                }

                InitialiseSyncrhonisationFolder();
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

        #region Core fx

        /// <summary>
        /// Syncrhonise les deux répertoires en fonction de la méthode de synchronisation
        /// </summary>
        private void InitialiseSyncrhonisationFolder()
        {
            //TODO Initialisation
        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est créé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string targetDirectory = e.FullPath.Contains(this._SourceDirectoryPath) ? _TargetDirectoryPath : _SourceDirectoryPath;

            if (!File.Exists($"{targetDirectory}\\{e.Name}"))
            {
                Loggers.WriteInformation($"Fichier créé : {e.FullPath}");
                try
                {
                    InactivateWatchers();
                    Loggers.WriteInformation($"Copie du fichier '{e.Name}' vers {targetDirectory}");
                    File.Copy(e.FullPath, $"{targetDirectory}\\{e.Name}", true);
                    ActivateWatchers();
                }
                catch (Exception ex)
                {
                    Loggers.WriteError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est renommé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string targetDirectory = e.FullPath.Contains(this._SourceDirectoryPath) ? _TargetDirectoryPath : _SourceDirectoryPath;

            if (File.Exists($"{targetDirectory}\\{e.OldName}"))
            {
                Loggers.WriteInformation($"Fichier renommé: {e.FullPath}");
                try
                {
                    Loggers.WriteInformation($"Renomage du fichier '{e.OldName}' en '{e.Name}' dans {targetDirectory}");
                    File.Move($"{targetDirectory}\\{e.OldName}", $"{targetDirectory}\\{e.Name}");
                }
                catch (Exception ex)
                {
                    Loggers.WriteError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est supprimé dans le répertoire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string targetDirectory = e.FullPath.Contains(this._SourceDirectoryPath) ? _TargetDirectoryPath : _SourceDirectoryPath;

            if (File.Exists($"{targetDirectory}\\{e.Name}"))
            {
                Loggers.WriteInformation($"Fichier supprimé: {e.FullPath}");
                try
                {
                    Loggers.WriteInformation($"Suppression du fichier '{e.Name}' dans {targetDirectory}");
                    File.Delete($"{targetDirectory}\\{e.Name}");
                }
                catch (Exception ex)
                {
                    Loggers.WriteError(ex.ToString());
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
            string targetDirectory = e.FullPath.Contains(this._SourceDirectoryPath) ? _TargetDirectoryPath : _SourceDirectoryPath;

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


        /// <summary>
        /// Ouvre un fichier avec attente si le fichier n'est pas disponible.
        /// </summary>
        /// <param name="filePath">Chemin du fichier à ouvrir.</param>
        /// <returns>Flux du fichier ouvert.</returns>
        private FileStream OpenFileAndWaitIfNeeded(string filePath)
        {
            bool isFileBusy = true;
            FileStream fileStream = null;

            DateTime startDateTime = DateTime.Now;

            do
            {
                try
                {
                    fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    isFileBusy = false; //Si on arrive à ouvrir, le fichier est accessible
                }
                catch (IOException)
                {
                    //Si on a une erreur d'IO, c'est que le fichier est encore ouvert
                    System.Threading.Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erreur à l'ouverture du fichier", ex);
                }

                if (DateTime.Now > startDateTime.AddMinutes(15))
                {
                    throw new Exception("Délai d'attente dépassé, impossible d'ouvrir le fichier.");
                }

            } while (isFileBusy);

            return fileStream;
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

        #endregion

        #endregion

    }
}
