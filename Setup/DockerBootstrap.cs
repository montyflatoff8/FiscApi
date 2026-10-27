using System.ComponentModel;
using System.Diagnostics;

namespace FiscApi.Setup;

/// <summary>
/// Development convenience (a slimmer version of the class API's DevelopmentBootstrap):
/// before the app connects to SQL Server, make sure Docker is running and the container is
/// healthy. Runs only when Program.cs calls it (Development + SqlServer + AutoStartDocker).
/// Uses Console.WriteLine because app.Logger doesn't exist yet this early in startup.
/// </summary>
public static class DockerBootstrap
{
    private const string Remediation =
        "Start Docker Desktop and run 'docker compose up -d' yourself, or set DataSource:AutoStartDocker to false.";

    public static void EnsureSqlContainer(string contentRootPath)
    {
        if (!File.Exists(Path.Combine(contentRootPath, "compose.yaml")))
        {
            throw new InvalidOperationException($"compose.yaml not found in {contentRootPath}. {Remediation}");
        }

        EnsureDockerEngine();

        // --wait blocks until the healthcheck in compose.yaml passes, so the API never starts
        // talking to a database that is still booting. Already running = returns in ~1 second.
        Console.WriteLine("Starting SQL Server container (docker compose up -d --wait)...");
        var (exitCode, error) = RunDocker(contentRootPath, TimeSpan.FromMinutes(3), "compose", "up", "-d", "--wait");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"docker compose failed (exit {exitCode}): {error} {Remediation}");
        }

        Console.WriteLine("SQL Server container is healthy.");
    }

    // "docker info" only succeeds when the Docker engine is up. If it isn't, launch Docker
    // Desktop and poll until it is (first start after a reboot takes 30-60 seconds).
    private static void EnsureDockerEngine()
    {
        if (RunDocker(null, TimeSpan.FromSeconds(20), "info").ExitCode == 0)
        {
            return;
        }

        var desktopExe = FindDockerDesktop()
            ?? throw new InvalidOperationException($"Docker is not running and Docker Desktop wasn't found. {Remediation}");

        Console.WriteLine("Docker is not running. Starting Docker Desktop (this can take a minute)...");
        Process.Start(new ProcessStartInfo(desktopExe) { UseShellExecute = true });

        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            if (RunDocker(null, TimeSpan.FromSeconds(20), "info").ExitCode == 0)
            {
                Console.WriteLine("Docker engine is running.");
                return;
            }
        }

        throw new InvalidOperationException($"Docker Desktop did not become ready within 3 minutes. {Remediation}");
    }

    // docker.exe lives in <install>\resources\bin, and Docker Desktop.exe in <install>.
    // Works for both the per-user install (AppData\Local\Programs) and the all-users one (Program Files).
    private static string? FindDockerDesktop()
    {
        var candidates = new List<string>();

        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);
        foreach (var dir in pathDirs.Where(d => File.Exists(Path.Combine(d, "docker.exe"))))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(dir, "..", "..", "Docker Desktop.exe")));
        }

        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "DockerDesktop", "Docker Desktop.exe"));
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Docker", "Docker", "Docker Desktop.exe"));

        return candidates.FirstOrDefault(File.Exists);
    }

    private static (int ExitCode, string Error) RunDocker(string? workingDirectory, TimeSpan timeout, params string[] args)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            return (-1, "docker command not found");   // Docker isn't installed or isn't on PATH
        }

        var stdout = process.StandardOutput.ReadToEndAsync();   // drained so the pipe can't fill and hang
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeout))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return (-1, $"timed out after {timeout.TotalSeconds:0}s");
        }

        return (process.ExitCode, stderr.Result.Trim());
    }
}
