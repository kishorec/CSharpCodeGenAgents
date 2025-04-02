using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Azure.AI.OpenAI.AzureOpenAIClientOptions;

namespace Microsoft.AzureDataEngineering.AI
{
    public class AzureOpenAI
    {
        public static async Task<string> AskAzureAsync(string prompt)
        {
            Console.WriteLine("Asking Azure OpenAI...");
            var stopwatch = Stopwatch.StartNew();
            string endpoint = ConfigurationManager.AppSettings["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
            string key = ConfigurationManager.AppSettings["AZURE_OPENAI_KEY"] ?? string.Empty;
            string deployment = ConfigurationManager.AppSettings["AZURE_OPENAI_DEPLOYMENT"] ?? string.Empty;
            string apiVersion = ConfigurationManager.AppSettings["AZURE_OPENAI_API_VERSION"] ?? "2024-12-01-preview";
            int maxRetries = Int32.Parse(ConfigurationManager.AppSettings["AZURE_OPENAI_CALL_MAX_RETRIES"] ?? "0");
            int delayMilliseconds = Int32.Parse(ConfigurationManager.AppSettings["AZURE_OPENAI_CALL_WAIT_INTERVAL_SECS"] ?? "1000");

            using var client = new HttpClient();
            client.BaseAddress = new Uri(endpoint);
            client.DefaultRequestHeaders.Add("api-key", key);

            var body = new
            {
                messages = new[] { new { role = "user", content = prompt } },
                
                //max_tokens = 10000, //Works for GPT4
                max_completion_tokens = 10000,
                model = deployment
            };

            // Pre-serialize the payload for reuse.
            string jsonPayload = JsonSerializer.Serialize(body);

            HttpResponseMessage response = null;

            // Only retry the POST call if an exception occurs.
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                    {
                        response = await client.PostAsync(
                            $"openai/deployments/{deployment}/chat/completions?api-version={apiVersion}",
                            content);
                    }
                    // Break out of the loop if POST was successful (no exception thrown).
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} failed with exception: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        throw; // Re-throw the exception if max retries reached.
                    }
                    await Task.Delay(delayMilliseconds);
                }
            }

            // Process the response outside the retry loop.
            string json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Azure OpenAI API Error: " + response.StatusCode);
                Console.WriteLine(json);
                throw new Exception("Azure OpenAI API call failed.");
            }

            String result = ParseOpenAIResponse(json);
            stopwatch.Stop();
            Console.WriteLine($"Time taken by Azure OpenAI: {stopwatch.ElapsedMilliseconds} ms");
            return result;
        }

        static string ParseOpenAIResponse(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
                    throw new Exception("No 'choices' returned from OpenAI response.");

                JsonElement firstChoice = choices[0];

                if (!firstChoice.TryGetProperty("message", out JsonElement message))
                    throw new Exception("Missing 'message' object in OpenAI response.");

                if (!message.TryGetProperty("content", out JsonElement content))
                    throw new Exception("Missing 'content' inside 'message'.");

                return content.GetString() ?? throw new Exception("❌ 'content' is null in OpenAI response.");
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse JSON from OpenAI: " + ex.Message);
            }
        }

        /// <summary>
        /// Sample code to show how to use the Azure SDK. This however cannot be used for all the models. 
        /// There are some limitations. Refer the documentation from Azure.
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public static async Task<string> AskAzureSdkAsync(string prompt)
        {
            Console.WriteLine("Asking Azure OpenAI...");
            //Console.WriteLine("Prompt:");
            //Console.WriteLine(prompt);

            string endpoint = ConfigurationManager.AppSettings["AZURE_OPENAI_ENDPOINT"] ?? String.Empty;
            string key = ConfigurationManager.AppSettings["AZURE_OPENAI_KEY"] ?? String.Empty;
            string deployment = ConfigurationManager.AppSettings["AZURE_OPENAI_DEPLOYMENT"] ?? String.Empty;
            string apiVersion = ConfigurationManager.AppSettings["AZURE_OPENAI_API_VERSION"] ?? "2024-12-01-preview";

            AzureOpenAIClientOptions clientOptions = new AzureOpenAIClientOptions(ServiceVersion.V2024_10_21);

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key), clientOptions);

            var chatOptions = new ChatCompletionOptions()
            {
                Temperature = 0.5f,
                MaxOutputTokenCount = 10000,
            };

            ChatClient chatClient = azureClient.GetChatClient(deployment);

            ChatCompletion completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are an expert C# assistant."),
                    new UserChatMessage(prompt),
                ],
                chatOptions);

            return completion.Content[0].Text.Trim();
        }
    }
}