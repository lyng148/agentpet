using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.WriteLine("Installing AgentPet...");
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string installDir = Path.Combine(localAppData, "AgentPetApp");

        if (Directory.Exists(installDir))
        {
            Console.WriteLine("Removing old version...");
            try { 
                foreach(var proc in Process.GetProcessesByName("AgentPetApp")) {
                    proc.Kill();
                    proc.WaitForExit(2000);
                }
                Directory.Delete(installDir, true); 
            } catch { }
        }
        Directory.CreateDirectory(installDir);

        Console.WriteLine("Extracting files...");
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AgentPetInstaller.Payload.zip"))
        {
            if (stream == null) {
                Console.WriteLine("Failed to load payload.");
                Console.ReadLine();
                return;
            }
            using (var archive = new ZipArchive(stream))
            {
                archive.ExtractToDirectory(installDir, true);
            }
        }

        Console.WriteLine("Creating shortcuts...");
        string exePath = Path.Combine(installDir, "AgentPetApp.exe");
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

        CreateShortcut(Path.Combine(desktop, "AgentPet.lnk"), exePath);
        CreateShortcut(Path.Combine(startMenu, "AgentPet.lnk"), exePath);

        Console.WriteLine("Installation complete! Launching AgentPet...");
        System.Threading.Thread.Sleep(1000);
        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
    }

    static void CreateShortcut(string shortcutPath, string targetPath)
    {
        string ps = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');$s.TargetPath='{targetPath}';$s.WorkingDirectory='{Path.GetDirectoryName(targetPath)}';$s.Save()";
        var proc = Process.Start(new ProcessStartInfo("powershell", $"-Command \"{ps}\"") { CreateNoWindow = true, UseShellExecute = false });
        proc?.WaitForExit();
    }
}
