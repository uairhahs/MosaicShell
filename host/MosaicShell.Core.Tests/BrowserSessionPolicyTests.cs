using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>Whether a Windows media session belongs to a web browser or an installed web app, from its app id.</summary>
    public class BrowserSessionPolicyTests
    {
        [Theory]
        [InlineData("music.youtube.com-5929F88E_vezhnr0wkvrcy!App")]
        [InlineData("MSEdge")]
        [InlineData("MSEdge.music.youtcom_/watch.mosaicedgeAJ.Default")]
        [InlineData("Chrome")]
        [InlineData("firefox.exe")]
        [InlineData("BraveSoftware.Brave-Browser")]
        public void Browsers_and_web_apps_are_browser_sessions(string appId)
        {
            _ = BrowserSessionPolicy.LooksLikeBrowserSession(appId).Should().BeTrue();
        }

        [Theory]
        [InlineData("Spotify.exe")]
        [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic")]
        [InlineData("foobar2000.exe")]
        [InlineData("")]
        [InlineData(null)]
        public void Native_players_and_nothing_are_not(string? appId)
        {
            _ = BrowserSessionPolicy.LooksLikeBrowserSession(appId).Should().BeFalse();
        }
    }
}
