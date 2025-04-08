using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.AzureDataEngineering.AI
{
    class GenCodeAgent
    {
        public static async Task<string> GenerateAsync(string task)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(task);

            Console.WriteLine("'CodeGen' agent is building the request for the task...");
            string prompt = $@"
                You are an expert C# software engineer writing production-quality code.

                Your task is to solve the following problem:
                {task}

                Requirements:
                - Write clean, correct, and idiomatic C# code targeting .NET 6 or higher
                - All code must be valid and compile without modification

                Structure:
                - Place the core logic in a public static class named 'Solution'
                - Add an entry Main() method in the Solution class to demonstrate the functionality
                - If UI elements are present, separate them from business logic
                - Use the namespace: Microsoft.AzureData.Engineering.AI
                - Include all necessary `using` directives at the top

                Guidelines:
                - Use async/await for any asynchronous logic
                - Prefer generic types in method signatures wherever appropriate
                - Output only valid, compilable C# source code
                - Use standard C# libraries only (no external NuGet packages)
                - Avoid redundant logic
                - Write code with readability, performance, and maintainability in mind
                - Do not include natural language explanations, markdown, or pseudocode
                - Only include comments using `//` when necessary to clarify the code

                Do not:
                - Include any text outside the code block
                - Output pseudocode, logs, or placeholders

                Output:
                - Only valid, fully compilable C# code following the above requirements
                ";
            return await AzureOpenAI.AskAsync(prompt);
        }
    }
}
