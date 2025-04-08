namespace Microsoft.AzureDataEngineering.AI
{
    class GenDocAgent
    {
        public static async Task<string> GenerateAsync(string code)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(code);

            Console.WriteLine("'DocumentGen' agent is building the request for the task...");
            string prompt = $@"
                You are a senior software architect. Based on the C# code below, write a **professional design document** in clear, well-structured **Markdown** format.

                Source Code:
                {code}

                Your document must include the following sections:

                1. Purpose & Problem Statement
                - Briefly describe what the code does
                - Explain the problem it solves or the use case it addresses

                2. Core Logic Overview
                - Explain the high-level structure of the solution
                - Highlight key algorithms or design patterns used

                3. Key Method Documentation
                - List and describe each key public method and class
                - Include parameter explanations and return values
                - Mention any notable logic or special edge-case handling

                4. Sequence Diagram
                - Include a **Mermaid** diagram (in a fenced code block) showing the flow of method calls, especially if there are multiple classes or components

                5. Block Diagram or Flowchart
                - Provide a simple text-based flowchart or another Mermaid diagram to visualize control flow, class interaction, or data movement

                Formatting:
                - Use clear Markdown with section headers (e.g., ## Purpose)
                - Use bullet points, code blocks, and numbered lists where helpful
                - Avoid unnecessary filler text or AI-generated disclaimers
                - Document should aim for at least **two pages** of content when rendered

                Output:
                - Markdown content **only**
                - No extra explanations outside the document
                ";

            return await AzureOpenAI.AskAsync(prompt);
        }
    }
}
