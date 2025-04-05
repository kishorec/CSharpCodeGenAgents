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

        static async Task Main() => await RunOrchestratorAsync();

        static async Task RunOrchestratorAsync()
        {
            await CleanupProjectsAsync();

            PrintWelcomeBanner();

            while (true)
            {
                Console.Write("What would you like me to build? (type 'exit' to quit)\n> ");
                string taskDescription = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(taskDescription))
                    continue;

                if (taskDescription.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                try
                {
                    await SetupVisualStudioProjectsAsync();
                    if (!await TryGenerateAndTestCodeAsync(taskDescription))
                    {
                        Console.WriteLine($"\nMaximum retries reached. Unable to complete: {taskDescription}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nException: {ex.Message}\nFailed to complete: {taskDescription}");
                }

                Console.WriteLine();
            }
        }

        static async Task<bool> TryGenerateAndTestCodeAsync(string taskDescription)
        {
            Console.WriteLine("\nStarting code generation...");
            string code = await GenCodeAgent.GenerateAsync(taskDescription);

            int maxRetries = AgentConfiguration.MAX_NUMBER_OF_CODEGEN_RETRIES;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Console.WriteLine($"\nAttempt {attempt} of {maxRetries} — Validating generated code...");

                try
                {
                    Console.WriteLine("\nStarting test suite generation...");
                    string tests = await GenTestSuiteAgent.GenerateAsync(code);

                    await File.WriteAllTextAsync(Path.Combine(CsProjDir, "Solution.cs"), code);
                    await File.WriteAllTextAsync(Path.Combine(TestProjDir, "Tests.cs"), tests);

                    Console.WriteLine("\nBuilding the solution...");
                    var (buildSuccess, buildErrors) = await BuildProjectAsync(CsProjDir);
                    if (!buildSuccess)
                    {
                        Console.WriteLine("Build failed. Attempting fix...");
                        Console.WriteLine(buildErrors);
                        code = await FixCodeAgent.GenerateAsync(code, buildErrors, taskDescription);
                        await Task.Delay(200);
                        continue;
                    }

                    Console.WriteLine("\nRunning tests...");
                    var (testSuccess, testErrors) = await RunTestsAsync(TestProjDir);
                    if (!testSuccess)
                    {
                        Console.WriteLine("Tests failed. Attempting fix...");
                        Console.WriteLine(testErrors);
                        code = await FixCodeAgent.GenerateAsync(code, testErrors, taskDescription);
                        await Task.Delay(200);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("All tests passed!");
                        Console.WriteLine("\nGenerating design document...");
                        await GenerateDesignDocAsync(code);
                        Console.WriteLine($"\nTask completed successfully.");
                        Console.WriteLine($"Number of attempts: {attempt}");
                        Console.WriteLine($"Task Descriptino: {taskDescription}");
                        return true;
                    }
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Tests timed out. Attempting fix...\n{ex.Message}");
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
            await File.WriteAllTextAsync(DesignDocFile, designDoc);
            await File.WriteAllTextAsync(DesignDocHtmlFile, MarkdownToHtml(designDoc));
            Console.WriteLine($"Design document saved: {DesignDocHtmlFile}");
        }

        static string MarkdownToHtml(string md) =>
            $"<html><head><meta charset='utf-8'><title>Design Doc</title></head><body><pre>{md}</pre></body></html>";

        static async Task SetupVisualStudioProjectsAsync()
        {
            Console.WriteLine();
            Directory.CreateDirectory(ProjectDir);

            string appType = AgentConfiguration.APP_TYPE.ToLower();
            Console.WriteLine($"Setting up Visual Studio project for app type: {appType}");

            if (appType == "winforms")
            {
                await Utils.RunAsync("dotnet", $"new winforms -o {CsProjDir}");
                await CreateTestProjectAsync();
                await FixWinFormsTestProjectAsync();
            }
            else if (appType == "web")
            {
                await Utils.RunAsync("dotnet", $"new web -o {CsProjDir}");
                await CreateTestProjectAsync();
            }
            else
            {
                await Utils.RunAsync("dotnet", $"new console -o {CsProjDir}");
                await CreateTestProjectAsync();
            }

            await Utils.RunInAsync(TestProjDir, "dotnet add reference ../CSProject/CSProject.csproj");
            await Utils.RunInAsync(ProjectDir, "dotnet new sln -n AIProjects");
            await Utils.RunInAsync(ProjectDir, "dotnet sln add CSProject/CSProject.csproj");
            await Utils.RunInAsync(ProjectDir, "dotnet sln add TestProject/TestProject.csproj");
        }

        static async Task CreateTestProjectAsync()
        {
            await Utils.RunAsync("dotnet", $"new nunit -o {TestProjDir}");
        }

        static async Task FixWinFormsTestProjectAsync()
        {
            string testProjPath = Path.Combine(TestProjDir, "TestProject.csproj");

            if (File.Exists(testProjPath))
            {
                string csprojContent = await File.ReadAllTextAsync(testProjPath);
                if (csprojContent.Contains("<TargetFramework>net8.0</TargetFramework>") && !csprojContent.Contains("-windows"))
                {
                    csprojContent = csprojContent.Replace("<TargetFramework>net8.0</TargetFramework>",
                        "<TargetFramework>net8.0-windows</TargetFramework>\n  <UseWindowsForms>true</UseWindowsForms>");
                    await File.WriteAllTextAsync(testProjPath, csprojContent);
                }
            }
        }

        static async Task CleanupProjectsAsync()
        {
            if (Directory.Exists(ProjectDir))
            {
                try
                {
                    Directory.Delete(ProjectDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to delete output directory. {ex.Message}");
                }
            }
            await Task.CompletedTask;
        }

        static async Task<(bool success, string output)> BuildProjectAsync(string projectDir)
        {
            return await Utils.RunCommandAsync(projectDir, "dotnet build");
        }

        static async Task<(bool success, string output)> RunTestsAsync(string testProjDir)
        {
            return await Utils.RunCommandAsync(testProjDir, "dotnet test");
        }

        private static void PrintWelcomeBanner()
        {
            Console.WriteLine("\nWelcome to the AI Code Generation Agent!");
            Console.WriteLine("========================================");
            Console.WriteLine("\nI’m an intelligent C# code generation agent—designed to write, test, and document clean code for your requests.\n");
        }

    }
}
