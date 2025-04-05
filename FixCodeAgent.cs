using System.Configuration;

namespace Microsoft.AzureDataEngineering.AI
{
    class FixCodeAgent
    {
        public static async Task<string> GenerateAsync(string code, string errors, string task)
        {
            if (code == null || errors == null || task == null)
            {
                throw new ArgumentNullException("Code, errors, and task cannot be null.");
            }

            Console.WriteLine("'FixCode' agent is building the prompt for the task...");
            Utils.TrimErrorsIfNeeded(ref code, ref errors, ref task);

            string prompt = $@"
                You are an expert C# software engineer and bug fixer. The following C# code has failed to build or pass unit tests.

                Here is the original code:
                {code}

                Here is the compiler/test error output:
                {errors}

                Here is the task that you have to solve using the code and errors provided above:
                {task}

                Objective:
                Fix the code so that it compiles successfully and passes **all unit tests**.

                Requirements:
                - Write clean, correct, and idiomatic C# code targeting .NET 6 or higher
                - All code must be valid and compile without modification

                Structure:
                - Place the core logic in a public static class named 'Solution'
                - Add an entry Main() method in the Solution class to demonstrate the functionality
                - If UI elements are present, separate them from business logic
                - Use the namespace: Microsoft.AzureData.Engineering.AI
                - Include all necessary `using` directives at the top
                - Ensure the test project can locate and reference the 'Solution' class

                Guidelines:
                - Use async/await for any asynchronous logic
                - Prefer generic types in method signatures wherever appropriate
                - Output only valid, compilable C# source code
                - Use standard C# libraries only (no external NuGet packages)
                - Avoid redundant logic
                - Write code with readability, performance, and maintainability in mind
                - Do not include natural language explanations, markdown, or pseudocode
                - Only include comments using `//` when necessary to clarify the code

                Output:
                - One corrected code that builds successfully and passes all associated NUnit tests

                Do not:
                - Include any text outside the code block
                - Output pseudocode, logs, or placeholders
                ";

            return await AzureOpenAI.AskAsync(prompt);
        }
    }
}
