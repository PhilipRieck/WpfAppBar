using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WpfAppBar
{
    public enum ABEdge : int
    {
        Left = 0,
        Top,
        Right,
        Bottom,
        None
    }

    public static class AppBarFunctions
    {
       
        private class RegisterInfo
        {
            public int CallbackId { get; set; }
            public bool IsRegistered { get; set; }
            public Window Window { get; set; }
            public ABEdge Edge { get; set; }
            public WindowStyle OriginalStyle { get; set; }
            public Point OriginalPosition { get; set; }
            public Size OriginalSize { get; set; }
            public ResizeMode OriginalResizeMode { get; set; }
            public bool OriginalTopmost { get; set; }
            public FrameworkElement ChildElement { get; set; }

            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                                    IntPtr lParam, ref bool handled)
            {
                if (msg == CallbackId)
                {
                    if (wParam.ToInt32() == (int)Interop.ABNotify.ABN_POSCHANGED)
                    {
                        ABSetPos(this, Window, ChildElement);
                        handled = true;
                    }
                }
                return IntPtr.Zero;
            }

        }
        private static readonly Dictionary<Window, RegisterInfo> RegisteredWindowInfo
            = new Dictionary<Window, RegisterInfo>();
        private static RegisterInfo GetRegisterInfo(Window appbarWindow)
        {
            RegisterInfo reg;
            if (RegisteredWindowInfo.ContainsKey(appbarWindow))
            {
                reg = RegisteredWindowInfo[appbarWindow];
            }
            else
            {
                reg = new RegisterInfo()
                {
                    CallbackId = 0,
                    Window = appbarWindow,
                    IsRegistered = false,
                    Edge = ABEdge.Top,
                    OriginalStyle = appbarWindow.WindowStyle,
                    OriginalPosition = new Point(appbarWindow.Left, appbarWindow.Top),
                    OriginalSize =
                        new Size(appbarWindow.ActualWidth, appbarWindow.ActualHeight),
                    OriginalResizeMode = appbarWindow.ResizeMode,
                    OriginalTopmost = appbarWindow.Topmost,
                
                };
                RegisteredWindowInfo.Add(appbarWindow, reg);
            }
            return reg;
        }

        private static void RestoreWindow(Window appbarWindow)
        {
            var info = GetRegisterInfo(appbarWindow);

            appbarWindow.WindowStyle = info.OriginalStyle;
            appbarWindow.ResizeMode = info.OriginalResizeMode;
            appbarWindow.Topmost = info.OriginalTopmost;
            

            var rect = new Rect(info.OriginalPosition.X, info.OriginalPosition.Y,
                info.OriginalSize.Width, info.OriginalSize.Height);
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new ResizeDelegate(DoResize), appbarWindow, rect);

        }

        public static void SetAppBar(Window appbarWindow, ABEdge edge, FrameworkElement childElement = null, bool topMost = true)
        {
            var info = GetRegisterInfo(appbarWindow);
            info.Edge = edge;
            info.ChildElement = childElement;

            var abd = new Interop.APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(appbarWindow).Handle;
            
            int renderPolicy;

            if (edge == ABEdge.None)
            {
                if (info.IsRegistered)
                {
                    Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_REMOVE, ref abd);
                    info.IsRegistered = false;
                }
                RestoreWindow(appbarWindow);
                
                // Restore normal desktop window manager attributes
                renderPolicy = (int)Interop.DWMNCRenderingPolicy.UseWindowStyle;

                Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_EXCLUDED_FROM_PEEK, ref renderPolicy, sizeof(int));
                Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_DISALLOW_PEEK, ref renderPolicy, sizeof(int));

                return;
            }

            if (!info.IsRegistered)
            {
                info.IsRegistered = true;
                info.CallbackId = Interop.RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = info.CallbackId;

                var ret = Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_NEW, ref abd);

                var source = HwndSource.FromHwnd(abd.hWnd);
                source.AddHook(info.WndProc);
            }

            appbarWindow.WindowStyle = WindowStyle.None;
            appbarWindow.ResizeMode = ResizeMode.NoResize;
            appbarWindow.Topmost = topMost;

            // Set desktop window manager attributes to prevent window
            // from being hidden when peeking at the desktop or when
            // the 'show desktop' button is pressed
            renderPolicy = (int)Interop.DWMNCRenderingPolicy.Enabled;

            Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_EXCLUDED_FROM_PEEK, ref renderPolicy, sizeof(int));
            Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_DISALLOW_PEEK, ref renderPolicy, sizeof(int));

            ABSetPos(info, appbarWindow, childElement);
        }

        private delegate void ResizeDelegate(Window appbarWindow, Rect rect);
        private static void DoResize(Window appbarWindow, Rect rect)
        {
            appbarWindow.Width = rect.Width;
            appbarWindow.Height = rect.Height;
            appbarWindow.Top = rect.Top;
            appbarWindow.Left = rect.Left;
        }

        private static void ABSetPos(RegisterInfo info, Window appbarWindow, FrameworkElement childElement)
        {
            var edge = info.Edge;
            var barData = new Interop.APPBARDATA();
            barData.cbSize = Marshal.SizeOf(barData);
            barData.hWnd = new WindowInteropHelper(appbarWindow).Handle;
            barData.uEdge = (int)edge;
            var screen = System.Windows.Forms.Screen.FromHandle(barData.hWnd);
            var screenBounds = screen.Bounds;

            // Transforms a coordinate from WPF space to Screen space
            var toPixel = PresentationSource.FromVisual(appbarWindow).CompositionTarget.TransformToDevice;
            // Transforms a coordinate from Screen space to WPF space
            var toWpfUnit = PresentationSource.FromVisual(appbarWindow).CompositionTarget.TransformFromDevice;

            // Transform window size from wpf units (1/96 ") to real pixels, for win32 usage
            var sizeInPixels = (childElement != null ?
                toPixel.Transform(new Vector(childElement.ActualWidth, childElement.ActualHeight)) :
                toPixel.Transform(new Vector(appbarWindow.ActualWidth, appbarWindow.ActualHeight)));

            var screenSizeInPixels = toPixel.Transform(new Vector(screenBounds.Width, screenBounds.Height));

            barData.rc = GetRectBounds(screenBounds);
            barData.rc = FixByEdge(barData, sizeInPixels);

            Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_QUERYPOS, ref barData);
            Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_SETPOS, ref barData);

            // transform back to wpf units, for wpf window resizing in DoResize. 
            var location = toWpfUnit.Transform(new Point(barData.rc.left, barData.rc.top));
            var dimension = toWpfUnit.Transform(new Vector(barData.rc.right - barData.rc.left, 
                barData.rc.bottom - barData.rc.top));

            var rect = new Rect(location, new Size(dimension.X, dimension.Y));

            //This is done async, because WPF will send a resize after a new appbar is added.  
            //if we size right away, WPFs resize comes last and overrides us.
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new ResizeDelegate(DoResize), appbarWindow, rect);
        }
        private static Interop.RECT FixByEdge(Interop.APPBARDATA barData, Vector sizeInPixels)
        {
            switch ((ABEdge)barData.uEdge)
            {
                case ABEdge.Right:
                    {
                        barData.rc.left = barData.rc.right - (int)Math.Round(sizeInPixels.X);
                        break;
                    }
                case ABEdge.Left:
                    {
                        barData.rc.right = barData.rc.left + (int)sizeInPixels.X;
                        break;
                    }
                case ABEdge.Bottom:
                    {
                        barData.rc.top = barData.rc.bottom - (int)Math.Round(sizeInPixels.Y);
                        break;
                    }
                case ABEdge.Top:
                    {
                        barData.rc.bottom = barData.rc.top + (int)sizeInPixels.Y;
                        break;
                    }
            }

            return barData.rc;
        }

        private static Interop.RECT GetRectBounds(System.Drawing.Rectangle screenBounds)
        {
            Interop.RECT rc = new Interop.RECT();

            rc.left = screenBounds.Left;
            rc.right = screenBounds.Right;
            rc.top = screenBounds.Top;
            rc.bottom = screenBounds.Bottom;

            return rc;
        }

    }
}
