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

                string firstDir = "C:\\temp\\Imaging\\Input";
                string secondDir = "C:\\temp\\Imaging\\Output";
                SynchronisationService service = new SynchronisationService(firstDir, secondDir);

                service.Start();

                Loggers.WriteInformation($"Ecoute du répertoire {firstDir}");
                Console.ReadLine();
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
