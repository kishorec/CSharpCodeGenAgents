    using System.Configuration;

namespace Microsoft.AzureDataEngineering.AI
{
    public static class AgentConfiguration
    {
        static AgentConfiguration()
        {
            AZURE_OPENAI_ENDPOINT = GetSetting("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
            AZURE_OPENAI_KEY = GetSetting("AZURE_OPENAI_KEY") ?? string.Empty;
            AZURE_OPENAI_DEPLOYMENT = GetSetting("AZURE_OPENAI_DEPLOYMENT") ?? string.Empty;
            AZURE_OPENAI_API_VERSION = GetSetting("AZURE_OPENAI_API_VERSION") ?? "2024-12-01-preview";

            AZURE_OPENAI_CALL_MAX_RETRIES = ParseIntSetting("AZURE_OPENAI_CALL_MAX_RETRIES", 0);
            AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS = ParseIntSetting("AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS", 1000);
            AZURE_OPENAI_MAX_NUMBER_OF_TOKENS = ParseIntSetting("AZURE_OPENAI_MAX_NUMBER_OF_TOKENS", 100000);

            AZURE_OPENAI_MAX_COMPLETION_TOKENS = AZURE_OPENAI_MAX_NUMBER_OF_TOKENS / 3;
            AZURE_OPENAI_MAX_PROMPT_TOKENS = (AZURE_OPENAI_MAX_NUMBER_OF_TOKENS * 2) / 3;

            APP_TYPE = GetSetting("APP_TYPE") ?? "console";

            RUN_PROCESS_MAX_RETRIES = ParseIntSetting("RUN_PROCESS_MAX_RETRIES", 1);
            RUN_PROCESS_TIMEOUT_INTERVAL_SECS = ParseIntSetting("RUN_PROCESS_TIMEOUT_INTERVAL_SECS", 60);

            MAX_NUMBER_OF_CODEGEN_RETRIES = ParseIntSetting("MAX_NUMBER_OF_CODEGEN_RETRIES", 10);

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

        public static int MAX_NUMBER_OF_CODEGEN_RETRIES { get; private set; }

        private static string? GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private static int ParseIntSetting(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }
            return defaultValue;
        }
    }
}


