using System.Text.Json;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The browser starts the relay only if the account has registered it as a native messaging host, and only lets
    /// the extensions named in that registration talk to it. The Host does this itself at startup, for the current
    /// user only, so nothing needs an installer, administrator rights, or a manual step.
    /// </summary>
    public sealed class NativeHostRegistrationTests : IDisposable
    {
        private const string EdgeKey = @"Software\Microsoft\Edge\NativeMessagingHosts\com.mosaicshell.grout";
        private const string ChromeKey = @"Software\Google\Chrome\NativeMessagingHosts\com.mosaicshell.grout";

        private readonly string _root = Path.Combine(Path.GetTempPath(), "mosaic-native-host-" + Guid.NewGuid().ToString("N"));
        private readonly FakeUserRegistry _registry = new();

        public NativeHostRegistrationTests()
        {
            _ = Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            Directory.Delete(_root, recursive: true);
        }

        private string Relay()
        {
            string path = Path.Combine(_root, "install", "MosaicShell.BrowserRelay.exe");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "");
            return path;
        }

        private string DataDir()
        {
            return Path.Combine(_root, "data");
        }

        [Fact]
        public void The_manifest_names_the_relay_and_the_extensions_allowed_to_start_it()
        {
            string relay = Relay();

            bool registered = NativeHostRegistration.Register(relay, DataDir(), _registry);

            _ = registered.Should().BeTrue();
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(_registry.Values[EdgeKey]));
            JsonElement root = manifest.RootElement;
            _ = root.GetProperty("name").GetString().Should().Be("com.mosaicshell.grout");
            _ = root.GetProperty("type").GetString().Should().Be("stdio");
            _ = root.GetProperty("path").GetString().Should().Be(relay);
            string[] origins = [.. root.GetProperty("allowed_origins").EnumerateArray().Select(o => o.GetString()!)];
            _ = origins.Should().NotBeEmpty();
            _ = origins.Should().OnlyContain(o => o.StartsWith("chrome-extension://", StringComparison.Ordinal) && o.EndsWith('/') && !o.Contains('*'),
                "a wildcard would let any extension start the relay");
            _ = origins.Should().Contain("chrome-extension://" + NativeHostRegistration.AllowedExtensionIds[0] + "/");
        }

        [Fact]
        public void The_published_store_copy_of_Grout_is_allowed_to_start_the_relay()
        {
            _ = NativeHostRegistration.Register(Relay(), DataDir(), _registry);

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(_registry.Values[EdgeKey]));
            string[] origins = [.. manifest.RootElement.GetProperty("allowed_origins").EnumerateArray().Select(o => o.GetString()!)];
            _ = origins.Should().Contain("chrome-extension://pcjkacalabdgejinbmfdejicfhlonnpf/",
                "the web store assigns its own ID, and a copy the manifest does not name is refused with 'does not trust this copy of Grout'");
            _ = origins.Should().Contain("chrome-extension://aaffcapodpfecchmelidkkhgiaamijpe/", "an unpacked copy made before the store key was added must not be locked out");
        }

        [Fact]
        public void Every_allowed_extension_id_is_a_well_formed_browser_extension_id()
        {
            _ = NativeHostRegistration.AllowedExtensionIds.Should().OnlyHaveUniqueItems();
            string[] malformed = [.. NativeHostRegistration.AllowedExtensionIds.Where(id => !IsWellFormedExtensionId(id))];
            _ = malformed.Should().BeEmpty("a browser extension ID is 32 letters from a to p, so anything else can never match a real extension");
        }

        private static bool IsWellFormedExtensionId(string id)
        {
            return id.Length == 32 && id.All(c => c is >= 'a' and <= 'p');
        }

        [Fact]
        public void Edge_and_Chrome_are_pointed_at_the_manifest_for_the_current_user_only()
        {
            _ = NativeHostRegistration.Register(Relay(), DataDir(), _registry);

            _ = _registry.Values.Keys.Should().BeEquivalentTo([EdgeKey, ChromeKey]);
            _ = _registry.Values[EdgeKey].Should().Be(_registry.Values[ChromeKey]);
            _ = File.Exists(_registry.Values[EdgeKey]).Should().BeTrue();
        }

        [Fact]
        public void Registering_again_changes_nothing()
        {
            string relay = Relay();
            _ = NativeHostRegistration.Register(relay, DataDir(), _registry);
            int writes = _registry.Writes;
            DateTime written = File.GetLastWriteTimeUtc(_registry.Values[EdgeKey]);

            bool again = NativeHostRegistration.Register(relay, DataDir(), _registry);

            _ = again.Should().BeTrue();
            _ = _registry.Writes.Should().Be(writes, "the Host does this at every start, and must not rewrite the registry each time");
            _ = File.GetLastWriteTimeUtc(_registry.Values[EdgeKey]).Should().Be(written);
        }

        [Fact]
        public void Moving_the_install_updates_the_registration()
        {
            _ = NativeHostRegistration.Register(Relay(), DataDir(), _registry);
            string moved = Path.Combine(_root, "elsewhere", "MosaicShell.BrowserRelay.exe");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(moved)!);
            File.WriteAllText(moved, "");

            _ = NativeHostRegistration.Register(moved, DataDir(), _registry);

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(_registry.Values[EdgeKey]));
            _ = manifest.RootElement.GetProperty("path").GetString().Should().Be(moved);
        }

        [Fact]
        public void A_relay_that_is_not_there_registers_nothing()
        {
            string missing = Path.Combine(_root, "install", "MosaicShell.BrowserRelay.exe");

            _ = NativeHostRegistration.Register(missing, DataDir(), _registry).Should().BeFalse();

            _ = _registry.Values.Should().BeEmpty();
            _ = Directory.Exists(DataDir()).Should().BeFalse();
        }

        [Fact]
        public void A_relative_path_is_refused()
        {
            _ = NativeHostRegistration.Register(@"install\MosaicShell.BrowserRelay.exe", DataDir(), _registry).Should().BeFalse();

            _ = _registry.Values.Should().BeEmpty();
        }

        [Fact]
        public void A_registry_that_refuses_the_write_does_not_stop_the_Host()
        {
            _registry.Throw = true;

            Func<bool> act = () => NativeHostRegistration.Register(Relay(), DataDir(), _registry);

            _ = act.Should().NotThrow();
            _ = act().Should().BeFalse();
        }

        [Fact]
        public void Unregistering_removes_the_keys_and_the_manifest()
        {
            _ = NativeHostRegistration.Register(Relay(), DataDir(), _registry);
            string manifest = _registry.Values[EdgeKey];

            NativeHostRegistration.Unregister(DataDir(), _registry);

            _ = _registry.Values.Should().BeEmpty();
            _ = File.Exists(manifest).Should().BeFalse();
        }

        [Fact]
        public void Unregistering_when_nothing_was_registered_is_harmless()
        {
            Action act = () => NativeHostRegistration.Unregister(DataDir(), _registry);

            _ = act.Should().NotThrow();
        }

        private sealed class FakeUserRegistry : IUserRegistry
        {
            public Dictionary<string, string> Values { get; } = [];
            public int Writes { get; private set; }
            public bool Throw { get; set; }

            public string? GetDefault(string subKey)
            {
                return Values.GetValueOrDefault(subKey);
            }

            public void SetDefault(string subKey, string value)
            {
                if (Throw)
                {
                    throw new UnauthorizedAccessException("policy");
                }

                Writes++;
                Values[subKey] = value;
            }

            public void DeleteTree(string subKey)
            {
                _ = Values.Remove(subKey);
            }
        }
    }
}
