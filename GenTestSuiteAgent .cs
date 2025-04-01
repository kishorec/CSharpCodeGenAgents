namespace Microsoft.AzureDataEngineering.AI
{
    class GenTestSuiteAgent
    {
        public static async Task<string> GenerateAsync(string code)
        {
            Console.WriteLine("'TestSuiteGen' agent is building the prompt for the task...");
            string prompt = $@"
                 You are an expert software developer. Write NUnit unit tests for the following C# class and method:
                {code}

                 Requirements:
                - Include [TestFixture] and [Test] attributes
                - Add at least 20 unit test cases
                - Include test for catching edge cases
                - Include fuzz testing for the APIs using every possible input
                - Add multithreaded tests wherever applicable
                - Add 'using NUnit.Framework;' at the top
                - Print the name of the test case in the beginning of each test method to show which test is running
                - Use 'Assert.That(actual, Is.EqualTo(expected))' for assertions
                - Only generate code that is valid C# code and compiles 
                - Do not add any explanations without comment marker //
                - Do not use any external libraries or frameworks
                - Use standard C# libraries only
                ";
            return await AzureOpenAI.AskAzureAsync(prompt);
        }
    }
}
