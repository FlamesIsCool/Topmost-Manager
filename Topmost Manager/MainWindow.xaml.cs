using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Topmost_Manager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => RefreshWindowList();
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e) => RefreshWindowList();

        private void MakeTopmost_Click(object sender, RoutedEventArgs e)
        {
            if (WindowListComboBox.SelectedItem is WindowInfo wi)
            {
                NativeMethods.SetWindowTopmost(wi.Handle, true);
                StatusText.Text = $"✓ {wi.Title.Split('—')[0].Trim()} → Always on Top!";
            }
        }

        private void RemoveTopmost_Click(object sender, RoutedEventArgs e)
        {
            if (WindowListComboBox.SelectedItem is WindowInfo wi)
            {
                NativeMethods.SetWindowTopmost(wi.Handle, false);
                StatusText.Text = $"✓ Removed Topmost from {wi.Title.Split('—')[0].Trim()}";
            }
        }

        private void RefreshWindowList()
        {
            var windows = GetAllWindows();
            WindowListComboBox.ItemsSource = windows;
            if (windows.Any()) WindowListComboBox.SelectedIndex = 0;
            StatusText.Text = $"{windows.Count} windows detected • Ready";
        }

        // Get all visible main windows
        private static List<WindowInfo> GetAllWindows()
        {
            var list = new List<WindowInfo>();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                int length = NativeMethods.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                var sb = new StringBuilder(length + 1);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (string.IsNullOrWhiteSpace(title)) return true;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

                try
                {
                    var process = Process.GetProcessById((int)pid);
                    if (process.MainWindowHandle == hWnd || process.MainWindowHandle == IntPtr.Zero)
                    {
                        list.Add(new WindowInfo
                        {
                            Handle = hWnd,
                            Title = $"{title} — {process.ProcessName} ({pid})"
                        });
                    }
                }
                catch { /* process already exited */ }

                return true;
            }, IntPtr.Zero);

            return list.OrderBy(x => x.Title).ToList();
        }

        public class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = string.Empty;
            public override string ToString() => Title;
        }
    }

    internal static class NativeMethods
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        public static void SetWindowTopmost(IntPtr hWnd, bool topmost)
        {
            SetWindowPos(hWnd,
                topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}
