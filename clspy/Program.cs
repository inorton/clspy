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
            var real_cl = Environment.GetEnvironmentVariable("REAL_CL_EXE");
            var started = DateTime.Now;

            if (real_cl == null)
            {
                Console.Error.WriteLine("REAL_CL_EXE not set");
                Environment.Exit(1);
            } else
            {
                if (!File.Exists(real_cl))
                {
                    Console.Error.WriteLine("REAL_CL_EXE '{0}' does not exist", real_cl);
                    Environment.Exit(1);
                }
            }

            if (!Directory.Exists(logdir))
            {
                Console.Error.WriteLine("clspy log folder {0} does not exist", logdir);
                Environment.Exit(1);
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo(real_cl);

            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.StartInfo.Arguments = Environment.CommandLine;
            try
            {
                foreach (String name in Environment.GetEnvironmentVariables().Keys)
                {
                    proc.StartInfo.EnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
                }

                proc.Start();
                proc.WaitForExit();
                var ended = DateTime.Now;
                SaveLog(Environment.CurrentDirectory, Environment.CommandLine, started, ended);
                Environment.Exit(proc.ExitCode);
            } catch (Exception err) {
                Console.Error.WriteLine(err.Message);
                Environment.Exit(1);
            }
        }
    }
}
