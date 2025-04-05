namespace Microsoft.AzureDataEngineering.AI
{
    class GenTestSuiteAgent
    {
        public static async Task<string> GenerateAsync(string code)
        {
            if (code == null)
            {
                throw new ArgumentNullException("Code cannot be null.");
            }

            Console.WriteLine("'TestSuiteGen' agent is building the prompt for the task...");
            string prompt = $@"
                You are an expert C# developer and test engineer. Your task is to write comprehensive NUnit unit tests for the following C# class and method:

                {code}

                Test Requirements:
                - Use [TestFixture] for the test class
                - Use [Test] for each test method
                - Add 'using NUnit.Framework;' at the top
                - Use namespace: Microsoft.AzureData.Engineering.AI
                - Do not include tests for any Windows Forms or UI classes

                Coverage Expectations:
                - Add **at least 20 unique test cases** for full code coverage
                - Cover normal cases, boundary values, and extreme edge cases
                - Include fuzz tests with randomized or unexpected inputs where applicable
                - Add **multi-threaded test cases** if the logic involves concurrency or shared state

                Test Behavior:
                - Each test method should begin with a comment or log indicating its name
                - Use `Assert.That(actual, Is.EqualTo(expected))` for all assertions
                - Group similar test cases together using meaningful method names

                Output Constraints:
                - Only output **valid, compilable C# code**
                - Use only standard C# libraries (no external packages)
                - Do not include any explanations, markdown, or descriptive text
                - Add `//` comments only when needed to clarify test intent
                - Do not generate pseudocode or test stubs — write complete, runnable code

                Output:
                - One complete C# file containing only NUnit tests following the rules above
                ";
            return await AzureOpenAI.AskAsync(prompt);
        }
    }
}
