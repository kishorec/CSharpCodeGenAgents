using System.Configuration;
using System.Net;
using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    class Orchestrator
    {
        static readonly string ProjectDir = Path.Combine(Environment.CurrentDirectory, "generated");
        static readonly string DesignDocFile = ProjectDir + "/DesignDocument.md";
        static readonly string DesignDocHtmlFile = ProjectDir + "/DesignDocument.html";
        static readonly string CsProjDir = ProjectDir + "/CSProject";
        static readonly string TestProjDir = ProjectDir + "/TestProject";
        static readonly int MaxRetries = 10;

        static async Task Main()
        {
            await RunOrchestrator();
        }

        static async Task RunOrchestrator()
        {
            CleanupProjects();

            while (true)
            {
                Console.WriteLine("\nI’m an intelligent C# code generation agent—designed to write, test, and document clean code for your requests.\nWhat would you like me to build? (e.g., 'Reverse a string')\n> ");

                string taskDescription = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(taskDescription)) continue;
                if (taskDescription.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                try
                {
                    SetupProjects();

                    Console.WriteLine("\nStarting code generation...");
                    string code = await GenCodeAgent.GenerateAsync(taskDescription);

                    bool success = false;
                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        Console.WriteLine($"\nAttempt {attempt + 1} of {MaxRetries} — Validating generated code...");

                        Console.WriteLine("Generating test suite...");
                        string tests = await GenTestSuiteAgent.GenerateAsync(code);

                        try
                        {
                            File.WriteAllText(Path.Combine(CsProjDir, "Solution.cs"), code);
                            File.WriteAllText(Path.Combine(TestProjDir, "Tests.cs"), tests);
                        }
                        catch (IOException ioEx)
                        {
                            Console.WriteLine("Failed to write generated files: " + ioEx.Message);
                            break;
                        }

                        Console.WriteLine("Building the solution...");
                        if (!BuildProject(CsProjDir, out string buildErrors))
                        {
                            Console.WriteLine("Build failed. Attempting to fix...");
                            Console.WriteLine(buildErrors);
                            code = await FixCodeAgent.GenerateAsync(code, buildErrors);
                            await Task.Delay(200);
                            continue;
                        }

                        Console.WriteLine("Running tests...");
                        try
                        {
                            if (RunTests(TestProjDir, out string testErrors))
                            {
                                Console.WriteLine("All tests passed!");
                                Console.WriteLine("Generating design document...");

                                string designDoc = await GenDocAgent.GenerateAsync(code);
                                File.WriteAllText(DesignDocFile, designDoc);
                                File.WriteAllText(DesignDocHtmlFile, MarkdownToHtml(designDoc));

                                Console.WriteLine($"Design document saved: {DesignDocHtmlFile}");
                                Console.WriteLine($"Successfully completed: {taskDescription}");

                                success = true;
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Tests failed. Attempting to fix...");
                                Console.WriteLine(testErrors);
                                code = await FixCodeAgent.GenerateAsync(code, testErrors);
                                await Task.Delay(200);
                                continue;
                            }
                        }
                        catch (TimeoutException e)
                        {
                            Console.WriteLine("Tests timed out. Attempting to fix...");
                            code = await FixCodeAgent.GenerateAsync(code, "Tests did not run because they were hung and timed out. Exception:" + e.Message);
                            await Task.Delay(200);
                            continue;
                        }
                    }

                    if (!success)
                    {
                        Console.WriteLine($"\nMaximum retries reached. Unable to complete: {taskDescription}");
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception occurred: " + e.Message);
                    Console.WriteLine($"Failed to complete task: {taskDescription}");
                }
            }
        }

        static void SetupProjects()
        {
            Directory.CreateDirectory(ProjectDir);

            string appType = ConfigurationManager.AppSettings["APP_TYPE"] ?? "console";
            Console.WriteLine("System is configured to create Project for App type: " + appType);
            if (appType.Equals("winforms", StringComparison.OrdinalIgnoreCase))
            {
                Utils.Run("dotnet", $"new winforms -o {CsProjDir}");
                Utils.Run("dotnet", $"new nunit -o {TestProjDir}");

                string testProjPath = Path.Combine(TestProjDir, "TestProject.csproj");
                if (File.Exists(testProjPath))
                {
                    string csproj = File.ReadAllText(testProjPath);

                    if (csproj.Contains("<TargetFramework>net8.0</TargetFramework>") && !csproj.Contains("-windows"))
                    {
                        csproj = csproj.Replace("<TargetFramework>net8.0</TargetFramework>",
                            "<TargetFramework>net8.0-windows</TargetFramework>\n  <UseWindowsForms>true</UseWindowsForms>");
                        File.WriteAllText(testProjPath, csproj);
                    }
                }
            }
            else if (appType.Equals("web", StringComparison.OrdinalIgnoreCase))
            {
                Utils.Run("dotnet", "new web -o " + CsProjDir);
                Utils.Run("dotnet", "new nunit -o " + TestProjDir);
            }
            else
            {
                Utils.Run("dotnet", "new console -o " + CsProjDir);
                Utils.Run("dotnet", "new nunit -o " + TestProjDir);
            }

            Utils.RunIn(TestProjDir, "dotnet add reference ../CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet new sln -n AIProjects");
            Utils.RunIn(ProjectDir, "dotnet sln add CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet sln add TestProject/TestProject.csproj");
        }

        static void CleanupProjects()
        {
            if (Directory.Exists(ProjectDir))
            {
                try
                {
                    Directory.Delete(ProjectDir, true);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to delete output directory.");
                }
            }
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
            errorOutput = string.Empty;

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

        static string MarkdownToHtml(string md)
        {
            return $"<html><head><meta charset='utf-8'><title>Design Doc</title></head><body><pre>{md}</pre></body></html>";
        }

    }
}
