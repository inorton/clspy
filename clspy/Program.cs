using System;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace clspy
{
    sealed class Program
    {
        static Mutex sync = null;
        static string lockname = "clspy";
        static string logdir = "c:\\clspy";
        static string logfile = "c:\\clspy\\clspy.log";

        static void Lock()
        {
            do
            {
                try
                {
                    try
                    {
                        sync = Mutex.OpenExisting(lockname);
                    }
                    catch
                    {
                        sync = new Mutex(false, lockname);
                    }
                }
                catch
                {

                }
            } while (sync == null);

            while (!sync.WaitOne());
        }

        static void Unlock()
        {
            sync.ReleaseMutex();
        }

        static void SaveLog(string workdir, string args, DateTime start, DateTime ended)
        {
            var duration = ended - start;
            var started = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            Lock();
            try
            {
                var line = String.Format("{0},{1},{2},{3}\n", started, duration.TotalSeconds, workdir, args);
                if (File.Exists(logfile))
                {
                    File.AppendAllText(logfile, line);
                } else
                {
                    File.WriteAllText(logfile, line);
                }
            }
            finally
            {
                Unlock();
            }
        }

        static void Main(string[] args)
        {
            int exitcode = 1;
            var self = Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var self_dir = Path.GetDirectoryName(self);            
            var real = Path.Combine(self_dir, Path.GetFileNameWithoutExtension(self) + "_orig.exe");
            
            if (!File.Exists(real))
            {
                Console.Error.WriteLine("'{0}' does not exist", real);
                Environment.Exit(1);                
            }

            if (!Directory.Exists(logdir))
            {
                Console.Error.WriteLine("clspy log folder {0} does not exist", logdir);
                Environment.Exit(1);
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo(real);

            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.StartInfo.Arguments = Environment.CommandLine;
            proc.StartInfo.UseShellExecute = false;
            foreach (String name in Environment.GetEnvironmentVariables().Keys)
            {
                proc.StartInfo.EnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
            }
            var started = DateTime.Now;
            try
            {             
                proc.Start();
                proc.WaitForExit();
                exitcode = proc.ExitCode;                
            } catch (Exception err) {
                Console.Error.WriteLine(err.Message);                
            } finally
            {
                var ended = DateTime.Now;
                try
                {
                    SaveLog(Environment.CurrentDirectory, Environment.CommandLine, started, ended);
                } catch (Exception e) { }
            }
            Environment.Exit(exitcode);
        }
    }
}
