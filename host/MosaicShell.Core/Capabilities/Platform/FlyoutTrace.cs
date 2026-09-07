namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Diagnostic sink for flyout routing decisions.
    /// <para>
    /// Core decides Present / Patch / SoftRefresh but owns no logging, so a routing decision could
    /// previously only be inferred from its Host-side effect (a <c>Show</c> vs <c>Update</c> line).
    /// That inference is ambiguous exactly where it matters: the same trigger resolves differently
    /// depending on a visibility answer that Core reads from Host and then discards. Host wires
    /// <see cref="Sink"/> to its flyout log so the decision and its inputs are recorded together.
    /// </para>
    /// No-op until wired, so Core keeps no dependency on any Host logging type.
    /// </summary>
    public static class FlyoutTrace
    {
        public static Action<string>? Sink { get; set; }

        public static bool IsEnabled => Sink is not null;

        public static void Write(string message)
        {
            try
            {
                Sink?.Invoke(message);
            }
            catch
            {
                // Diagnostics must never break routing.
            }
        }
    }
}
