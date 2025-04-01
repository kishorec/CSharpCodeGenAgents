namespace Microsoft.AzureDataEngineering.AI
{
    class GenCodeAgent
    {
        public static async Task<string> GenerateAsync(string task)
        {
            Console.WriteLine("'CodeGen' agent is building the prompt for the task...");
            string prompt = $@"
                You are an expert software developer. Write clean C# code to solve the following problem:
                {task}

                Requirements:
                - Wrap the method inside a public static class named 'Solution'
                - Use a public static method called 'Solve'
                - Make sure it compiles on .NET 6 or higher
                - Use generic types in the method signature wherever possible
                - Include necessary using directives at the top
                - Do not use any external libraries or frameworks
                - Use standard C# libraries only
                - use async/await for async methods wherever possible
                - Only generate code that is valid C# code and compiles
                - Do not add any explanations without comment marker //
                - Optimize the algorithms for performance and readability and memory usage
                ";
            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
