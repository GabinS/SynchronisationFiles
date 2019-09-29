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
        /// Dossier d'entrée des images à redimensionner.
        /// </summary>
        private string _FirstDirectoryPath;
        /// <summary>
        /// Dossier dans lequel sauvegarder les images redimensionnées.
        /// </summary>
        private string _SecondDirectoryPath;
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
        /// <param name="FirstDirectoryPath">Dossier principale des fichiers à synchroniser.</param>
        /// <param name="SecondDirectoryPath">Dossier secondaire des fichiers à synchroniser.</param>
        public SynchronisationService(string FirstDirectoryPath, string SecondDirectoryPath)
        {
            _FirstDirectoryPath = FirstDirectoryPath;
            _SecondDirectoryPath = SecondDirectoryPath;

            try
            {
                Directory.CreateDirectory(_FirstDirectoryPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de créer / ouvrir le dossier principale" + _FirstDirectoryPath);
            }

            try
            {
                Directory.CreateDirectory(_SecondDirectoryPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de créer / ouvrir le dossier secondaire", ex);
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

                _WatcherFirstDirectory = new FileSystemWatcher(_FirstDirectoryPath);
                _WatcherFirstDirectory.Created += _Watcher_Created_First;
                _WatcherFirstDirectory.EnableRaisingEvents = true;

                _WatcherSecondDirectory = new FileSystemWatcher(_SecondDirectoryPath);
                _WatcherSecondDirectory.Created += _Watcher_Created_Second;
                _WatcherSecondDirectory.EnableRaisingEvents = true;
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
        /// Méthode appelée lorsqu'un fichier est créé dans le répertoire principale.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created_First(object sender, FileSystemEventArgs e)
        {
            Loggers.WriteInformation("Nouveau fichier : " + e.FullPath);
            try
            {
                // TODO Synchronisation des fichiers

                //Copy
                Loggers.WriteInformation("Copi du fichier vers " + _SecondDirectoryPath);
                File.Copy(e.FullPath, _SecondDirectoryPath +"\\"+ e.Name, true);
            }
            catch (Exception ex)
            {
                Loggers.WriteError(ex.ToString());
            }
        }

        /// <summary>
        /// Méthode appelée lorsqu'un fichier est créé dans le répertoire secondaire.
        /// </summary>
        /// <param name="sender">Instance qui a déclenchée l'événement.</param>
        /// <param name="e">Argument des événements.</param>
        private void _Watcher_Created_Second(object sender, FileSystemEventArgs e)
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
