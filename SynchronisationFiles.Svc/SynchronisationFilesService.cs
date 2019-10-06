using SynchronisationFiles.Core;
using SynchronisationFiles.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SynchronisationFiles.Svc
{
    public partial class SynchronisationFilesService : ServiceBase
    {
        #region Fields

        SynchronisationService _Service;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialies une nouvelle instance de la classe <see cref="ImgService"/>.
        /// </summary>
        public SynchronisationFilesService()
        {
            InitializeComponent();
            Loggers.AvaillableLoggers.Add(new FileLogger() { Source = @"C:\temp\log.txt"});
            //Loggers.AvaillableLoggers.Add(new EventLogger());
        }

        #endregion


        #region Methods

        protected override void OnStart(string[] args)
        {

            try
            {
                string sourceDirectoryPath = ConfigurationManager.AppSettings["SourceDirectoryPath"];
                string targetDirectoryPath = ConfigurationManager.AppSettings["TargetDirectoryPath"];
                string synchronisationMethode = ConfigurationManager.AppSettings["SynchronisationMethode"];

                Loggers.WriteInformation(
                $"Démarrage du service{Environment.NewLine}Dossier source = {sourceDirectoryPath}{Environment.NewLine}Dossier cible = {targetDirectoryPath}");

                _Service = new SynchronisationService(sourceDirectoryPath, targetDirectoryPath, synchronisationMethode);

                _Service.Start();
            }
            catch (Exception ex)
            {
                Loggers.WriteError(ex.ToString());
                // On relance l'exception pour que le gestionnaire de service ne finalise pas le démarrage du service.
                throw new Exception("Erreur au démarrage du service.", ex);
            }
        }

        protected override void OnStop()
        {
            Loggers.WriteInformation($"Arrêt du service");
            _Service?.Stop();
        }
        protected override void OnPause()
        {
            Loggers.WriteInformation($"Mise en pause du service");
            _Service?.Pause();
        }
        protected override void OnContinue()
        {
            Loggers.WriteInformation($"Reprise du service");
            _Service?.Resume();
        }

        #endregion
    }
}
