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
        private FileSystemWatcher _WatcherFirstDirectory;
        /// <summary>
        /// Ecouteur du répertoire secondaire.
        /// </summary>
        private FileSystemWatcher _WatcherSecondDirectory;

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

                _WatcherFirstDirectory = new FileSystemWatcher(_SourceDirectoryPath);
                _WatcherFirstDirectory.Created += _Watcher_Created_Source;
                _WatcherFirstDirectory.EnableRaisingEvents = true;

                if (_SynchronisationMethode != "OneWay")
                {
                    _WatcherSecondDirectory = new FileSystemWatcher(_TargetDirectoryPath);
                    _WatcherSecondDirectory.Created += _Watcher_Created_Target;
                    _WatcherSecondDirectory.EnableRaisingEvents = true;
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
                _WatcherFirstDirectory.Dispose();
                _WatcherFirstDirectory = null;

                _WatcherSecondDirectory.Dispose();
                _WatcherSecondDirectory = null;

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
                _WatcherFirstDirectory.EnableRaisingEvents = false;
                _WatcherSecondDirectory.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Reprend le service.
        /// </summary>
        public void Resume()
        {
            if (_IsRunning && _IsPaused)
            {
                _WatcherFirstDirectory.EnableRaisingEvents = true;
                _WatcherSecondDirectory.EnableRaisingEvents = true;
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

        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est créé dans le répertoire source.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created_Source(object sender, FileSystemEventArgs e)
        {
            Loggers.WriteInformation("Nouveau fichier : " + e.FullPath);
            try
            {
                // TODO Synchronisation des fichiers

                //Copy
                Loggers.WriteInformation("Copi du fichier vers " + _TargetDirectoryPath);
                File.Copy(e.FullPath, _TargetDirectoryPath +"\\"+ e.Name, true);
            }
            catch (Exception ex)
            {
                Loggers.WriteError(ex.ToString());
            }
        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est créé dans le répertoire cible.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created_Target(object sender, FileSystemEventArgs e)
        {
            Loggers.WriteInformation("Nouveau fichier : " + e.FullPath);
            try
            {
                // TODO Synchronisation des fichiers
            }
            catch (Exception ex)
            {
                Loggers.WriteError(ex.ToString());
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

        #endregion

        #endregion

    }
}
