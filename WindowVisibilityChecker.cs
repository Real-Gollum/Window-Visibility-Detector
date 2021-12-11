using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Runtime.InteropServices;

namespace Window_Visibility_Check {
    static class WindowVisibilityChecker {

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        // Used to reduce the rect size if the window is partially out of bounds, small tolerance needed
        // https://stackoverflow.com/questions/34139450/getwindowrect-returns-a-size-including-invisible-borders: appears to be 7px invisible border or something
        private const int tolerance = 8;

        public struct Rect {
            public int Left, Top, Right, Bottom;

            public static Rect Init(int Left, int Top, int Right, int Bottom) {
                Rect ret = new Rect();
                ret.Left = Left;
                ret.Right = Right;
                ret.Top = Top;
                ret.Bottom = Bottom;
                return ret;
            }

            // removes the invisible border from the calculations
            public void SubtractBordersForWindow() {
                Left += tolerance;
                Bottom -= tolerance;
                Right -= tolerance;
            }
        }

        const int SM_CXVIRTUALSCREEN = 78;
        const int SM_CYVIRTUALSCREEN = 79;
        const int SM_CMONITORS = 80;

        private static int width, height, numberOfDisplays, sizeOfTaskbar;
        public static int extendedInfo = -1;
        private static Rect window;
        private static IntPtr currentHwnd;
        private static List<Rect> rects;

        public static void init(bool DebugInfo = false) {
            getDisplaysData(DebugInfo);

            sizeOfTaskbar = DisplayInfo.Screens[0].height - DisplayInfo.Screens[0].workingArea.bottom;
        }

        private static void getDisplaysData(bool Debuginfo = false) {
            width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            numberOfDisplays = GetSystemMetrics(SM_CMONITORS);

            DisplayInfo.getDisplaysInfo();

            if (DisplayInfo.numberOfDisplays != numberOfDisplays) { throw new Exception("Number of display didnt line up"); }

            if (!Debuginfo) { return; }

            Console.WriteLine("(" + DisplayInfo.bounds[0] + " | " + DisplayInfo.bounds[1] + ") " + width + "x" + height + " with " + numberOfDisplays + " Screens");
            for (int i = 0; i < DisplayInfo.Screens.Count; i++) {
                Console.WriteLine("Display " + i + ":  (" + DisplayInfo.Screens[i].x + " | " + DisplayInfo.Screens[i].y + ")  " + DisplayInfo.Screens[i].width + "x" + DisplayInfo.Screens[i].height);
            }
        }

        public static string ExtendedInfoToMessage() {
            switch (extendedInfo) {
                case 0: return "Completely hidden";
                case 1: return "Out of bounds";
                case 2: return "Covered by a fullscreen window";
                case 3: return "Covered completely";
                case 4: return "Partially covered";
                case 5: return "Completely Visible";
                case 6: return "Active window";

                default: return "No info available yet. If this doesnt change over time, the window probably doesnt exist";
            }
        }


        // includeTaskbar includes the taskbar as normal screen in every calculation, increasing the size needed for a non fullscreen window (aka something maximized) to count as "completely covering"
        public static bool IsWindowVisibleOnScreen(IntPtr handle, bool includeTaskbarAsNormalScreen = false) {
            currentHwnd = handle;
            extendedInfo = 5;   // Default: Visible
            
            if (!IsWindow(handle)) { extendedInfo = -1; return false; }
            if (GetForegroundWindow() == handle) { extendedInfo = 6; return true; }
            if (!IsWindowVisible(handle) || IsIconic(handle)) { extendedInfo = 0; return false; }

            // Check for out of bounds:
            GetWindowRect(handle, out window);
            window.SubtractBordersForWindow();
            //Console.WriteLine(RectToString(window));
            if (window.Left > DisplayInfo.bounds[0] + width || window.Right < DisplayInfo.bounds[0] || window.Top > DisplayInfo.bounds[1] + height || window.Bottom < DisplayInfo.bounds[1]) { extendedInfo = 1; return false; }
            

            // Check every window for overlap
            rects = new List<Rect>();

            // If the window overlaps with the taskbar, reduce its size so it doesn't
            if (!includeTaskbarAsNormalScreen && window.Bottom > DisplayInfo.bounds[1] + height - sizeOfTaskbar) { window.Bottom = DisplayInfo.bounds[1] + height - sizeOfTaskbar; }

            CallBackPtr callBackPtr = new CallBackPtr(EnumReport.Report);
            EnumReport.EnumDesktopWindows(IntPtr.Zero, callBackPtr, IntPtr.Zero);

            if (rects.Count > 0) { IsCoveredByWindows(); }

            if (extendedInfo < 4) { return false; }
            return true;
        }


