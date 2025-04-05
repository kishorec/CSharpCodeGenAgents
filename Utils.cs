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
            return RunIn(dir, command, out errorBuilder, out outputBuilder, false);
        }

        public static Process RunIn(string dir, string command, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder, bool throwOnError)
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

            return RunProcess(startInfo, out errorBuilder, out outputBuilder, throwOnError);
        }

        public static Process Run(string command, string args)
        {
            return Run(command, args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder, false);
        }

        public static Process Run(string command, string args, bool throwOnError)
        {
            return Run(command, args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder, throwOnError);
        }

        public static Process Run(string command, string args, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder, bool throwOnError)
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

            return RunProcess(startInfo, out errorBuilder, out outputBuilder, throwOnError);
        }

        private static Process RunProcess(ProcessStartInfo startInfo, out StringBuilder? errorBuilder, out StringBuilder? outputBuilder, bool throwOnError)
        {
            errorBuilder = null;
            outputBuilder = null;

            int maxRetries = AgentConfiguration.RUN_PROCESS_MAX_RETRIES;
            int timeoutSeconds = AgentConfiguration.RUN_PROCESS_TIMEOUT_INTERVAL_SECS;

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
                        throw new TimeoutException($"Process '{startInfo.FileName} {startInfo.Arguments}' timed out on attempt {attempt}.");
                    }

                    process.WaitForExit(); // Ensure output is flushed

                    errorBuilder = tempErrorBuilder.Length > 0 ? tempErrorBuilder : null;
                    outputBuilder = tempOutputBuilder.Length > 0 ? tempOutputBuilder : null;

                    if (process.ExitCode != 0 && throwOnError)
                    {
                        throw new Exception($"Process '{startInfo.FileName} {startInfo.Arguments}' failed with exit code {process.ExitCode} on attempt {attempt}.");
                    }   

                    return process; //success
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout running process '{startInfo.FileName} {startInfo.Arguments}' on attempt {attempt}: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"Max retries for running the process '{startInfo.FileName} {startInfo.Arguments}' reached. Throwing TimeoutException...");
                        throw;
                    }

                    // Optional: delay between retries
                    Thread.Sleep(1000);
                }
            }

            throw new ProcessExecutionFailedException($"Unexpected failure while running process for {startInfo.ToString}");
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

            // Ensure that the total length of these strings is less than max number of chars. This is to ensure that the
            // Max size of the prompt supported by the OpenAI API is not exceeded.
            int maxPromptTokens = AgentConfiguration.AZURE_OPENAI_MAX_PROMPT_TOKENS;
            int maxNumberOfChars = maxPromptTokens * AgentConfiguration.NUMBER_OF_CHARS_PER_TOKEN;

            int totalLength = code.Length + errors.Length + task.Length;

            if (totalLength <= maxNumberOfChars)
            {
                // No trimming needed
                return;
            }

            int excess = totalLength - maxNumberOfChars;

            if (errors.Length > excess)
            {
                // Trim errors enough to fit
                errors = errors.Substring(0, errors.Length - excess);
                Console.WriteLine($"Trimming errors by {excess} characters to fit the max token limit of {maxPromptTokens}.");
            }
            else
            {
                // If errors is smaller than excess, trim errors completely
                errors = string.Empty;
                Console.WriteLine($"Trimming errors and setting it to empty to fit the max token limit of {maxPromptTokens}.");
            }
        }

    }

    public class ProcessExecutionFailedException : Exception
    {
        public ProcessExecutionFailedException(string message) : base(message) { }
    }

    public static class AgentConfiguration
    {
        static AgentConfiguration()
        {
            AZURE_OPENAI_ENDPOINT = ConfigurationManager.AppSettings["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
            AZURE_OPENAI_KEY = ConfigurationManager.AppSettings["AZURE_OPENAI_KEY"] ?? string.Empty;
            AZURE_OPENAI_DEPLOYMENT = ConfigurationManager.AppSettings["AZURE_OPENAI_DEPLOYMENT"] ?? string.Empty;
            AZURE_OPENAI_API_VERSION = ConfigurationManager.AppSettings["AZURE_OPENAI_API_VERSION"] ?? "2024-12-01-preview";
            AZURE_OPENAI_CALL_MAX_RETRIES = Int32.Parse(ConfigurationManager.AppSettings["AZURE_OPENAI_CALL_MAX_RETRIES"] ?? "0");
            AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS = Int32.Parse(ConfigurationManager.AppSettings["AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS"] ?? "1000");
            AZURE_OPENAI_MAX_NUMBER_OF_TOKENS = Int32.Parse(ConfigurationManager.AppSettings["AZURE_OPENAI_MAX_NUMBER_OF_TOKENS"] ?? "100000");
            AZURE_OPENAI_MAX_COMPLETION_TOKENS = AZURE_OPENAI_MAX_NUMBER_OF_TOKENS / 3; // 1/3 of the total tokens for response
            AZURE_OPENAI_MAX_PROMPT_TOKENS = (AZURE_OPENAI_MAX_NUMBER_OF_TOKENS * 2) / 3; // 2/3 of the total tokens for prompt
            APP_TYPE = ConfigurationManager.AppSettings["APP_TYPE"] ?? "console";
            RUN_PROCESS_MAX_RETRIES = int.Parse(ConfigurationManager.AppSettings["RUN_PROCESS_MAX_RETRIES"] ?? "1");
            RUN_PROCESS_TIMEOUT_INTERVAL_SECS = int.Parse(ConfigurationManager.AppSettings["RUN_PROCESS_TIMEOUT_INTERVAL_SECS"] ?? "60");
            NUMBER_OF_CHARS_PER_TOKEN = 4; // Approximate value for English text
        }

        public static string APP_TYPE { get; private set; }
        public static string AZURE_OPENAI_ENDPOINT { get; private set; }
        public static string AZURE_OPENAI_KEY { get; private set; }
        public static string AZURE_OPENAI_DEPLOYMENT { get; private set; }
        public static string AZURE_OPENAI_API_VERSION { get; private set; }
        public static int AZURE_OPENAI_CALL_MAX_RETRIES { get; private set; }
        public static int AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS { get; private set; }
        public static int AZURE_OPENAI_MAX_NUMBER_OF_TOKENS { get; private set; }
        public static int AZURE_OPENAI_MAX_COMPLETION_TOKENS { get; private set; }
        public static int AZURE_OPENAI_MAX_PROMPT_TOKENS { get; private set; }
        public static int NUMBER_OF_CHARS_PER_TOKEN { get; private set; } 
        public static int RUN_PROCESS_MAX_RETRIES { get; private set; }
        public static int RUN_PROCESS_TIMEOUT_INTERVAL_SECS { get; private set; }
    }
}
