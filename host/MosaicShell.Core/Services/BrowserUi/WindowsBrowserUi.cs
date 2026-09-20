using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace MosaicShell.Core.Services.BrowserUi
{
    /// <summary>
    /// The browsers' accessibility trees through Windows UI Automation. Chromium browsers (Edge, Chrome, Brave) and the
    /// web apps installed from them share one top-level window class, and switch their accessibility tree on the first
    /// time something asks for it, so the first read of a window can come back sparse; the caller polls.
    /// </summary>
    /// <remarks>
    /// Windows are found, and their titles read, with plain Win32 calls that involve no accessibility at all (measured: a
    /// UI Automation read of every Chromium-class window, including an editor and a browser with many tabs, took a minute).
    /// UI Automation is touched only for a window the caller has judged could be the player, so no other application is
    /// made to build an accessibility tree, and one slow application cannot hold the read up.
    /// Call from thread-pool threads only: the automation object is created on first use, and creating it on a
    /// single-threaded UI thread would route every later call back through that thread.
    /// </remarks>
    public sealed class WindowsBrowserUi : IBrowserUi
    {
        private const string ChromiumWindowClass = "Chrome_WidgetWin_1";
        private const int MaxSiblingsScanned = 64;

        /// <summary>How long UI Automation waits to reach a window's process, and for one reply from it, in milliseconds.</summary>
        private const uint ConnectionTimeoutMs = 1000;

        private const uint TransactionTimeoutMs = 3000;

        private IUIAutomation Automation
        {
            get
            {
                if (field is null)
                {
                    field = (IUIAutomation)new CUIAutomation();
                    if (field is IUIAutomation2 configurable)
                    {
                        configurable.ConnectionTimeout = ConnectionTimeoutMs;
                        configurable.TransactionTimeout = TransactionTimeoutMs;
                    }
                }

                return field;
            }
        }

        public IReadOnlyList<IUiWindow> Windows()
        {
            List<IUiWindow> windows = [];
            _ = PInvoke.EnumWindows(
                (handle, _) =>
                {
                    if (PInvoke.IsWindowVisible(handle) && ClassOf(handle) == ChromiumWindowClass)
                    {
                        windows.Add(new BrowserWindow(this, handle, TitleOf(handle)));
                    }

                    return true;
                },
                0);
            return windows;
        }

        private static string ClassOf(HWND handle)
        {
            Span<char> buffer = stackalloc char[ChromiumWindowClass.Length + 2];
            int length = PInvoke.GetClassName(handle, buffer);
            return new string(buffer[..length]);
        }

        private static string TitleOf(HWND handle)
        {
            int length = PInvoke.GetWindowTextLength(handle);
            if (length <= 0)
            {
                return "";
            }

            char[] buffer = new char[length + 1];
            int read = PInvoke.GetWindowText(handle, buffer);
            return new string(buffer, 0, read);
        }

        private static string Text(IUIAutomationElement element, UIA_PROPERTY_ID property)
        {
            return element.GetCurrentPropertyValue(property) as string ?? "";
        }

        private sealed class BrowserWindow(WindowsBrowserUi owner, HWND handle, string title) : IUiWindow
        {
            public string Title { get; } = title;

            public string? PlayerTitle()
            {
                IUIAutomationElement? title = TitleElement(owner.Automation);
                return title is null ? null : Text(title, UIA_PROPERTY_ID.UIA_NamePropertyId);
            }

            /// <summary>
            /// The player bar's progress bar. Measured: the title and the bar are both direct children of the player bar,
            /// and the bar is its only progress bar (the volume control is a slider).
            /// </summary>
            public double? PlayerProgress()
            {
                IUIAutomation automation = owner.Automation;
                IUIAutomationElement? title = TitleElement(automation);
                IUIAutomationElement? playerBar = title is null ? null : automation.RawViewWalker.GetParentElement(title);
                if (playerBar is null)
                {
                    return null;
                }

                IUIAutomationCondition isProgressBar = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, (int)UIA_CONTROLTYPE_ID.UIA_ProgressBarControlTypeId);
                IUIAutomationElement? progress = playerBar.FindFirst(TreeScope.TreeScope_Descendants, isProgressBar);
                return progress?.GetCurrentPattern(UIA_PATTERN_ID.UIA_RangeValuePatternId) is IUIAutomationRangeValuePattern range
                    ? range.CurrentValue
                    : null;
            }

            private IUIAutomationElement? TitleElement(IUIAutomation automation)
            {
                IUIAutomationCondition isTitle = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ClassNamePropertyId, YouTubeMusicControls.PlayerTitleClass);
                return automation.ElementFromHandle(handle).FindFirst(TreeScope.TreeScope_Descendants, isTitle);
            }

            public IReadOnlyList<UiToggle> Toggles()
            {
                IUIAutomation automation = owner.Automation;
                IUIAutomationElement window = automation.ElementFromHandle(handle);
                IUIAutomationCondition isToggle = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_IsTogglePatternAvailablePropertyId, true);
                IUIAutomationElementArray found = window.FindAll(TreeScope.TreeScope_Descendants, isToggle);
                IUIAutomationTreeWalker walker = automation.RawViewWalker;

                List<UiToggle> toggles = [];
                for (int i = 0; i < found.Length; i++)
                {
                    IUIAutomationElement button = found.GetElement(i);
                    IUIAutomationElement? parent = walker.GetParentElement(button);
                    toggles.Add(new UiToggle(
                        parent is null ? "" : Text(parent, UIA_PROPERTY_ID.UIA_ClassNamePropertyId),
                        parent is null ? -1 : IndexInParent(automation, walker, parent, button),
                        StateOf(button),
                        () => ((IUIAutomationTogglePattern)button.GetCurrentPattern(UIA_PATTERN_ID.UIA_TogglePatternId)).Toggle()));
                }

                return toggles;
            }

            private static UiToggleState StateOf(IUIAutomationElement button)
            {
                return button.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ToggleToggleStatePropertyId) switch
                {
                    (int)ToggleState.ToggleState_Off => UiToggleState.Off,
                    (int)ToggleState.ToggleState_On => UiToggleState.On,
                    _ => UiToggleState.Indeterminate,
                };
            }

            private static int IndexInParent(IUIAutomation automation, IUIAutomationTreeWalker walker, IUIAutomationElement parent, IUIAutomationElement child)
            {
                IUIAutomationElement? sibling = walker.GetFirstChildElement(parent);
                for (int index = 0; sibling is not null && index < MaxSiblingsScanned; index++)
                {
                    if (automation.CompareElements(sibling, child))
                    {
                        return index;
                    }

                    sibling = walker.GetNextSiblingElement(sibling);
                }

                return -1;
            }
        }
    }
}
