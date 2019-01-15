﻿using CairoDesktop.Common;
using CairoDesktop.Configuration;
using CairoDesktop.Interop;
using CairoDesktop.SupportingClasses;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CairoDesktop
{
    /// <summary>
    /// Interaction logic for Desktop.xaml
    /// </summary>
    public partial class Desktop : Window, INotifyPropertyChanged
    {
        private WindowInteropHelper helper;
        private bool altF4Pressed;

        public Stack<string> PathHistory = new Stack<string>();
        public DesktopIcons Icons;
        public DependencyProperty IsOverlayOpenProperty = DependencyProperty.Register("IsOverlayOpen", typeof(bool), typeof(Desktop), new PropertyMetadata(new bool()));


        public bool IsOverlayOpen
        {
            get
            {
                return (bool)GetValue(IsOverlayOpenProperty);
            }
            set
            {
                SetValue(IsOverlayOpenProperty, value);

                if (value)
                {
                    ShowOverlay();
                }
                else
                {
                    CloseOverlay();
                }
            }
        }

        public Desktop()
        {
            InitializeComponent();

            Width = AppBarHelper.PrimaryMonitorSize.Width;
            Height = AppBarHelper.PrimaryMonitorSize.Height - 1;

            if (Startup.IsCairoUserShell)
            {
                sepPersonalization.Visibility = Visibility.Collapsed;
                miPersonalization.Visibility = Visibility.Collapsed;
            }

            setGridPosition();
            setBackground();
        }

        private System.Windows.Media.Brush BackgroundBrush { get; set; }
        private void setBackground()
        {
            if (Startup.IsCairoUserShell)
            {
                if (BackgroundBrush == null)
                    BackgroundBrush = GetCairoBackgroundBrush();

                Background = BackgroundBrush;
            }
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush()
        {
            // TODO: impliment Cairo settings for Background
            return GetCairoBackgroundBrush_Windows();
            // return GetCairoBackgroundBrush_Image();
            // return GetCairoBackgroundBrush_Color();
            // return GetCairoBackgroundBrush_Video();
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush_Windows()
        {
            // draw wallpaper
            string regWallpaper = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", "") as string;
            string regWallpaperStyle = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WallpaperStyle", "") as string;
            string regTileWallpaper = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "TileWallpaper", "") as string;

            CairoWallpaperStyle style = CairoWallpaperStyle.Stretch;
            // https://docs.microsoft.com/en-us/windows/desktop/Controls/themesfileformat-overview
            switch ($"{regWallpaperStyle}{regTileWallpaper}")
            {
                case "01": // Tiled { WallpaperStyle = 0; TileWallpaper = 1 }
                    style = CairoWallpaperStyle.Tile;
                    break;
                case "00": // Centered { WallpaperStyle = 1; TileWallpaper = 0 }
                    style = CairoWallpaperStyle.Center;
                    break;
                case "60": // Fit { WallpaperStyle = 6; TileWallpaper = 0 }
                    style = CairoWallpaperStyle.Fit;
                    break;
                case "100": // Fill { WallpaperStyle = 10; TileWallpaper = 0 }
                    style = CairoWallpaperStyle.Fill;
                    break;
                case "220": // Span { WallpaperStyle = 10; TileWallpaper = 0 }
                    style = CairoWallpaperStyle.Span;
                    break;
                case "20": // Stretched { WallpaperStyle = 2; TileWallpaper = 0 }
                default:
                    style = CairoWallpaperStyle.Stretch;
                    break;
            }

            return GetCairoBackgroundBrush_Image(regTileWallpaper, style);
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush_Image()
        {

            string wallpaper = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", "") as string;
            CairoWallpaperStyle wallpaperStyle = CairoWallpaperStyle.Stretch;

            return GetCairoBackgroundBrush_Image(wallpaper, wallpaperStyle);
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush_Color()
        {
            // TODO: Impliment settings for Color and ColorGradients

            System.Drawing.Color drawingColor1 = System.Drawing.Color.CornflowerBlue; // Come on XNA!
            System.Drawing.Color drawingColor2 = System.Drawing.Color.DarkRed;

            var mediaColor1 = System.Windows.Media.Color.FromRgb(drawingColor1.R, drawingColor1.G, drawingColor1.B);
            var mediaColor2 = System.Windows.Media.Color.FromRgb(drawingColor2.R, drawingColor2.G, drawingColor2.B);

            return new SolidColorBrush(mediaColor1);
            // return new LinearGradientBrush(mediaColor1, mediaColor2, 45);
            // return new RadialGradientBrush(mediaColor1, mediaColor2);
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush_Video()
        {
            // TODO: Impliment Settings
            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/how-to-paint-an-area-with-a-video
            System.Windows.Controls.MediaElement myMediaElement = new System.Windows.Controls.MediaElement();
            myMediaElement.Source = new Uri(@"C:\Users\josua\Videos\Wallpaper.mp4", UriKind.Relative); // Get this from settings
            myMediaElement.LoadedBehavior = System.Windows.Controls.MediaState.Play;
            myMediaElement.IsMuted = true;
            myMediaElement.MediaEnded += (o, a) => myMediaElement.Position = new TimeSpan(0, 0, 1);

            VisualBrush myVisualBrush = new VisualBrush();
            myVisualBrush.Visual = myMediaElement;
            myVisualBrush.AlignmentX = AlignmentX.Center;
            myVisualBrush.AlignmentY = AlignmentY.Center;
            myVisualBrush.TileMode = TileMode.None;
            myVisualBrush.Stretch = Stretch.UniformToFill;

            return myVisualBrush;
        }

        private System.Windows.Media.Brush GetCairoBackgroundBrush_Image(string wallpaper, CairoWallpaperStyle wallpaperStyle)
        {
            ImageBrush backgroundImageBrush = null;
            if (!string.IsNullOrWhiteSpace(wallpaper) && Shell.Exists(wallpaper))
            {
                TryAndEat(() =>
                {
                    Uri backgroundImageUri = new Uri(wallpaper, UriKind.Absolute);
                    BitmapImage backgroundBitmapImage = new BitmapImage(backgroundImageUri);
                    backgroundImageBrush = new ImageBrush(backgroundBitmapImage);

                    switch (wallpaperStyle)
                    {
                        case CairoWallpaperStyle.Tile:
                            backgroundImageBrush.AlignmentX = AlignmentX.Left;
                            backgroundImageBrush.AlignmentY = AlignmentY.Top;
                            backgroundImageBrush.TileMode = TileMode.Tile;
                            backgroundImageBrush.Stretch = Stretch.None;
                            break;
                        case CairoWallpaperStyle.Center:
                            backgroundImageBrush.AlignmentX = AlignmentX.Center;
                            backgroundImageBrush.AlignmentY = AlignmentY.Center;
                            backgroundImageBrush.TileMode = TileMode.None;
                            backgroundImageBrush.Stretch = Stretch.None;
                            break;
                        case CairoWallpaperStyle.Fit:
                            backgroundImageBrush.AlignmentX = AlignmentX.Center;
                            backgroundImageBrush.AlignmentY = AlignmentY.Center;
                            backgroundImageBrush.TileMode = TileMode.None;
                            backgroundImageBrush.Stretch = Stretch.Uniform;
                            break;
                        case CairoWallpaperStyle.Fill:
                        case CairoWallpaperStyle.Span: // TODO: Impliment multiple monitor backgrounds
                            backgroundImageBrush.AlignmentX = AlignmentX.Center;
                            backgroundImageBrush.AlignmentY = AlignmentY.Center;
                            backgroundImageBrush.TileMode = TileMode.None;
                            backgroundImageBrush.Stretch = Stretch.UniformToFill;
                            break;
                        case CairoWallpaperStyle.Stretch:
                        default:
                            backgroundImageBrush.AlignmentX = AlignmentX.Center;
                            backgroundImageBrush.AlignmentY = AlignmentY.Center;
                            backgroundImageBrush.TileMode = TileMode.None;
                            backgroundImageBrush.Stretch = Stretch.Fill;
                            break;
                    }
                });
            }
            return backgroundImageBrush;
        }
               
        private void SetupPostInit()
        {
            Shell.HideWindowFromTasks(helper.Handle);

            if (Settings.EnableDesktopOverlayHotKey)
            {
                HotKeyManager.RegisterHotKey(Settings.DesktopOverlayHotKey, OnShowDesktop);
            }
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);
            }
            else if (msg == NativeMethods.WM_WINDOWPOSCHANGING)
            {
                /*// Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // Determine if the z-order is changing (absence of SWP_NOZORDER flag)
                if (!((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) == NativeMethods.SetWindowPosFlags.SWP_NOZORDER))
                {
                    // add the SWP_NOZORDER flag
                    wndPos.flags = wndPos.flags | NativeMethods.SetWindowPosFlags.SWP_NOZORDER;
                    wndPos.UpdateMessage(lParam);
                }*/

                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);
            }
            else if (msg == NativeMethods.WM_DISPLAYCHANGE && (Startup.IsCairoUserShell))
            {
                SetPosition(((uint)lParam & 0xffff), ((uint)lParam >> 16));
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SetPosition(uint x, uint y)
        {
            Top = 0;
            Left = 0;

            Width = x;
            Height = y - 1;
            setGridPosition();
        }

        public void ResetPosition()
        {
            Top = 0;
            Left = 0;

            Width = AppBarHelper.PrimaryMonitorSize.Width;
            Height = AppBarHelper.PrimaryMonitorSize.Height - 1;
            setGridPosition();
        }

        private void setGridPosition()
        {
            grid.Width = AppBarHelper.PrimaryMonitorWorkArea.Width / Shell.DpiScale;
            grid.Height = AppBarHelper.PrimaryMonitorWorkArea.Height / Shell.DpiScale;
            grid.Margin = new Thickness(System.Windows.Forms.SystemInformation.WorkingArea.Left / Shell.DpiScale, System.Windows.Forms.SystemInformation.WorkingArea.Top / Shell.DpiScale, 0, 0);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!Topmost)
            {
                int result = NativeMethods.SetShellWindow(helper.Handle);
                Shell.ShowWindowBottomMost(helper.Handle);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Startup.IsShuttingDown) // show the windows desktop
            {
                Shell.ToggleDesktopIcons(true);
            }
            else if (altF4Pressed) // Show the Shutdown Confirmation Window
            {
                Cairo.ShowShutdownConfirmation();
                e.Cancel = true;
            }
            else // Eat it !!!
            {
                e.Cancel = true;
            }
        }


        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                altF4Pressed = true;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Top = 0;
            helper = new WindowInteropHelper(this);
            HwndSource.FromHwnd(helper.Handle).AddHook(new HwndSourceHook(WndProc));

            if (Settings.EnableDesktop && Icons == null)
            {
                grid.Children.Add(Icons = new DesktopIcons());
                if (Settings.EnableDynamicDesktop)
                {
                    TryAndEat(() =>
                         {
                             DesktopNavigationToolbar nav = new DesktopNavigationToolbar() { Owner = this };
                             nav.Show();
                         });
                }
            }

            SetupPostInit();
        }

        private void PasteFromClipboard()
        {
            IDataObject clipFiles = Clipboard.GetDataObject();
            if (clipFiles.GetDataPresent(DataFormats.FileDrop))
            {
                if (clipFiles.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (string file in files)
                    {
                        if (Shell.Exists(file))
                        {
                            TryAndEat(() =>
                            {
                                FileAttributes attr = File.GetAttributes(file);
                                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                {
                                    FileSystem.CopyDirectory(file, Icons.Location.FullName + "\\" + new DirectoryInfo(file).Name, UIOption.AllDialogs);
                                }
                                else
                                {
                                    FileSystem.CopyFile(file, Icons.Location.FullName + "\\" + Path.GetFileName(file), UIOption.AllDialogs);
                                }
                            });
                        }
                    }
                }
            }
        }


        private void miPaste_Click(object sender, RoutedEventArgs e)
        {
            PasteFromClipboard();
        }

        private void miPersonalization_Click(object sender, RoutedEventArgs e)
        {
            // doesn't work when shell because Settings app requires Explorer :(
            if (!Shell.StartProcess("desk.cpl"))
            {
                CairoMessage.Show("Unable to open Personalization settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (IsOverlayOpen)
            {
                IsOverlayOpen = false;
            }
        }

        private void grid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!Topmost)
            {
                NativeMethods.SetForegroundWindow(helper.Handle);
            }
        }

        public void Navigate(string newLocation)
        {
            PathHistory.Push(Icons.Location.FullName);
            Icons.Location.Dispose();
            Icons.Location = new SystemDirectory(newLocation, Dispatcher.CurrentDispatcher);
            OnPropertyChanged("CurrentDirectoryFriendly");
        }

        public string CurrentLocation
        {
            get
            {
                return Icons.Location.FullName;
            }
            set
            {
                Icons.Location = new SystemDirectory(value, Dispatcher.CurrentDispatcher);
                OnPropertyChanged("CurrentDirectoryFriendly");
            }
        }

        private void CairoDesktopWindow_LocationChanged(object sender, EventArgs e)
        {
            ResetPosition();
        }

        private void OnShowDesktop(HotKey hotKey)
        {
            ToggleOverlay();
        }

        public void ToggleOverlay()
        {
            IsOverlayOpen = !IsOverlayOpen;
        }

        private void ShowOverlay()
        {
            Topmost = true;
            NativeMethods.SetForegroundWindow(helper.Handle);
            grid.Background = new SolidColorBrush(Color.FromArgb(0x88, 0, 0, 0));
            Background = null;
        }

        private void CloseOverlay()
        {
            Topmost = false;
            Shell.ShowWindowBottomMost(helper.Handle);
            grid.Background = Brushes.Transparent;
            setBackground();
        }

        private void grid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(System.Windows.Controls.ScrollViewer))
            {
                IsOverlayOpen = false;
            }
        }

        public string CurrentDirectoryFriendly
        {
            get
            {
                return Localization.DisplayString.sDesktop_CurrentFolder + " " + Icons.Location.FullName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TryAndEat(Action action)
        {
            try
            { action.Invoke(); }
            catch { }
        }
    }
}