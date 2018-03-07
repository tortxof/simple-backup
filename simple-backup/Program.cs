using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace simple_backup
{
    public class BackupJob
    {
        public string Drive { get; }
        public string Destination { get; }
        public List<string> Sources { get; }

        public BackupJob(string drive, string destination, List<string> sources)
        {
            this.Drive = drive;
            this.Destination = destination;
            this.Sources = sources;
        }
    }
    class Program
    {
        public static void CopyRecursive(DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists)
            {
                target.Create();
            }
            try
            {
                foreach (FileInfo sourceFile in source.EnumerateFiles())
                {
                    var destFile = new FileInfo(Path.Combine(target.FullName, sourceFile.Name));
                    if (!destFile.Exists || sourceFile.Length != destFile.Length && sourceFile.LastWriteTimeUtc != destFile.LastWriteTimeUtc)
                    {
                        Console.WriteLine($"{sourceFile.FullName} -> {destFile.FullName}");
                        sourceFile.CopyTo(destFile.FullName, true);
                    }
                    else
                    {
                        // Console.WriteLine($"Up to date: {destFile.FullName}");
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            try
            {
                foreach (var subDir in source.EnumerateDirectories())
                {
                    var nextTarget = target.CreateSubdirectory(subDir.Name);
                    CopyRecursive(subDir, nextTarget);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Main(string[] args)
        {
            string systemDrive = Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.System)
            ).Substring(0, 2);
            Console.WriteLine($"System drive: {systemDrive}");
            var drives = new List<string>();
            var jobs = new List<BackupJob>();
            foreach (var drive in DriveInfo.GetDrives().Select(drive => drive.ToString().Substring(0, 2)))
            {
                if (drive != systemDrive)
                {
                    drives.Add(drive);
                }
            }
            foreach (var drive in drives)
            {
                var configFile = new FileInfo($"{drive}\\backup-config.json");
                if (configFile.Exists)
                {
                    Console.WriteLine($"Config found for drive {drive}");
                    string jsonData = "";
                    using (StreamReader sr = configFile.OpenText())
                    {
                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            jsonData += s;
                        }
                    }
                    var configJobs = JsonConvert.DeserializeObject<List<BackupJob>>(jsonData);
                    foreach (var configJob in configJobs)
                    {
                        jobs.Add(new BackupJob(drive, configJob.Destination, configJob.Sources));
                    }
                }
            }
            foreach (var job in jobs)
            {
                var destDir = new DirectoryInfo(Environment.ExpandEnvironmentVariables(
                    Path.Combine($"{job.Drive}\\", job.Destination)
                ));
                destDir.Create();
                Console.WriteLine($"Destination: {destDir.FullName}");
                foreach (var source in job.Sources)
                {
                    var sourceDir = new DirectoryInfo(Environment.ExpandEnvironmentVariables(source));
                    if (sourceDir.Exists)
                    {
                        var sourceDestDir = new DirectoryInfo(Path.Combine(destDir.FullName, sourceDir.Name));
                        sourceDestDir.Create();
                        Console.WriteLine($"copy {sourceDir.FullName} to {sourceDestDir.FullName}");
                        CopyRecursive(sourceDir, sourceDestDir);
                    }
                }
            }
        }
    }
}
