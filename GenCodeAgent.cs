namespace Microsoft.AzureDataEngineering.AI
{
    class GenCodeAgent
    {
        public static async Task<string> GenerateAsync(string task)
        {
            Console.WriteLine("'CodeGen' agent is building the prompt for the task...");
            string prompt = $@"
                You are an expert C# software engineer writing production-quality code.

                Your task is to solve the following problem:
                {task}

                Requirements:
                - Write clean, correct, and idiomatic C# code targeting .NET 6 or higher
                - All code must be valid and compile without modification

                Structure:
                - Place the core logic in a public static class named 'Solution'
                - The entry method must be public static and named 'Solve'
                - If user interaction is required, isolate it in a separate class (e.g., 'ConsoleUI')
                - Use the namespace: Microsoft.AzureDataEngineering.AI
                - Add all necessary `using` directives at the top of the file

                Guidelines:
                - Use generic types where appropriate in method signatures
                - Prefer async/await for asynchronous operations
                - Use standard C# libraries only (no external NuGet packages)
                - Write code with readability, performance, and maintainability in mind
                - Do not include any explanation, markdown, or natural language text
                - Only include comments using `//` when necessary to clarify the code

                Do not:
                - Include any text outside the code block
                - Output pseudocode, logs, or placeholders

                Output:
                - Only valid, fully compilable C# code following the above requirements
                ";
            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
