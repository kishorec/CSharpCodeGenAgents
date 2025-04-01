using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    class Orchestrator
    {
        static readonly string ProjectDir = Path.Combine(Environment.CurrentDirectory, "generated");
        static readonly  string DesignDocFile = ProjectDir + "/DesignDocument.md";
        static readonly  string DesignDocHtmlFile = ProjectDir + "/DesignDocument.html";
        static readonly  string CsProjDir = ProjectDir + "/CSProject";
        static readonly  string TestProjDir = ProjectDir + "/TestProject";
        static readonly  int MaxRetries = 10;

        static async Task Main()
        {
            SetupProjects();

            while (true)
            {
                Console.Write("\nEnter your C# task (e.g., 'Reverse a string'):\n> ");
                string taskDescription = Console.ReadLine() ?? String.Empty;
                if (String.IsNullOrEmpty(taskDescription))
                {
                    continue;
                }

                if (taskDescription.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                try
                {
                    Console.WriteLine("\nStarting code generation...");
                    string code = await GenCodeAgent.GenerateAsync(taskDescription);

                    bool success = false;
                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        Console.WriteLine($"Validating generated code. Attempt {attempt + 1} of {MaxRetries}");
                        Console.WriteLine("Starting test suite generation...");
                        string tests = await GenTestSuiteAgent.GenerateAsync(code);

                        Console.WriteLine("Adding generated files to the VS project and solution files...");
                        File.WriteAllText(Path.Combine(CsProjDir, "Solution.cs"), code);
                        File.WriteAllText(Path.Combine(TestProjDir, "Tests.cs"), tests);

                        Console.WriteLine("Building the solution...");
                        if (!BuildProject(CsProjDir, out string buildErrors))
                        {
                            Console.WriteLine("!!! Build failed. Attempting to fix...");
                            Console.WriteLine(buildErrors);
                            code = await FixCodeAgent.GenerateAsync(code, buildErrors);
                            continue;
                        }

                        Console.WriteLine("Running the tests...");
                        if (RunTests(TestProjDir, out string testErrors))
                        {
                            Console.WriteLine("All tests passed!");
                            Console.WriteLine("Generating design document...");
                            string designDoc = await GenDocAgent.GenerateAsync(code);
                            File.WriteAllText(DesignDocFile, designDoc);
                            File.WriteAllText(DesignDocHtmlFile, Utils.MarkdownToHtml(designDoc));
                            Console.WriteLine($"Design document saved. File: {DesignDocHtmlFile}");

                            success = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("\n!!! Tests failed with following errors. Attempting to fix...");
                            Console.WriteLine();
                            Console.WriteLine(testErrors);
                            code = await FixCodeAgent.GenerateAsync(code, testErrors);
                            Thread.Sleep(1);
                            continue;
                        }
                    }

                    if (!success)
                    {
                        Console.WriteLine($"\nMaximum retries reached. Unable to produce working code for Task={taskDescription}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine($"\nFailed to produce working code for Task={taskDescription}");
                }

            }
        }

        static void SetupProjects()
        {
            try
            {
                //if (Directory.Exists(ProjectDir))
                //{
                //    Directory.Delete(ProjectDir, true);
                //}
            }
            catch (Exception)
            {
                //Ignore exception
            }

            Directory.CreateDirectory(ProjectDir);
            Utils.Run("dotnet", "new web -o " + CsProjDir);
            Utils.Run("dotnet", "new nunit -o " + TestProjDir);
            Utils.RunIn(TestProjDir, "dotnet add reference ../CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet new sln -n AIProjects");
            Utils.RunIn(ProjectDir, "dotnet sln add CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet sln add TestProject/TestProject.csproj");
        }

        static bool BuildProject(string projectDir, out string errorOutput)
        {
            return Run(projectDir, "dotnet build", out errorOutput);
        }

        static bool RunTests(string testProjDir, out string errorOutput)
        {
            return Run(testProjDir, "dotnet test", out errorOutput);
        }

        static bool Run(string dir, string command, out string errorOutput)
        {
            errorOutput = String.Empty;
            StringBuilder? outputMessage = null;
            StringBuilder? errorMessage = null;
            var process = Utils.RunIn(dir, command, out errorMessage, out outputMessage);
            if (errorMessage != null)
            {
                errorOutput = errorMessage.ToString();
            }
            else if (outputMessage != null)
            {
                errorOutput = outputMessage.ToString();
            }
            return process.ExitCode == 0;
        }

    }
}