namespace Microsoft.AzureDataEngineering.AI
{
    class GenDocAgent
    {
        public static async Task<string> GenerateAsync(string code)
        {
            string prompt = $@"
                Write a professional design document based on the following C# code:
                {code}

                The document should include:
                - A brief summary of the purpose and problem being solved
                - Write atleast two-page document wherever possible
                - Core logic explanation
                - Documentation for key methods
                - A sequence diagram (in code block using Mermaid or text form)
                - A block diagram or flow chart if appropriate
                - Format it as Markdown (start with ## headings).
                ";
            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
