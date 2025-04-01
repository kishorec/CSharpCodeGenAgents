namespace Microsoft.AzureDataEngineering.AI
{
    class FixCodeAgent
    {
        public static async Task<string> GenerateAsync(string code, string errors)
        {
            Console.WriteLine("'FixCode' agent is building the prompt for the task...");

            string prompt = $@"
                The following C# code failed. Here is the code:
                {code}

                Here is the errors output:
                {errors}

                Please fix the code so that all tests pass.
                - Wrap the method inside a public static class named 'Solution'
                - Use a public static method called 'Solve'
                - Make sure it compiles on .NET 6 or higher
                - Use generic types in the method signature whereever possible
                - Include necessary using directives at the top
                - Do not use any external libraries or frameworks
                - Use standard C# libraries only
                - use async/await for async methods wherever possible
                - Only generate code that is valid C# code and compiles
                - Do not add any explanations without comment marker //
                - Optimize the algorithms for performance and readability and memory usage
                - Ensure the test project can find the 'Solution' class.
                ";
            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
