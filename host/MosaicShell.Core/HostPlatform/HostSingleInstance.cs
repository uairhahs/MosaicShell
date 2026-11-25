using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// One Host per data folder. The first launch holds <c>host.lock</c> in the folder (an exclusive open on Windows, an
    /// flock on Linux, so the OS releases it if the Host dies) and listens on a local pipe; a later launch finds the
    /// lock held, asks the first to show its Hub through the pipe, and exits rather than arming a second set of
    /// capabilities. Separate data folders (a throwaway <c>MOSAICSHELL_HOME</c>) each get their own Host.
    /// </summary>
    /// <remarks>
    /// The request carries nothing but "show", so a stray connection can at most bring the Hub forward. On Linux the pipe
    /// is a socket file in the temp folder; one left by a Host that died is replaced when the next one listens.
    /// ActivationRequested is raised on a thread-pool thread.
    /// </remarks>
    public sealed class HostSingleInstance : IDisposable
    {
        public const string LockFileName = "host.lock";
        private const string ShowRequest = "show";

        private readonly FileStream _lock;
        private readonly string _pipeName;
        private readonly CancellationTokenSource _stop = new();
        private int _disposed;

        private HostSingleInstance(FileStream lockStream, string pipeName)
        {
            _lock = lockStream;
            _pipeName = pipeName;
        }

        public event EventHandler? ActivationRequested;

        /// <summary>The single instance for <paramref name="dataDirectory"/>, or null when another Host already holds it.</summary>
        public static HostSingleInstance? TryAcquire(string dataDirectory)
        {
            _ = Directory.CreateDirectory(dataDirectory);
            try
            {
                FileStream stream = new(
                    Path.Combine(dataDirectory, LockFileName),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
                return new HostSingleInstance(stream, PipeNameFor(dataDirectory));
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// Asks the Host holding <paramref name="dataDirectory"/> to show itself; false when none answered in time.
        /// Synchronous: a second launch runs this before any UI exists, from a <c>Main</c> that stays STA.
        /// </summary>
        public static bool SignalPrimary(string dataDirectory, TimeSpan timeout)
        {
            try
            {
                using NamedPipeClientStream client = new(".", PipeNameFor(dataDirectory), PipeDirection.Out);
                client.Connect((int)timeout.TotalMilliseconds);
                client.Write(Encoding.UTF8.GetBytes(ShowRequest + "\n"));
                client.Flush();
                return true;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>A glint capture writes its images and exits, so it runs beside a Host instead of deferring to it.</summary>
        public static bool AppliesTo(HostLaunchMode mode)
        {
            return mode is HostLaunchMode.FullHub or HostLaunchMode.TrayOnly;
        }

        /// <summary>
        /// A second launch made for the Hub brings the running Hub forward; one made for the tray (a login entry) leaves the
        /// running Host as it is.
        /// </summary>
        public static bool ShouldSignal(HostLaunchMode mode)
        {
            return mode == HostLaunchMode.FullHub;
        }

        /// <summary>Starts answering later launches; each request raises <see cref="ActivationRequested"/>.</summary>
        public void StartListening()
        {
            _ = Task.Run(ListenAsync);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _stop.Cancel();
            _lock.Dispose();
            _stop.Dispose();
        }

        /// <summary>Per data folder, so two folders never share a pipe; short enough for a Unix socket path.</summary>
        internal static string PipeNameFor(string dataDirectory)
        {
            string full = Path.GetFullPath(dataDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (OperatingSystem.IsWindows())
            {
                full = full.ToUpperInvariant();
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(full));
            return "MosaicShell.Activate." + Convert.ToHexStringLower(hash.AsSpan(0, 8));
        }

        private async Task ListenAsync()
        {
            CancellationToken token = _stop.Token;
            NamedPipeServerStream? next = null;
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream server = next ?? CreateServer();
                next = null;
                try
                {
                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    using (StreamReader reader = new(server, Encoding.UTF8, leaveOpen: true))
                    {
                        if (await reader.ReadLineAsync(token).ConfigureAwait(false) == ShowRequest)
                        {
                            Raise();
                        }
                    }

                    // Open the next instance before closing this one. On Unix the instances share one listening socket,
                    // and a launch that connected meanwhile waits in its queue; closing the last instance first would close
                    // the listener and drop that launch's request (as 765be30f found for the capability pipe).
                    next = CreateServer();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (IOException ex)
                {
                    // A client that hung up mid-request; keep listening.
                    Debug.WriteLine($"[SingleInstance] {ex.Message}");
                }
                finally
                {
                    await server.DisposeAsync().ConfigureAwait(false);
                }
            }

            if (next is not null)
            {
                await next.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Two instances, so the next can open while the last request's is still closing.</summary>
        private NamedPipeServerStream CreateServer()
        {
            return new NamedPipeServerStream(_pipeName, PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        private void Raise()
        {
            try
            {
                ActivationRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not end the listener.
                Debug.WriteLine($"[SingleInstance] handler: {ex.Message}");
            }
        }
    }
}
