using System;
using System.IO;

namespace AnglickaVyzva.API.Helpers
{
    public class EnvironmentHelper
    {
        /// <summary>
        /// Jakekoli vyvojove prostredi: jak localhost tak RemoteDEV
        /// </summary>
        /// <returns></returns>
        public static bool IsOnAnyDev()
        {
            var isOnAnyDev = IsOnLocalDevelopMachine() || IsOnRemoteDev();

            //LogHelper.LogIntoFile($"isOnAnyDev: {isOnAnyDev}");  

            return isOnAnyDev;
        }

        public static bool IsOnRemoteDev()
        {
            

            var isOnRemoteDev = Environment.GetEnvironmentVariable("isOnRemoteDev");
            //LogHelper.LogIntoFile($"isOnRemoteDev.variable: {isOnRemoteDev}");

            return isOnRemoteDev == "true";
        }

        public static bool IsOnLocalDevelopMachine()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("thisIsDevelopMachine"));
        }
    }
}
