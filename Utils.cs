using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    class Utils
    {
        public static string MarkdownToHtml(string md)
        {
            return $"<html><head><meta charset='utf-8'><title>Design Doc</title></head><body><pre>{md}</pre></body></html>";
        }

        public static Process RunIn(string dir, string command)
        {
            return RunIn(dir, command, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder);
        }

        public static Process RunIn(string dir, string command, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            string[] parts = command.Split(' ', 2);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = parts.Length > 1 ? parts[1] : "",
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            return RunProcess(process, out errorBuilder, out outputBuilder);
        }

        public static Process Run(string command, string args)
        {
            return Run(command, args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder);
        }

        public static Process Run(string command, string args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            return RunProcess(process, out errorBuilder, out outputBuilder);
        }

        private static Process RunProcess(Process process, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            errorBuilder = null;
            outputBuilder = null;
            var tempOutputBuilder = new StringBuilder();
            var tempErrorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) tempOutputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) tempErrorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            int timeoutSeconds = Int32.Parse(ConfigurationManager.AppSettings["BUILD_AND_TEST_TIMEOUT_INTERVAL_SECS"] ?? "60");
            if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                process.Kill(true);
                throw new TimeoutException($"Process '{process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out.");
            }

            process.WaitForExit(); // Ensure output handlers are finished

            if (tempErrorBuilder.Length > 0)
            {
                errorBuilder = tempErrorBuilder;
            }
            if (tempOutputBuilder.Length > 0)
            {
                outputBuilder = tempOutputBuilder;
            }
            return process;
        }
    }
}