        //https://stackoverflow.com/questions/16180667/given-a-rectangular-area-and-a-set-of-rectangles-check-if-the-entire-area-is-co/16180879#16180879 @Syam answer:
        private static bool IsCoveredByWindows() {
            List<Rect> cover = new List<Rect>();
            cover.Add(ClampRect(window, Rect.Init(DisplayInfo.bounds[0], DisplayInfo.bounds[1], DisplayInfo.bounds[0] + width, DisplayInfo.bounds[1] + height)));
            //Console.WriteLine("Default: " + cover[0].Left + "|" + cover[0].Top + "  " + cover[0].Right + "|" + cover[0].Bottom);

            for (int i = 0; i < rects.Count; i++) {
                int length = cover.Count;
                Stack<Rect> remove = new Stack<Rect>();

                for (int i2 = 0; i2 < length; i2++) {
                    // Check for overlap
                    if (rects[i].Left < cover[i2].Right && rects[i].Right > cover[i2].Left && rects[i].Bottom > cover[i2].Top && rects[i].Top < cover[i2].Bottom) {
                        remove.Push(cover[i2]);

                        // Now add between 0 and 4 new rects in a way, that none of them overlap with rects[i]
                        if (rects[i].Left - cover[i2].Left > 0) { cover.Add(Rect.Init(cover[i2].Left, cover[i2].Top, rects[i].Left, cover[i2].Bottom)); }
                        if (cover[i2].Right - rects[i].Right > 0) { cover.Add(Rect.Init(rects[i].Right, cover[i2].Top, cover[i2].Right, cover[i2].Bottom)); }
                        // Now for Vertical (Note that the width is reduced/different so that the new rects dont overlap)
                        if (rects[i].Top - cover[i2].Top > 0) { cover.Add(Rect.Init(Math.Max(rects[i].Left, cover[i2].Left), cover[i2].Top, Math.Min(rects[i].Right, cover[i2].Right), rects[i].Top)); }
                        if (cover[i2].Bottom - rects[i].Bottom > 0) { cover.Add(Rect.Init(Math.Max(rects[i].Left, cover[i2].Left), rects[i].Top, Math.Min(rects[i].Right, cover[i2].Right), cover[i2].Bottom)); }
                    }
                }

                length = remove.Count;
                for (int i2 = 0; i2 < length; i2++) {
                    cover.Remove(remove.Pop());
                }

                // If there are no uncovered areas left, return true
                if (cover.Count == 0) { extendedInfo = 3; return true; }

                /*for (int i2 = 0; i2 < cover.Count; i2++) {
                    Console.WriteLine("cover["+i2+"]: " + RectToString(cover[i2]));
                }*/
            }

            extendedInfo = 4;
            return false;
        }

        
        private static Rect ClampRect(Rect rect, Rect bounds) {
            return Rect.Init(Math.Min(Math.Max(bounds.Left, rect.Left), bounds.Right), Math.Min(Math.Max(bounds.Top, rect.Top), bounds.Bottom),
                Math.Max(Math.Min(bounds.Right, rect.Right), bounds.Left), Math.Max(Math.Min(bounds.Bottom, rect.Bottom), bounds.Top));
        }

        public static string RectToString(Rect rect) {
            return rect.Left + "|" + rect.Top + " " + rect.Right + "|" + rect.Bottom;
        }

        private delegate bool CallBackPtr(IntPtr hwnd, int lParam);

        private class EnumReport {
            [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool EnumDesktopWindows(IntPtr hDesktop, CallBackPtr lpEnumCallbackFunction, IntPtr lParam);

            //[DllImport("user32.dll")]
            //private static extern bool EnumChildWindows(IntPtr window, CallBackPtr callback, int lParam);

            [DllImport("user32.dll")]
            private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", EntryPoint = "GetWindowText",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

            private static Rect rect = new Rect();


            public static bool Report(IntPtr hwnd, int lParam) {
                // No more checking needed, false should end EnumDesktopWindows
                if (hwnd == currentHwnd) { return false; }

                // Check for visibility
                if (IsWindowVisible(hwnd) && !IsIconic(hwnd)) {
                    StringBuilder strbTitle = new StringBuilder(255);
                    int nLength = GetWindowText(hwnd, strbTitle, strbTitle.Capacity + 1);

                    // Visibility Check, not implemented yet, some windows have the layered attribute, even if the dont use it (cmd i think)
                    // And check for transparency (and ignore their windows)
                    //(GetWindowLong(hwnd, -20) & 0x00080000) >> 19 == 1

                    // Almost every window has a title, except for system windows
                    if (strbTitle.ToString().Length == 0) { return true; }
                    GetWindowRect(hwnd, out rect);
                    
                    // Check if windows overlap
                    if (rect.Left < WindowVisibilityChecker.window.Right && rect.Right > WindowVisibilityChecker.window.Left && rect.Bottom > WindowVisibilityChecker.window.Top && rect.Top < WindowVisibilityChecker.window.Bottom) {
                        rect.SubtractBordersForWindow();
                        WindowVisibilityChecker.rects.Add(rect);
                        
                        //Console.Write(strbTitle.ToString() + " " + strbTitle.ToString().Length + ": " + RectToString(rect));
                    }
                    
                }

                // And check all child windows ? - Nope, not implemented yet

                return true;
            }
        }


    }
}