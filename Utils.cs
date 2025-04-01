using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    class Utils
    {
        public static Process RunIn(string dir, string command)
        {
            return RunIn(dir, command, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder);
        }

        public static Process RunIn(string dir, string command, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            string[] parts = command.Split(' ', 2);
            var startInfo = new ProcessStartInfo
            {
                FileName = parts[0],
                Arguments = parts.Length > 1 ? parts[1] : "",
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            return RunProcess(startInfo, out errorBuilder, out outputBuilder);
        }

        public static Process Run(string command, string args)
        {
            return Run(command, args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder);
        }

        public static Process Run(string command, string args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            return RunProcess(startInfo, out errorBuilder, out outputBuilder);
        }

        private static Process RunProcess(ProcessStartInfo startInfo, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder)
        {
            errorBuilder = null;
            outputBuilder = null;

            int maxRetries = int.Parse(ConfigurationManager.AppSettings["RUN_PROCESS_MAX_RETRIES"] ?? "3");
            int timeoutSeconds = int.Parse(ConfigurationManager.AppSettings["RUN_PROCESS_TIMEOUT_INTERVAL_SECS"] ?? "60");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var process = new Process();
                process.StartInfo = startInfo;

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

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        process.Kill(true);
                        throw new TimeoutException($"Process '{process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out on attempt {attempt}.");
                    }

                    process.WaitForExit(); // Ensure output is flushed

                    errorBuilder = tempErrorBuilder.Length > 0 ? tempErrorBuilder : null;
                    outputBuilder = tempOutputBuilder.Length > 0 ? tempOutputBuilder : null;

                    return process; //success
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout on attempt {attempt}: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Console.WriteLine("Max retries reached. Failing...");
                        throw;
                    }

                    // Optional: delay between retries
                    Thread.Sleep(1000);
                }
            }

            throw new Exception($"Unexpected failure while running process for {startInfo.ToString}");
        }
    }
}
