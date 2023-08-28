using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

public class Program {

    #region Native Stuff
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className,  string windowTitle);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);

    const short SWP_NOMOVE = 0X2;
    const short SWP_NOSIZE = 1;
    const short SWP_NOZORDER = 0X4;
    const int SWP_SHOWWINDOW = 0x0040;

    #endregion

    public static int Cooldown; // Since The Teams Notification Stutters, Add A Cooldown To Dismiss

    public static void Main(string[] args) {
        NotifyIcon notifyIcon = new NotifyIcon();

        // Load the custom icon directly using Icon class
        // Using custom icon from Google Material Icons
        // Icon source: https://fonts.google.com/icons?selected=Material%20Icons%3Aquestion_answer%3A
        notifyIcon.Icon = new Icon("question_answer_white_24dp.ico");
        notifyIcon.Visible = true;

        // Create the context menu for the taskbar icon
        ContextMenu contextMenu = new ContextMenu();
        MenuItem exitMenuItem = new MenuItem("Exit");
        exitMenuItem.Click += new EventHandler(ExitMenuItem_Click);
        contextMenu.MenuItems.Add(exitMenuItem);
        notifyIcon.ContextMenu = contextMenu;

        // Run the main logic in another task to avoid blocking the main one with the while loop.
        Task.Run(() => {
            while (true) {

                //MS Teams Notifications
                try{
                    var teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
                    var chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window"); // I Think MS Is Using Chrome Webview For Notifications

                    //If A Notification Is Showing Up
                    if (chromeHwnd != IntPtr.Zero){
                        Cooldown = 0;

                        if (System.AppDomain.CurrentDomain.FriendlyName == "topleft.exe"){
                            //Sets to top left
                            SetWindowPos(teamsHwnd, 0, 15, 15, 100, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                        }
                        else if (System.AppDomain.CurrentDomain.FriendlyName == "topright.exe"){
                            //Sets to top right

                            //Get the current position of the notification window
                            Rectangle NotifyRect = new Rectangle();
                            GetWindowRect(teamsHwnd, ref NotifyRect);

                            NotifyRect.Width = NotifyRect.Width - NotifyRect.X;
                            NotifyRect.Height = NotifyRect.Height - NotifyRect.Y;

                            SetWindowPos(teamsHwnd, 0, Screen.PrimaryScreen.Bounds.Width - NotifyRect.Width - 15, 15, 100, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                        }
                    }
                    else {
                        if (Cooldown >= 30){
                            SetWindowPos(teamsHwnd, 0, 0, -9999, -9999, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW); // Move To Off Screen
                            Cooldown = 0;
                        }
                        Cooldown += 1; // Don't Dismiss Until 30 Frames After Signal, Prevents Stutters
                    }
                }
                catch{
                    //User Doesn't Have Teams
                }

                //Windows System Notifications
                var hwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");
                if (System.AppDomain.CurrentDomain.FriendlyName == "topleft.exe"){
                    //Sets to top left (easy peasy)
                    //50PX Y offset to make the spacing even
                    SetWindowPos(hwnd, 0, 0, -50, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                }
                else if (System.AppDomain.CurrentDomain.FriendlyName == "topright.exe"){
                    //Sets to top right (not as easy)

                    //Get the current position of the notification window
                    Rectangle NotifyRect = new Rectangle();
                    GetWindowRect(hwnd, ref NotifyRect);

                    NotifyRect.Width = NotifyRect.Width - NotifyRect.X;
                                NotifyRect.Height = NotifyRect.Height - NotifyRect.Y;

                    //50PX Y offset to make the spacing even
                    SetWindowPos(hwnd, 0, Screen.PrimaryScreen.Bounds.Width - NotifyRect.Width, -50, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                }

                Thread.Sleep(10);
            }
        });

        Application.Run();
    }

    private static void ExitMenuItem_Click(object sender, EventArgs e) {
        Application.Exit();
    }
}