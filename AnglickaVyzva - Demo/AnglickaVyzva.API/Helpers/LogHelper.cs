using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public static class LogHelper
    {
        const string token = "xxx";

        const int projectId = 8;

        public static void LogIntoFile(string text)
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), @"myLog.txt");

                // Pokud je Vetsi nez MB, tak ho smaz
                if(File.Exists(logPath))
                {
                    var info = new FileInfo(logPath);
                    if(info.Length > 1000000) // Neni otestovano, jestli je toto opravdu 1MB
                    {
                        File.Delete(logPath);
                    }
                }

                File.AppendAllText(logPath, $"{DateTime.Now}: {text}\n");
            }
            catch
            {

            }
        }
    }
}
