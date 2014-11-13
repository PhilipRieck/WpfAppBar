using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
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


            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                                    IntPtr lParam, ref bool handled)
            {
                if (msg == CallbackId)
                {
                    if (wParam.ToInt32() == (int)Interop.ABNotify.ABN_POSCHANGED)
                    {
                        ABSetPos(Edge, Window);
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
            appbarWindow.Topmost = false;

            var rect = new Rect(info.OriginalPosition.X, info.OriginalPosition.Y,
                info.OriginalSize.Width, info.OriginalSize.Height);
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                    new ResizeDelegate(DoResize), appbarWindow, rect);

        }

        public static void SetAppBar(Window appbarWindow, ABEdge edge)
        {
            var info = GetRegisterInfo(appbarWindow);
            info.Edge = edge;

            var abd = new Interop.APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(appbarWindow).Handle;
            
            int renderpolicy;

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
            appbarWindow.Topmost = true;
            
            // Set desktop window manager attributes to prevent window
            // from being hidden when peeking at the desktop or when
            // the 'show desktop' button is pressed
            renderPolicy = (int)Interop.DWMNCRenderingPolicy.Enabled;

            Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_EXCLUDED_FROM_PEEK, ref renderPolicy, sizeof(int));
            Interop.DwmSetWindowAttribute(abd.hWnd, (int)Interop.DWMWINDOWATTRIBUTE.DWMA_DISALLOW_PEEK, ref renderPolicy, sizeof(int));

            ABSetPos(info.Edge, appbarWindow);
        }

        private delegate void ResizeDelegate(Window appbarWindow, Rect rect);
        private static void DoResize(Window appbarWindow, Rect rect)
        {
            appbarWindow.Width = rect.Width;
            appbarWindow.Height = rect.Height;
            appbarWindow.Top = rect.Top;
            appbarWindow.Left = rect.Left;
        }



        private static void ABSetPos(ABEdge edge, Window appbarWindow)
        {
            var barData = new Interop.APPBARDATA();
            barData.cbSize = Marshal.SizeOf(barData);
            barData.hWnd = new WindowInteropHelper(appbarWindow).Handle;
            barData.uEdge = (int)edge;

            if (barData.uEdge == (int)ABEdge.Left || barData.uEdge == (int)ABEdge.Right)
            {
                barData.rc.top = 0;
                barData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
                if (barData.uEdge == (int)ABEdge.Left)
                {
                    barData.rc.left = 0;
                    barData.rc.right = (int)Math.Round(appbarWindow.ActualWidth);
                }
                else
                {
                    barData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                    barData.rc.left = barData.rc.right - (int)Math.Round(appbarWindow.ActualWidth);
                }
            }
            else
            {
                barData.rc.left = 0;
                barData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
                if (barData.uEdge == (int)ABEdge.Top)
                {
                    barData.rc.top = 0;
                    barData.rc.bottom = (int)Math.Round(appbarWindow.ActualHeight);
                }
                else
                {
                    barData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;
                    barData.rc.top = barData.rc.bottom - (int)Math.Round(appbarWindow.ActualHeight);
                }
            }

            Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_QUERYPOS, ref barData);
            Interop.SHAppBarMessage((int)Interop.ABMsg.ABM_SETPOS, ref barData);

            var rect = new Rect((double)barData.rc.left, (double)barData.rc.top,
                (double)(barData.rc.right - barData.rc.left), (double)(barData.rc.bottom - barData.rc.top));
            
            //This is done async, because WPF will send a resize after a new appbar is added.  
            //if we size right away, WPFs resize comes last and overrides us.
            appbarWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new ResizeDelegate(DoResize), appbarWindow, rect);
        }
    }
}
