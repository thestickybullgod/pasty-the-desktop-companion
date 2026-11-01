using System.IO.Pipes;
using Avalonia.Threading;

namespace Pasty.Desktop;

/// <summary>
/// One Pasty per user session, on any OS. The running instance listens on a named pipe
/// (a Unix domain socket on Linux); a second launch connects, passes along what it was asked to do ("summon" or "settings"), and exits.
/// </summary>
internal static class SingleInstance
{
    private static readonly string PipeName = "pasty-" + Environment.UserName;

    /// <summary>Raised on the UI thread when another launch asks the running Pasty to come forward.</summary>
    public static event Action<string>? Summoned;

    public static bool TrySummonExisting(string message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(250);
            using var writer = new StreamWriter(client);
            writer.WriteLine(message);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Listen()
    {
        var thread = new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server);
                    if (await reader.ReadLineAsync() is { } message)
                        Dispatcher.UIThread.Post(() => Summoned?.Invoke(message));
                }
                catch (IOException)
                {
                    await Task.Delay(1000);
                }
            }
        })
        {
            IsBackground = true,
            Name = "Pasty single-instance listener",
        };
        thread.Start();
    }
}
