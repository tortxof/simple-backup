using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace simple_backup
{
    public class BackupJob
    {
        public string Destination { get; }
        public List<string> Sources { get; }

        public BackupJob(string destination, List<string> sources)
        {
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
                    if (!destFile.Exists || sourceFile.Length != destFile.Length || sourceFile.LastWriteTimeUtc != destFile.LastWriteTimeUtc)
                    {
                        Console.Write("+");
                        sourceFile.CopyTo(destFile.FullName, true);
                    }
                    else
                    {
                        Console.Write($".");
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.Write("!");
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
                Console.Write("!");
            }
        }

        static List<BackupJob> GetJobsForDrive(string drive)
        {
            var jobs = new List<BackupJob>();
            var jsonConfig = new FileInfo($"{drive}\\backup-config.json");
            var txtConfig = new FileInfo($"{drive}\\backup-config.txt");
            if (jsonConfig.Exists)
            {
                Console.WriteLine($"JSON config found for drive {drive}\n");
                string jsonData = "";
                using (StreamReader sr = jsonConfig.OpenText())
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
                    jobs.Add(new BackupJob(
                        Path.Combine($"{drive}\\", configJob.Destination),
                        configJob.Sources
                    ));
                }
            }
            else if (txtConfig.Exists)
            {
                Console.WriteLine($"Text config found for drive {drive}\n");
                var lines = new List<string>();
                using (StreamReader sr = txtConfig.OpenText())
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        s = s.Trim();
                        if (s.Length > 0 && !s.Substring(0, 1).Equals("#"))
                        {
                            lines.Add(s);
                        }
                    }
                    jobs.Add(new BackupJob(
                        Path.Combine($"{drive}\\", lines[0]),
                        lines.GetRange(1, lines.Count-1)
                    ));
                }
            }
            return jobs;
        }

        static void Main(string[] args)
        {
            string systemDrive = Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.System)
            ).Substring(0, 2);
            var drives = new List<string>();
            var jobs = new List<BackupJob>();
            var subJobs = new List<Tuple<DirectoryInfo, DirectoryInfo>>();
            foreach (var drive in DriveInfo.GetDrives().Select(drive => drive.ToString().Substring(0, 2)))
            {
                if (drive != systemDrive)
                {
                    drives.Add(drive);
                }
            }
            foreach (var drive in drives)
            {
                jobs.AddRange(GetJobsForDrive(drive));
            }
            foreach (var job in jobs)
            {
                var destDir = new DirectoryInfo(
                    Environment.ExpandEnvironmentVariables(job.Destination)
                );
                destDir.Create();
                Console.WriteLine($"Destination: {destDir.FullName}");
                foreach (var source in job.Sources)
                {
                    var sourceDir = new DirectoryInfo(Environment.ExpandEnvironmentVariables(source));
                    if (sourceDir.Exists)
                    {
                        var sourceDestDir = new DirectoryInfo(Path.Combine(destDir.FullName, sourceDir.Name));
                        sourceDestDir.Create();
                        Console.WriteLine($"  Source: {sourceDir.FullName}");
                        subJobs.Add(Tuple.Create(sourceDir, sourceDestDir));
                    }
                }
                Console.WriteLine("");
            }
            foreach (var job in subJobs)
            {
                CopyRecursive(job.Item1, job.Item2);
            }
            Console.WriteLine("\n\nDone...");
            Thread.Sleep(5000);
        }
    }
}
