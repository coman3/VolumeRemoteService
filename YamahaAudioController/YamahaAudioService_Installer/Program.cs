using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YamahaAudioService_Installer
{
    class Program
    {
        public const string ServiceName = "Yamaha Amplifier Controller";
        public const string ServiceRelativePath = "YamahaAudioService\\bin\\Debug\\YamahaAudioService.exe";
        public const string InstallUtilFileName = "InstallUtil.exe";
        public static readonly string InstallUtilPath = Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), InstallUtilFileName);
        public static readonly string ServicePath = Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\..\\", ServiceRelativePath);
        

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;            
            var services = ServiceController.GetServices();
            var serviceThatMatches = services.FirstOrDefault(x => x.DisplayName == ServiceName);
            if (serviceThatMatches != null)
                ManageServiceInstall(uninstall: true);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Installing Service...");
            ManageServiceInstall();
            Console.WriteLine("Finished! :)");

        }

        private static void StartService()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = "/C " + $"NET START '{ServiceName}'";
            cmd.Start();

            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
        }

        private static void ManageServiceInstall(bool uninstall = false)
        {
            var installProccess = Process.Start(InstallUtilPath, $"{(uninstall ? "/u " : "")}\"{ServicePath}\"");
            installProccess.StartInfo.RedirectStandardInput = true;
            installProccess.StartInfo.RedirectStandardOutput = true;
            installProccess.StartInfo.CreateNoWindow = true;
            installProccess.StartInfo.UseShellExecute = false;
            installProccess.Start();
            Console.ForegroundColor = ConsoleColor.Yellow;
            do
            {
                Console.Out.Write(installProccess.StandardOutput.ReadToEnd());
            }
            while (!installProccess.HasExited);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
