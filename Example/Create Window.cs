using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;

using System.IO;
using System.Net.Http;
using static Window.Win32Window;

namespace Window {
    delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public class Win32Window {

        private static ushort regResult;
        private static WNDCLASSEX wind_class;

        #region Consts
        const UInt32 WS_OVERLAPPEDWINDOW = 0xcf0000;
        const UInt32 WS_VISIBLE = 0x10000000;
        const UInt32 WS_CAPTION = 0x00C00000;
        const UInt32 WS_SYSMENU = 0x00080000;
        const UInt32 WS_MAXIMIZEBOX = 0x00010000;
        const UInt32 WS_MINIMIZEBOX = 0x00020000;


        const UInt32 CS_USEDEFAULT = 0x80000000;
        const UInt32 CS_DBLCLKS = 8;
        const UInt32 CS_VREDRAW = 1;
        const UInt32 CS_HREDRAW = 2;
        const UInt32 COLOR_WINDOW = 5;
        const UInt32 COLOR_BACKGROUND = 1;
        const UInt32 IDC_CROSS = 32515;
        const UInt32 IDC_ARROW = 32512;
        const UInt32 WM_DESTROY = 2;
        const UInt32 WM_PAINT = 0x0f;
        const UInt32 WM_LBUTTONDOWN = 513;
        const UInt32 WM_LBUTTONUP = 0x0202;
        const UInt32 WM_LBUTTONDBLCLK = 0x0203;
        const UInt32 WM_MOUSEMOVE = 0x0200;
        const UInt32 WM_ENTERIDLE = 0x0121;
        const UInt32 WM_NCHITTEST = 0x0084;
        const UInt32 HTCAPTION = 0x2;
        const UInt32 HTCLIENT = 1;

        const int GWL_EXSTYLE = -20;
        const int GWL_STYLE = -16;
        const int WS_EX_TOOLWINDOW = 0x0080;
        const int WS_EX_LAYERED = 524288;
        const int HWND_TOPMOST = -1;
        //const int fuchsia = 8388863;
        const uint fuchsia = 0xFFFF00FF;
        #endregion

        #region Structs
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WNDCLASSEX {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public IntPtr lpszClassName;
            public IntPtr hIconSm;
        }

        public struct MSG {
            public IntPtr hwnd;
            public uint message;
            public int wParam;
            public int lParam;
            public int time;
            public POINT pt;
            public int lPrivate;
        }

        public struct POINT {
            public int X;
            public int Y;
        }

        #endregion

        #region Dlls
        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyWindow(IntPtr hWnd);


        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
        static extern IntPtr CreateWindowEx(
           int dwExStyle,
           UInt16 regResult,
           //string lpClassName,
           string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClassEx")]
        static extern System.UInt16 RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);
        [DllImport("user32.dll")]
        static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, int dwFlags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        
        [DllImport("user32.dll")]
        public static extern uint WaitForInputIdle(IntPtr hProc, uint dwMilliseconds);
        #endregion

        [Flags]
        public enum QueueStatusFlags : UInt32
        {
            QS_KEY = 0x0001,
            QS_MOUSEMOVE = 0x0002,
            QS_MOUSEBUTTON = 0x0004,
            QS_POSTMESSAGE = 0x0008,
            QS_TIMER = 0x0010,
            QS_PAINT = 0x0020,
            QS_SENDMESSAGE = 0x0040,
            QS_HOTKEY = 0x0080,
            QS_ALLPOSTMESSAGE = 0x0100,
            QS_RAWINPUT = 0x0400,
            QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON),
            QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT),
            QS_REFRESH = (QS_HOTKEY | QS_KEY | QS_MOUSEBUTTON | QS_PAINT),
            QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY),
            QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE)
        }



        private WndProc delegWndProc;
        public IntPtr handle;
        public bool isClosed = false;
        public BufferedGraphics buffer;
        private Task windowThread;

        public bool Create(string name, string className) {
            delegWndProc = myWndProc;

            if (regResult == 0) {

                wind_class = new WNDCLASSEX();
                wind_class.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
                wind_class.style = (int)(CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS);     //Doubleclicks are active
                wind_class.hbrBackground = (IntPtr)COLOR_BACKGROUND + 1;            //Black background, +1 is necessary
                wind_class.cbClsExtra = 0;
                wind_class.cbWndExtra = 0;
                wind_class.hInstance = Marshal.GetHINSTANCE(this.GetType().Module);     // alternative: Process.GetCurrentProcess().Handle;
                wind_class.hIcon = IntPtr.Zero;
                wind_class.hCursor = LoadCursor(IntPtr.Zero, (int)IDC_CROSS);
                wind_class.lpszMenuName = null;
                wind_class.lpszClassName = Marshal.StringToHGlobalAnsi(className);  // Need conversion, or else only the first char gets used (idk why) https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.stringtohglobalansi?view=net-6.0
                wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);
                wind_class.hIconSm = IntPtr.Zero;
                regResult = RegisterClassEx(ref wind_class);
            }

            if (regResult == 0) {
                uint error = GetLastError();
                Console.WriteLine("Could not register class: " + error);
                return false;
            }

            //The next line did NOT work with me! When searching the web, the reason seems to be unclear! 
            //It resulted in a zero hWnd, but GetLastError resulted in zero (i.e. no error) as well !!??)
            //IntPtr hWnd = CreateWindowEx(0, wind_class.lpszClassName, "MyWnd", WS_OVERLAPPEDWINDOW | WS_VISIBLE, 0, 0, 30, 40, IntPtr.Zero, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);

            //This version worked and resulted in a non-zero hWnd
            IntPtr hWnd = CreateWindowEx(0, regResult, name, WS_CAPTION | WS_SYSMENU | WS_VISIBLE, 0, 0, 300, 400, IntPtr.Zero, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);

            if (hWnd == ((IntPtr)0)) {
                uint error = GetLastError();
                Console.WriteLine("Error in Create 2: " + error);
                return false;
            }

            ShowWindow(hWnd, 1);
            UpdateWindow(hWnd);
            this.handle = hWnd;
            return true;
        }



        private IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            switch (msg)
            {
                case 0x0010:
                    isClosed = true;
                    Destroy();
                    return DefWindowProc(hWnd, msg, wParam, lParam);

                /*case WM_NCHITTEST:
                    IntPtr hit = DefWindowProc(hWnd, msg, wParam, lParam);
                    if (hit == (IntPtr)HTCLIENT) { hit = (IntPtr)HTCAPTION; }
                    return hit;*/

                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }

            //if (msg == 0x0010) { isClosed = true; Destroy(); }
        }

        
        public static implicit operator IntPtr(Win32Window win) {
            return win.handle;
        }


        public void Destroy() {
            if (!DestroyWindow(this)) { throw new Exception("Window couldn't be destroyed"); }
        }





        [DllImport("user32.dll")]
        public static extern bool PeekMessageA(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
        
        [DllImport("user32.dll")]
        public static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")]
        public static extern uint GetQueueStatus(uint flags);


        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);
        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);


        public void CreateWindowInSeperateThread(string name, string className)
        {
            windowThread = Task.Run(() => { Create(name, className); MessageHandler(); });
        }


        /// <summary>
        /// Pumps all messages to the windProc
        /// </summary>
        private void MessageHandler()
        {
            Window.Win32Window.MSG msg;
            while (!isClosed)
            {

                if (GetQueueStatus((uint) QueueStatusFlags.QS_ALLINPUT) > 0)
                {
                    if (Window.Win32Window.GetMessage(out msg, IntPtr.Zero, 0, 0)) { Window.Win32Window.TranslateMessage(ref msg); Window.Win32Window.DispatchMessage(ref msg); }
                }
                //if (Window.Win32Window.GetMessage(out msg, IntPtr.Zero, 0, 0)) { Window.Win32Window.TranslateMessage(ref msg); Window.Win32Window.DispatchMessage(ref msg); }
            }
        }



        public void ApplyStyles()
        {
            SetWindowLong(handle, GWL_STYLE, GetWindowLong(handle, GWL_STYLE) | (0x00C00000 | 0x00010000 | 0x00020000 | 0x00040000 | 0x10000000));      // best option
            //SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
        }

    }
}