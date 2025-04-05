using System.Diagnostics;
using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    public static class Utils
    {
        public static async Task<(Process process, StringBuilder? errorBuilder, StringBuilder? outputBuilder)> 
            RunInAsync(string dir, string command, bool throwOnError = false)
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

            return await RunProcessAsync(startInfo, throwOnError);
        }

        public static async Task<(Process process, StringBuilder? errorBuilder, StringBuilder? outputBuilder)> 
            RunAsync(string command, string args, bool throwOnError = false)
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

            return await RunProcessAsync(startInfo, throwOnError);
        }

        private static async Task<(Process process, StringBuilder? errorBuilder, StringBuilder? outputBuilder)> 
            RunProcessAsync(ProcessStartInfo startInfo, bool throwOnError)
        {
            int maxRetries = AgentConfiguration.RUN_PROCESS_MAX_RETRIES;
            int timeoutSeconds = AgentConfiguration.RUN_PROCESS_TIMEOUT_INTERVAL_SECS;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                var outputTcs = new TaskCompletionSource<bool>();
                var errorTcs = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) outputTcs.TrySetResult(true);
                    else outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) errorTcs.TrySetResult(true);
                    else errorBuilder.AppendLine(e.Data);
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool exited = await WaitForExitAsync(process, timeoutSeconds);

                    if (!exited)
                    {
                        process.Kill(entireProcessTree: true);
                        throw new TimeoutException($"Process '{startInfo.FileName} {startInfo.Arguments}' timed out on attempt {attempt}.");
                    }

                    await Task.WhenAll(outputTcs.Task, errorTcs.Task);

                    if (process.ExitCode != 0 && throwOnError)
                    {
                        throw new Exception($"Process '{startInfo.FileName} {startInfo.Arguments}' failed with exit code {process.ExitCode} on attempt {attempt}.");
                    }

                    return (process,
                            errorBuilder.Length > 0 ? errorBuilder : null,
                            outputBuilder.Length > 0 ? outputBuilder : null);
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout running process '{startInfo.FileName} {startInfo.Arguments}' on attempt {attempt}: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for process '{startInfo.FileName} {startInfo.Arguments}'. Throwing...");
                        throw;
                    }

                    await Task.Delay(200); // Delay before retry
                }
            }

            throw new ProcessExecutionFailedException($"Unexpected failure while running process '{startInfo.FileName} {startInfo.Arguments}'.");
        }

        public static async Task<(bool success, string output)> RunCommandAsync(string dir, string command)
        {
            var (process, errorBuilder, outputBuilder) = await RunInAsync(dir, command);

            string output = errorBuilder?.ToString() ?? outputBuilder?.ToString() ?? string.Empty;
            bool success = process.ExitCode == 0;

            return (success, output);
        }

        private static async Task<bool> WaitForExitAsync(Process process, int timeoutSeconds)
        {
            try
            {
                return await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));
            }
            catch
            {
                return false;
            }
        }

        public static void TrimErrorsIfNeeded(ref string code, ref string errors, ref string task)
        {
            if (code == null || errors == null || task == null)
            {
                throw new ArgumentNullException("Code, errors, and task cannot be null.");
            }

            code = code.Trim();
            errors = errors.Trim();
            task = task.Trim();

            int maxPromptTokens = AgentConfiguration.AZURE_OPENAI_MAX_PROMPT_TOKENS;
            int maxChars = maxPromptTokens * AgentConfiguration.NUMBER_OF_CHARS_PER_TOKEN;
            int totalLength = code.Length + errors.Length + task.Length;

            if (totalLength <= maxChars)
                return;

            int excess = totalLength - maxChars;

            if (errors.Length > excess)
            {
                errors = errors.Substring(0, errors.Length - excess);
                Console.WriteLine($"Trimming errors by {excess} characters to fit the max token limit.");
            }
            else
            {
                errors = string.Empty;
                Console.WriteLine($"Errors fully trimmed to fit the token limit.");
            }
        }
    }

    public class ProcessExecutionFailedException : Exception
    {
        public ProcessExecutionFailedException(string message) : base(message) { }
    }
}
