using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.Commands;
using PayPalReports.Contexts;
using PayPalReports.CustomEvents;
using PayPalReports.Pages;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PayPalReports
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public List<NavigateFrameCommand> FramePages { get; private set; }

        public Page CurrentFramePage => FRAME_NAVIGATION_CONTEXT.CurrentPage;

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly ILogger<MainWindow> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly IServiceProvider SERVICE_PROVIDER;
        private readonly FrameNavigationContext FRAME_NAVIGATION_CONTEXT;


        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            LOGGER = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            SERVICE_PROVIDER = serviceProvider;

            FRAME_NAVIGATION_CONTEXT = serviceProvider.GetRequiredService<FrameNavigationContext>();
            FRAME_NAVIGATION_CONTEXT.CurrentPageChanged += OnCurrentPageChanged;

            NavigateFrameCommand ReportFrameNavigateCommand = new(serviceProvider.GetRequiredService<FrameNavigationContext>(), serviceProvider.GetRequiredService<ReportsPage>(), "Report");
            NavigateFrameCommand ConfigurationFrameNavigateCommand = new(serviceProvider.GetRequiredService<FrameNavigationContext>(), serviceProvider.GetRequiredService<ConfigurationPage>(), "Configuration");

            FramePages = [ReportFrameNavigateCommand, ConfigurationFrameNavigateCommand];
            OnPropertyChanged(nameof(FramePages));
            TabRow.SelectedIndex = 0;

            restoreButton.Visibility = Visibility.Collapsed;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCurrentPageChanged()
        {
            ContentFrame.Navigate(CurrentFramePage);
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Navigation Tabs
        private void ListBoxTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //LOGGER.LogDebug("Sender: {@Sender} \n eventArgs: {@EventArgs}", sender, e);

            // Cast sender to Listbox, cast SelectedItem to NavigateFrameCommand, then execute with unused arg
            ((NavigateFrameCommand)((ListBox)sender).SelectedItem)?.Execute(null);
        }

        private void TitleBar_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            LOGGER.LogDebug("{RestoreBounds}", this.RestoreBounds);
            LOGGER.LogDebug("{Top}", this.Top);
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                maximizeButton.Visibility = Visibility.Visible;
                restoreButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                maximizeButton.Visibility = Visibility.Collapsed;
                restoreButton.Visibility = Visibility.Visible;
            }
        }

        private void TitleBar_MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /**
         * Notes for restoring      TODO
         *      RestoreBounds   - need to modify position to curser position
         * */
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                TitleBar_MaximizeRestoreButtonClick(sender, e);
                // reposition to mouse here - TODO
            }
            DragMove();
        }

        // The following is only for proper maximizing of the window (may remove resizing)
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure<MINMAXINFO>(lParam);

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new();
                    monitorInfo.cbSize = Marshal.SizeOf<MONITORINFO>();
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
    }
}