using System.IO.Pipes;
using MosaicShell.Core.Services.BrowserBridge;
using Relay = MosaicShell.Core.Services.BrowserBridge.BrowserRelay;

namespace MosaicShell.BrowserRelay
{
    /// <summary>
    /// The native messaging host registered for the MosaicShell browser extension. The browser starts it and talks to
    /// it over standard input and output; it forwards to the Host over a per-user pipe. Standard output belongs to the
    /// protocol, so nothing else may ever be written to it.
    /// </summary>
    internal static class Program
    {
        public static async Task<int> Main()
        {
            string pipeName = BrowserPipeServer.PipeNameForCurrentUser();
            await using Stream input = Console.OpenStandardInput();
            await using Stream output = Console.OpenStandardOutput();

            Relay relay = new(input, output, cancellationToken => ConnectAsync(pipeName, cancellationToken));
            await relay.RunAsync(CancellationToken.None);
            return 0;
        }

        /// <summary>Connects to the Host, or returns null when it is not running so the relay waits and tries again.</summary>
        private static async Task<Stream?> ConnectAsync(string pipeName, CancellationToken cancellationToken)
        {
            // CurrentUserOnly also checks that the server on the other end belongs to this user.
            NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            try
            {
                await pipe.ConnectAsync(2000, cancellationToken);
                return pipe;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                await pipe.DisposeAsync();
                return null;
            }
        }
    }
}
