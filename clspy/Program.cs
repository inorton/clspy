using System;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace clspy
{
    sealed class Program
    {
        static Mutex sync = null;

        static void Lock()
        {
            do
            {
                try
                {
                    try
                    {
                        sync = Mutex.OpenExisting("clspy");
                    }
                    catch
                    {
                        sync = new Mutex(false, "clspy");
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
            Lock();
            try
            {
                var filename = "c:\\clspy.log";
                File.AppendAllText(filename, String.Format("{},{},{},{}", start, ended, workdir, args));
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
