using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// What a built flyout was made from: the inputs that decide its layout, size and material. A request whose key
    /// equals the key the content was built with may be patched in place; any other must rebuild, because a patch
    /// would leave the window describing a structure its content does not have (audit F02, ADR-0002).
    /// <para>
    /// Only structure is here. Values that change on every tick (volume, titles, lock state), motion (<c>Ani</c>),
    /// dismissal time and monitor are not: they are carried by the request and never require a rebuild.
    /// </para>
    /// </summary>
    public readonly record struct FlyoutStructureKey(
        string Kind,
        string Style,
        int ScalePercent,
        bool ShowMedia,
        TesseraFlyoutSessionMode Topology,
        bool Acrylic,
        bool BackdropBlur,
        string Accent)
    {
        /// <summary>The key of <paramref name="request"/> as it would be built on <paramref name="topology"/>.</summary>
        public static FlyoutStructureKey From(FlyoutRequest request, TesseraFlyoutSessionMode topology)
        {
            IReadOnlyDictionary<string, string>? payload = request.Payload;
            return new FlyoutStructureKey(
                Kind: (request.Kind ?? string.Empty).Trim().ToLowerInvariant(),
                Style: StyleIds.Normalize(request.StyleId).ToLowerInvariant(),
                ScalePercent: (int)Math.Round(TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(payload) * 100),
                ShowMedia: TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(payload),
                Topology: topology,
                Acrylic: TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(payload),
                BackdropBlur: TesseraFlyoutRequestBuilder.BackdropBlurFromPayload(payload),
                Accent: TesseraFlyoutRequestBuilder.AccentFromPayload(payload) ?? string.Empty);
        }

        /// <summary>True when a flyout built with <paramref name="builtFrom"/> can take this request as a patch.</summary>
        public bool CanPatch(FlyoutStructureKey builtFrom)
        {
            return this == builtFrom;
        }

        /// <summary>The names of the fields that differ from <paramref name="other"/>, in a fixed order, for the log.</summary>
        public IReadOnlyList<string> DifferingFields(FlyoutStructureKey other)
        {
            List<string> names = [];
            AddIf(names, Kind != other.Kind, "kind");
            AddIf(names, Style != other.Style, "style");
            AddIf(names, ScalePercent != other.ScalePercent, "scale");
            AddIf(names, ShowMedia != other.ShowMedia, "showMedia");
            AddIf(names, Topology != other.Topology, "topology");
            AddIf(names, Acrylic != other.Acrylic, "acrylic");
            AddIf(names, BackdropBlur != other.BackdropBlur, "backdropBlur");
            AddIf(names, Accent != other.Accent, "accent");
            return names;
        }

        private static void AddIf(List<string> names, bool differs, string name)
        {
            if (differs)
            {
                names.Add(name);
            }
        }
    }
}
