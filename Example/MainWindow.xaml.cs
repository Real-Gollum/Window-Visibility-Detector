using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Window_Visibility_Check {
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private System.Timers.Timer timer;
        private IntPtr handle;

        public MainWindow() {
            InitializeComponent();

            WindowVisibilityChecker.init(true);

            timer = new System.Timers.Timer();
            timer.Interval = 500;
            timer.AutoReset = true;
            timer.Elapsed += loop;
        }

        private void ButtonPressed(object sender, RoutedEventArgs e) {
            //bool result = WindowVisibilityChecker.IsWindowVisibleOnScreen(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            if (timer.Enabled) { timer.Stop(); MainButton.Content = "Start"; MainButton.Background = new SolidColorBrush(Colors.Green); }
            else { timer.Start(); MainButton.Content = "Stop"; MainButton.Background = new SolidColorBrush(Colors.Red); }
        }

        private void loop(object sender, System.Timers.ElapsedEventArgs e) {
            DateTime start = DateTime.Now;
            bool result = WindowVisibilityChecker.IsWindowVisibleOnScreen(handle);
            DateTime end = DateTime.Now;

            if (!result) { Console.ForegroundColor = ConsoleColor.Red; }
            else { Console.ForegroundColor = ConsoleColor.Green; }
            Console.WriteLine("(" + (end - start).TotalMilliseconds + " ms): " + WindowVisibilityChecker.ExtendedInfoToMessage());
        }
    }
}
