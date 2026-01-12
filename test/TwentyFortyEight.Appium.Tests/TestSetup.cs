using System.Diagnostics;
using System.Net.Http;

// Disable parallelization for Appium tests as they share a single driver instance
[assembly: DoNotParallelize]

namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Assembly-level test setup that automatically starts required services.
/// </summary>
[TestClass]
public class TestSetup
{
    private static Process? _appiumProcess;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>
    /// Default iOS Simulator device name.
    /// </summary>
    public static string SimulatorDeviceName { get; } =
        Environment.GetEnvironmentVariable("IOS_SIMULATOR_NAME") ?? "iPhone 17 Pro";

    /// <summary>
    /// Default iOS platform version.
    /// </summary>
    public static string PlatformVersion { get; } =
        Environment.GetEnvironmentVariable("IOS_PLATFORM_VERSION") ?? "26.2";

    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        context.WriteLine("=== Appium Test Setup ===");

        // 1. Start Appium server if not running
        await EnsureAppiumServerRunning(context);

        // 2. Boot iOS Simulator if not running
        await EnsureSimulatorBooted(context);

        // 3. Build and install the app if needed
        await EnsureAppInstalled(context);

        context.WriteLine("=== Setup Complete ===");
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        // Stop Appium server if we started it
        if (_appiumProcess != null && !_appiumProcess.HasExited)
        {
            Console.WriteLine("Stopping Appium server...");
            _appiumProcess.Kill(entireProcessTree: true);
            _appiumProcess.Dispose();
            _appiumProcess = null;
        }
    }

    private static async Task EnsureAppiumServerRunning(TestContext context)
    {
        if (await IsAppiumRunning())
        {
            context.WriteLine("Appium server is already running.");
            return;
        }

        context.WriteLine("Starting Appium server...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "appium",
            Arguments = "server --port 4723 --relaxed-security",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            _appiumProcess = Process.Start(startInfo);

            if (_appiumProcess == null)
            {
                throw new InvalidOperationException("Failed to start Appium process.");
            }

            // Wait for Appium to be ready (up to 30 seconds)
            var timeout = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < timeout)
            {
                if (await IsAppiumRunning())
                {
                    context.WriteLine("Appium server started successfully.");
                    return;
                }

                await Task.Delay(1000);
            }

            throw new TimeoutException("Appium server failed to start within 30 seconds.");
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            throw new InvalidOperationException(
                "Failed to start Appium. Make sure Appium is installed: npm install -g appium && appium driver install xcuitest",
                ex
            );
        }
    }

    private static async Task<bool> IsAppiumRunning()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://127.0.0.1:4723/status");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnsureSimulatorBooted(TestContext context)
    {
        // Check if any simulator is booted
        var listResult = await RunCommandAsync("xcrun", "simctl list devices booted -j");

        if (listResult.Contains("\"state\" : \"Booted\""))
        {
            context.WriteLine("iOS Simulator is already booted.");
            return;
        }

        context.WriteLine($"Booting iOS Simulator: {SimulatorDeviceName}...");

        // Find the simulator UDID
        var allDevices = await RunCommandAsync("xcrun", "simctl list devices -j");

        // Try to boot by name
        var bootResult = await RunCommandAsync(
            "xcrun",
            $"simctl boot \"{SimulatorDeviceName}\"",
            throwOnError: false
        );

        if (bootResult.Contains("Unable to boot") || bootResult.Contains("Invalid device"))
        {
            // Try to find any available iPhone simulator
            context.WriteLine(
                $"Could not find '{SimulatorDeviceName}', looking for any available iPhone..."
            );
            var fallbackBoot = await RunCommandAsync(
                "bash",
                "-c \"xcrun simctl list devices available | grep 'iPhone' | head -1 | sed 's/.*(//' | sed 's/).*//' | xargs xcrun simctl boot\"",
                throwOnError: false
            );
        }

        // Wait for simulator to fully boot
        await Task.Delay(5000);
        context.WriteLine("iOS Simulator booted.");
    }

    private static async Task EnsureAppInstalled(TestContext context)
    {
        var appPath = GetAppPath();

        if (!Directory.Exists(appPath))
        {
            context.WriteLine($"App not found at {appPath}, building...");
            await BuildApp(context);
        }
        else
        {
            context.WriteLine($"App found at {appPath}");
        }

        // Install the app on the simulator
        context.WriteLine("Installing app on simulator...");
        await RunCommandAsync("xcrun", $"simctl install booted \"{appPath}\"", throwOnError: false);
        context.WriteLine("App installed.");
    }

    private static async Task BuildApp(TestContext context)
    {
        context.WriteLine("Building iOS app for simulator...");

        var projectPath = GetProjectPath();
        var result = await RunCommandAsync(
            "dotnet",
            $"build \"{projectPath}\" -f net10.0-ios -c Debug",
            throwOnError: true,
            timeoutSeconds: 300
        );

        context.WriteLine("Build completed.");
    }

    private static string GetAppPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "TwentyFortyEight.Maui",
                "bin",
                "Debug",
                "net10.0-ios",
                "iossimulator-arm64",
                "TwentyFortyEight.Maui.app"
            )
        );
    }

    private static string GetProjectPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "TwentyFortyEight.Maui",
                "TwentyFortyEight.Maui.csproj"
            )
        );
    }

    private static async Task<string> RunCommandAsync(
        string command,
        string arguments,
        bool throwOnError = true,
        int timeoutSeconds = 60
    )
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Command '{command} {arguments}' timed out.");
        }

        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{command} {arguments}' failed with exit code {process.ExitCode}:\n{error}"
            );
        }

        return output.ToString() + error.ToString();
    }
}
