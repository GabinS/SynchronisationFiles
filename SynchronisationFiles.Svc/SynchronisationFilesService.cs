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

            Loggers.AvaillableLoggers.Add(new ConsoleLogger() { IsInformationBypassed = true });
        }

        #endregion


        #region Methods

        protected override void OnStart(string[] args)
        {
            try
            {
                string inputDir = ConfigurationManager.AppSettings["InputDirectoryPath"]; ;
                string outputDir = ConfigurationManager.AppSettings["OutputDirectoryPath"];

                Loggers.WriteInformation(
                $"Démarrage du service{Environment.NewLine}input = {inputDir}{Environment.NewLine}output = {outputDir}");

                _Service = new SynchronisationService(inputDir, outputDir);

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
        }

        #endregion
    }
}
