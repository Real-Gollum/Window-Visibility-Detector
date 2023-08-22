using System;



public static class Program
{

    static Window.Win32Window window;

    public static void Main()
    {
        Console.Write("Window Handle (Leave empty to create a new custom window): ");
        string input = Console.ReadLine();

        IntPtr handle;

        uint castHandle;

        if (uint.TryParse(input, out castHandle)) {
            handle = (IntPtr)castHandle;
        
        } else {

            window = new Window.Win32Window();
            window.Create("Test Window", "Demo Window");
            //window.CreateWindowInSeperateThread("Test Window", "Demo Window");
            window.ApplyStyles();

            handle = window;
        }

        Window_Visibility_Check.WindowVisibilityChecker.init();

        DateTime start = DateTime.Now;
        while(true) {
            
            if (window != null) {
                if (window.isClosed) { break; }

                Window.Win32Window.MSG msg;
                while (Window.Win32Window.GetQueueStatus((uint)(Window.Win32Window.QueueStatusFlags.QS_ALLINPUT & ~Window.Win32Window.QueueStatusFlags.QS_MOUSEMOVE)) > 0)
                {
                    if (Window.Win32Window.GetMessage(out msg, IntPtr.Zero, 0, 0)) { Window.Win32Window.TranslateMessage(ref msg); Window.Win32Window.DispatchMessage(ref msg); }
                }

            }

            if ((DateTime.Now - start).TotalMilliseconds > 500) {
                start = DateTime.Now;

                if (Window_Visibility_Check.WindowVisibilityChecker.IsWindowVisibleOnScreen(handle)) {
                    Console.ForegroundColor = ConsoleColor.Green;
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine(Window_Visibility_Check.WindowVisibilityChecker.ExtendedInfoToMessage());
            }


        }

    }


}