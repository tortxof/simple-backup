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
        public static int[] CopyRecursive(DirectoryInfo source, DirectoryInfo target)
        {
            string[] progressChars = { "|", "/", "-", "\\" };
            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;
            if (!target.Exists)
            {
                target.Create();
            }
            IEnumerable<FileInfo> sourceFiles = new List<FileInfo>();
            try
            {
                sourceFiles = source.EnumerateFiles();
            }
            catch (UnauthorizedAccessException) { }
            foreach (FileInfo sourceFile in sourceFiles)
            {
                var destFile = new FileInfo(Path.Combine(target.FullName, sourceFile.Name));
                if (!destFile.Exists || sourceFile.Length != destFile.Length || sourceFile.LastWriteTimeUtc != destFile.LastWriteTimeUtc)
                {
                    try
                    {
                        sourceFile.CopyTo(destFile.FullName, true);
                        successCount++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\rUnable to copy " + sourceFile.FullName);
                        Console.ResetColor();
                        failCount++;
                    }
                    catch (IOException)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\rUnable to copy " + sourceFile.FullName);
                        Console.ResetColor();
                        failCount++;
                    }
                }
                else
                {
                    skipCount++;
                }
                Console.Write($"\b{progressChars[(successCount + skipCount + failCount) % 4]}");
            }
            IEnumerable<DirectoryInfo> subDirs = new List<DirectoryInfo>();
            try
            {
                subDirs = source.EnumerateDirectories();
            }
            catch (UnauthorizedAccessException) { }
            foreach (var subDir in subDirs)
            {
                int[] counts = new int[3];
                try
                {
                    var nextTarget = target.CreateSubdirectory(subDir.Name);
                    counts = CopyRecursive(subDir, nextTarget);
                }
                catch (IOException e)
                {
                    Console.WriteLine("\n" + e);
                }
                successCount += counts[0];
                skipCount += counts[1];
                failCount += counts[2];
            }
            return new int[] { successCount, skipCount, failCount };
        }

        static List<BackupJob> GetJobsForDrive(string drive)
        {
            var jobs = new List<BackupJob>();
            var jsonConfig = new FileInfo($"{drive}\\backup-config.json");
            var txtConfig = new FileInfo($"{drive}\\backup-config.txt");
            if (jsonConfig.Exists)
            {
                Console.WriteLine($"JSON config found for drive {drive}");
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
                Console.WriteLine($"Text config found for drive {drive}");
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
                        lines.GetRange(1, lines.Count - 1)
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
                try
                {
                    destDir.Create();
                }
                catch (UnauthorizedAccessException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nWarning: Could not create destination directory: {destDir.FullName}");
                    Console.ResetColor();
                    continue;
                }
                Console.WriteLine($"\nDestination: {destDir.FullName}");
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
            }
            foreach (var job in subJobs)
            {
                Console.WriteLine($"\nStarting backup to {job.Item2}");
                int[] counts = CopyRecursive(job.Item1, job.Item2);
                Console.WriteLine($"\b \nFinished backup to {job.Item2}");
                Console.WriteLine($"  Copied: {counts[0]}");
                Console.WriteLine($"  Skipped: {counts[1]}");
                Console.WriteLine($"  Failed: {counts[2]}");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nBackup Complete.");
            Console.ResetColor();
            Console.WriteLine("Press any key.");
            Console.ReadKey();
        }
    }
}
