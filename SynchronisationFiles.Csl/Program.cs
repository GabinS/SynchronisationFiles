using SynchronisationFiles.Core;
using SynchronisationFiles.Diagnostics;
using System;
using System.Configuration;

namespace SynchronisationFiles.Csl
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Loggers.AvaillableLoggers.Add(new ConsoleLogger());

                string SourceDirectoryPath = "C:\\temp\\DossierPrincipale";
                string TargetDirectoryPath = "C:\\temp\\DossierSecondaire";
                string SynchronisationMethode = "TwoWaySourceWon"; //OneWay - TwoWaySourceWon - TwoWayTargetWon


                SynchronisationService service = new SynchronisationService(SourceDirectoryPath, TargetDirectoryPath, SynchronisationMethode);

                service.Start();

                Loggers.WriteInformation($"Ecoute du répertoire {SourceDirectoryPath}");
                Loggers.WriteInformation($"Ecoute du répertoire {TargetDirectoryPath}");
                Console.ReadKey();
                Loggers.WriteInformation($"Fin du programme.");

                service.Stop();
            }
            catch (Exception ex)
            {
                Loggers.WriteError(ex.ToString());
            }
        }
    }
}
