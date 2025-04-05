using System.Text;

namespace Microsoft.AzureDataEngineering.AI
{
    class Orchestrator
    {
        static readonly string ProjectDir = Path.Combine(Environment.CurrentDirectory, "generated");
        static readonly string DesignDocFile = Path.Combine(ProjectDir, "DesignDocument.md");
        static readonly string DesignDocHtmlFile = Path.Combine(ProjectDir, "DesignDocument.html");
        static readonly string CsProjDir = Path.Combine(ProjectDir, "CSProject");
        static readonly string TestProjDir = Path.Combine(ProjectDir, "TestProject");
        static readonly int MaxRetries = 10;

        static async Task Main() => await RunOrchestratorAsync();

        static async Task RunOrchestratorAsync()
        {
            CleanupProjects();

            Console.WriteLine("\nI’m an intelligent C# code generation agent—designed to write, test, and document clean code for your requests.\n");

            while (true)
            {
                Console.Write("What would you like me to build? (type 'exit' to quit)\n> ");
                string taskDescription = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(taskDescription))
                {
                    continue;
                }
                if (taskDescription.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                try
                {
                    SetupProjects();
                    if (!await TryGenerateAndTestCodeAsync(taskDescription))
                    {
                        Console.WriteLine($"\nMaximum retries reached. Unable to complete: {taskDescription}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nException: {ex.Message}\nFailed to complete: {taskDescription}");
                }
            }
        }

        static async Task<bool> TryGenerateAndTestCodeAsync(string taskDescription)
        {
            Console.WriteLine("\nStarting code generation...");
            string code = await GenCodeAgent.GenerateAsync(taskDescription);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                Console.WriteLine($"\nAttempt {attempt} of {MaxRetries} — Validating generated code...");

                try
                {
                    Console.WriteLine("\nStarting test suite generation...");
                    string tests = await GenTestSuiteAgent.GenerateAsync(code);

                    File.WriteAllText(Path.Combine(CsProjDir, "Solution.cs"), code);
                    File.WriteAllText(Path.Combine(TestProjDir, "Tests.cs"), tests);

                    Console.WriteLine("Building the solution...");
                    if (!BuildProject(CsProjDir, out var buildErrors))
                    {
                        Console.WriteLine("Build failed. Attempting fix...\n" + buildErrors);
                        code = await FixCodeAgent.GenerateAsync(code, buildErrors, taskDescription);
                        await Task.Delay(200);
                        continue;
                    }

                    Console.WriteLine("Running tests...");
                    if (RunTests(TestProjDir, out var testErrors))
                    {
                        Console.WriteLine("All tests passed! Generating design document...");
                        await GenerateDesignDocAsync(code);
                        Console.WriteLine($"Task completed: {taskDescription}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Tests failed. Attempting fix...\n" + testErrors);
                        code = await FixCodeAgent.GenerateAsync(code, testErrors, taskDescription);
                        await Task.Delay(200);
                        continue;
                    }
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("Tests timed out. Attempting fix...");
                    code = await FixCodeAgent.GenerateAsync(code, $"Tests timed out: {ex.Message}", taskDescription);
                    await Task.Delay(200);
                    continue;
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"File write error: {ioEx.Message}");
                    return false;
                }
            }
            return false;
        }

        static async Task GenerateDesignDocAsync(string code)
        {
            string designDoc = await GenDocAgent.GenerateAsync(code);
            File.WriteAllText(DesignDocFile, designDoc);
            File.WriteAllText(DesignDocHtmlFile, MarkdownToHtml(designDoc));
            Console.WriteLine($"Design document saved: {DesignDocHtmlFile}");
        }
        static string MarkdownToHtml(string md) =>
        $"<html><head><meta charset='utf-8'><title>Design Doc</title></head><body><pre>{md}</pre></body></html>";

        static void SetupProjects()
        {
            Directory.CreateDirectory(ProjectDir);

            string appType = AgentConfiguration.APP_TYPE.ToLower();
            Console.WriteLine($"Setting up project for app type: {appType}");

            switch (appType)
            {
                case "winforms":
                    Utils.Run("dotnet", $"new winforms -o {CsProjDir}");
                    CreateTestProject();
                    FixWinFormsTestProject();
                    break;

                case "web":
                    Utils.Run("dotnet", $"new web -o {CsProjDir}");
                    CreateTestProject();
                    break;

                default:
                    Utils.Run("dotnet", $"new console -o {CsProjDir}");
                    CreateTestProject();
                    break;
            }

            Utils.RunIn(TestProjDir, "dotnet add reference ../CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet new sln -n AIProjects");
            Utils.RunIn(ProjectDir, "dotnet sln add CSProject/CSProject.csproj");
            Utils.RunIn(ProjectDir, "dotnet sln add TestProject/TestProject.csproj");
        }

        static void CreateTestProject()
        {
            Utils.Run("dotnet", $"new nunit -o {TestProjDir}");
        }

        static void FixWinFormsTestProject()
        {
            string testProjPath = Path.Combine(TestProjDir, "TestProject.csproj");

            if (File.Exists(testProjPath))
            {
                string csprojContent = File.ReadAllText(testProjPath);
                if (csprojContent.Contains("<TargetFramework>net8.0</TargetFramework>") && !csprojContent.Contains("-windows"))
                {
                    csprojContent = csprojContent.Replace("<TargetFramework>net8.0</TargetFramework>",
                        "<TargetFramework>net8.0-windows</TargetFramework>\n  <UseWindowsForms>true</UseWindowsForms>");
                    File.WriteAllText(testProjPath, csprojContent);
                }
            }
        }

        static void CleanupProjects()
        {
            if (Directory.Exists(ProjectDir))
            {
                try
                {
                    Directory.Delete(ProjectDir, true);
                }
                catch
                {
                    Console.WriteLine("Warning: Failed to delete output directory.");
                }
            }
        }

        static bool BuildProject(string projectDir, out string errors) => RunCommand(projectDir, "dotnet build", out errors);

        static bool RunTests(string testProjDir, out string errors) => RunCommand(testProjDir, "dotnet test", out errors);

        static bool RunCommand(string dir, string command, out string output)
        {
            output = string.Empty;
            var process = Utils.RunIn(dir, command, out var errorMessage, out var outputMessage);

            output = errorMessage?.ToString() ?? outputMessage?.ToString() ?? string.Empty;
            return process.ExitCode == 0;
        }
    }
}
