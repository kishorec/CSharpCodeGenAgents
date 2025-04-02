namespace Microsoft.AzureDataEngineering.AI
{
    class FixCodeAgent
    {
        public static async Task<string> GenerateAsync(string code, string errors)
        {
            Console.WriteLine("'FixCode' agent is building the prompt for the task...");

            string prompt = $@"
                You are an expert C# software engineer and bug fixer. The following C# code has failed to build or pass unit tests.

                Here is the original code:
                {code}

                Here is the compiler/test error output:
                {errors}

                Objective:
                Fix the code so that it compiles successfully and passes **all unit tests**.

                Requirements:
                - Wrap the corrected method in a public static class named 'Solution'
                - Ensure the entry method is public static and named 'Solve'
                - Ensure the test project can locate and reference the 'Solution' class
                - If UI elements are present, separate them from business logic
                - Namespace must be: Microsoft.AzureDataEngineering.AI
                - Include all necessary `using` directives at the top
                - The solution must compile under .NET 6 or higher
                - Use async/await for any asynchronous logic
                - Prefer generic types in method signatures wherever appropriate

                Code Quality:
                - Use only standard C# libraries (no external frameworks or NuGet packages)
                - Avoid redundant logic, optimize for performance and readability
                - Add `//` comments only where needed to clarify fixes or logic
                - Do not include natural language explanations, markdown, or pseudocode
                - Output only valid, compilable C# source code

                Output:
                - One corrected code file that builds successfully and passes all associated NUnit tests
                ";

            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
