using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Turns the property maps of org.mpris.MediaPlayer2 and org.mpris.MediaPlayer2.Player into an
    /// <see cref="MprisPlayerState"/>. Players disagree on types (length as int64, int32, uint64 or double; trackid as
    /// object path or string; artist as array or string), so every field is read leniently and a field of an
    /// unexpected type is skipped, never thrown on.
    /// </summary>
    public static class MprisVariantDecoder
    {
        public static MprisPlayerState Decode(
            string busName,
            IReadOnlyDictionary<string, VariantValue> root,
            IReadOnlyDictionary<string, VariantValue> player,
            int? ownerPid)
        {
            IReadOnlyDictionary<string, VariantValue> metadata = Metadata(player);
            return new MprisPlayerState(busName)
            {
                Identity = String(root, "Identity"),
                DesktopEntry = String(root, "DesktopEntry"),
                PlaybackStatus = String(player, "PlaybackStatus") switch
                {
                    "Playing" => MprisPlaybackStatus.Playing,
                    "Paused" => MprisPlaybackStatus.Paused,
                    _ => MprisPlaybackStatus.Stopped,
                },
                TrackId = String(metadata, "mpris:trackid"),
                Title = String(metadata, "xesam:title"),
                Artists = Strings(metadata, "xesam:artist"),
                ArtUrl = String(metadata, "mpris:artUrl"),
                LengthMicros = Integer(metadata, "mpris:length") ?? 0,
                PositionMicros = Integer(player, "Position") ?? 0,
                Shuffle = Bool(player, "Shuffle"),
                LoopStatus = String(player, "LoopStatus"),
                CanSeek = Bool(player, "CanSeek") ?? false,
                OwnerPid = ownerPid,
                MirroredPid = Integer(metadata, "kde:pid") is { } pid and > 0 and <= int.MaxValue ? (int)pid : null,
            };
        }

        /// <summary>A Position reply (a variant holding an integer), or null.</summary>
        public static long? Integer(VariantValue value)
        {
            VariantValue v = Unwrap(value);
            return v.Type switch
            {
                VariantValueType.Int64 => v.GetInt64(),
                VariantValueType.Int32 => v.GetInt32(),
                VariantValueType.UInt32 => v.GetUInt32(),
                VariantValueType.UInt64 => v.GetUInt64() <= long.MaxValue ? (long)v.GetUInt64() : null,
                VariantValueType.Int16 => v.GetInt16(),
                VariantValueType.UInt16 => v.GetUInt16(),
                VariantValueType.Byte => v.GetByte(),
                VariantValueType.Double => double.IsFinite(v.GetDouble()) ? (long)Math.Round(v.GetDouble()) : null,
                _ => null,
            };
        }

        private static IReadOnlyDictionary<string, VariantValue> Metadata(IReadOnlyDictionary<string, VariantValue> player)
        {
            if (!player.TryGetValue("Metadata", out VariantValue raw))
            {
                return new Dictionary<string, VariantValue>();
            }

            VariantValue dict = Unwrap(raw);
            if (dict.Type != VariantValueType.Dictionary || dict.KeyType != VariantValueType.String)
            {
                return new Dictionary<string, VariantValue>();
            }

            Dictionary<string, VariantValue> result = [with(StringComparer.Ordinal)];
            for (int i = 0; i < dict.Count; i++)
            {
                KeyValuePair<VariantValue, VariantValue> entry = dict.GetDictionaryEntry(i);
                result[entry.Key.GetString()] = entry.Value;
            }

            return result;
        }

        private static VariantValue Unwrap(VariantValue value)
        {
            while (value.Type == VariantValueType.Variant)
            {
                value = value.GetVariantValue();
            }

            return value;
        }

        private static string? String(IReadOnlyDictionary<string, VariantValue> map, string key)
        {
            if (!map.TryGetValue(key, out VariantValue raw))
            {
                return null;
            }

            VariantValue v = Unwrap(raw);
            return v.Type switch
            {
                VariantValueType.String => v.GetString(),
                VariantValueType.ObjectPath => v.GetObjectPathAsString(),
                _ => null,
            };
        }

        private static IReadOnlyList<string> Strings(IReadOnlyDictionary<string, VariantValue> map, string key)
        {
            if (!map.TryGetValue(key, out VariantValue raw))
            {
                return [];
            }

            VariantValue v = Unwrap(raw);
            if (v.Type == VariantValueType.String)
            {
                return [v.GetString()];
            }

            if (v.Type != VariantValueType.Array)
            {
                return [];
            }

            List<string> items = [];
            for (int i = 0; i < v.Count; i++)
            {
                VariantValue item = Unwrap(v.GetItem(i));
                if (item.Type == VariantValueType.String)
                {
                    items.Add(item.GetString());
                }
            }

            return items;
        }

        private static long? Integer(IReadOnlyDictionary<string, VariantValue> map, string key)
        {
            return map.TryGetValue(key, out VariantValue raw) ? Integer(raw) : null;
        }

        private static bool? Bool(IReadOnlyDictionary<string, VariantValue> map, string key)
        {
            if (!map.TryGetValue(key, out VariantValue raw))
            {
                return null;
            }

            VariantValue v = Unwrap(raw);
            return v.Type == VariantValueType.Bool ? v.GetBool() : null;
        }
    }
}
