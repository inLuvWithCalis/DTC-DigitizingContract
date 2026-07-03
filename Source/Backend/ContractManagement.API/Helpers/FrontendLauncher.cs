using System.Diagnostics;
using System.Net.Sockets;

namespace ContractManagement.API.Helpers;

public static class FrontendLauncher
{
    private static bool _started;

    public static void Start()
    {
#if !DEBUG
        return;
#endif

        if (_started)
            return;

        _started = true;

        // Nếu frontend đã chạy thì thôi
        if (IsPortOpen("localhost", 3000))
            return;

        var frontendPath = Path.GetFullPath(
      Path.Combine(
          AppContext.BaseDirectory,
          "..",
          "..",
          "..",
          "..",
          "..",
          "Frontend"));

        var process = new Process();
        Console.WriteLine(frontendPath);
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c npm run dev";
        process.StartInfo.WorkingDirectory = frontendPath;
        process.StartInfo.UseShellExecute = true;

        process.Start();
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(host, port);
            return true;
        }
        catch
        {
            return false;
        }
    }
}