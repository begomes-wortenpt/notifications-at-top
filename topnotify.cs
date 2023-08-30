using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

public class Program {

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);

    const short SWP_NOMOVE = 0X2;
    const short SWP_NOSIZE = 1;
    const short SWP_NOZORDER = 0X4;
    const int SWP_SHOWWINDOW = 0x0040;

    public static int Cooldown;
    private static bool exceptionLogged = false;


    private static string SettingsFilePath {
        get {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.ini");
        }
    }

    private static NotificationPosition LoadNotificationPosition() {
        if (File.Exists(SettingsFilePath)) {
            string[] lines = File.ReadAllLines(SettingsFilePath);
            foreach (string line in lines) {
                if (line.StartsWith("trayposition")) {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2) {
                        string positionStr = parts[1].Trim().ToLower();
                        if (positionStr == "topleft") {
                            return NotificationPosition.TopLeft;
                        } else if (positionStr == "topright") {
                            return NotificationPosition.TopRight;
                        }
                    }
                }
            }
        }

        return NotificationPosition.TopRight;
    }  

    private static void SaveNotificationPosition(NotificationPosition position) {
        string positionStr = position == NotificationPosition.TopLeft ? "topleft" : "topright";
        string[] lines = { "trayposition = " + positionStr };

        File.WriteAllLines(SettingsFilePath, lines);
    }

    private static void Main(string[] args) {
        NotifyIcon notifyIcon = InitializeNotifyIcon();

        NotificationPosition initialPosition = LoadNotificationPosition();

        MenuItem topLeftMenuItem = new MenuItem("Top Left");
        MenuItem topRightMenuItem = new MenuItem("Top Right");
        MenuItem exitMenuItem = new MenuItem("Exit");

        ContextMenu contextMenu = new ContextMenu();
        contextMenu.MenuItems.Add(topLeftMenuItem);
        contextMenu.MenuItems.Add(topRightMenuItem);
        contextMenu.MenuItems.Add(exitMenuItem);

        SetCheckmarks(initialPosition, topLeftMenuItem, topRightMenuItem);

        topLeftMenuItem.Click += (sender, e) => ChangePosition(NotificationPosition.TopLeft, topLeftMenuItem, topRightMenuItem);
        topRightMenuItem.Click += (sender, e) => ChangePosition(NotificationPosition.TopRight, topLeftMenuItem, topRightMenuItem);
        exitMenuItem.Click += new EventHandler(ExitMenuItem_Click);

        notifyIcon.ContextMenu = contextMenu;

        Task.Run(() => {
            while (true) {
                HandleNotifications(LoadNotificationPosition());
                Thread.Sleep(10);
            }
        });

        Application.Run();
    }

    private static NotifyIcon InitializeNotifyIcon() {
        NotifyIcon notifyIcon = new NotifyIcon();
        notifyIcon.Icon = new Icon("question_answer_white_24dp.ico");
        notifyIcon.Visible = true;
        return notifyIcon;
    }

    private static void SetCheckmarks(NotificationPosition position, MenuItem topLeftMenuItem, MenuItem topRightMenuItem) {
        topLeftMenuItem.Checked = position == NotificationPosition.TopLeft;
        topRightMenuItem.Checked = position == NotificationPosition.TopRight;
    }

    private static void ChangePosition(NotificationPosition newPosition, MenuItem topLeftMenuItem, MenuItem topRightMenuItem) {
        SetNotificationPosition(newPosition);
        SetCheckmarks(newPosition, topLeftMenuItem, topRightMenuItem);
    }

    private static void SetNotificationPosition(NotificationPosition position) {
        SaveNotificationPosition(position);
    }

    private static void HandleNotifications(NotificationPosition position) {
        HandleTeamsNotifications(position);
        HandleSystemNotifications(position);
    }

    private static void HandleTeamsNotifications(NotificationPosition position) {
        try {
            var teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
            var chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");

            if (chromeHwnd != IntPtr.Zero) {
                Cooldown = 0;

                if (position == NotificationPosition.TopRight) {
                    Rectangle notificationRect = GetWindowRectangle(teamsHwnd);
                    int notificationWidth = notificationRect.Width - notificationRect.X;
                    int x = Screen.PrimaryScreen.Bounds.Width - notificationWidth - Constants.NotificationMargin;
                    SetWindowPos(teamsHwnd, IntPtr.Zero, x, Constants.NotificationTopMargin, Constants.NotificationWidth, 0, SWP_SHOWWINDOW);
                } else {
                    SetWindowPos(teamsHwnd, IntPtr.Zero, Constants.NotificationLeftMargin, Constants.NotificationTopMargin, Constants.NotificationWidth, 0, SWP_SHOWWINDOW);
                }
            } else {
                if (Cooldown >= 30) {
                    SetWindowPos(teamsHwnd, IntPtr.Zero, 0, Constants.HiddenYPosition, Constants.HiddenWidth, 0, SWP_SHOWWINDOW);
                    Cooldown = 0;
                }
                Cooldown += 1;
            }

            // Reset the exceptionLogged flag since we have successfully handled the situation
            exceptionLogged = false;
        } catch (Exception ex) {
            // Log the exception only if it hasn't been logged before
            if (!exceptionLogged) {
                Console.WriteLine("An error occurred while handling Teams notifications:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                exceptionLogged = true; // Set the flag to true so that the exception is not logged repeatedly
            }
        }
    }

    private static void HandleSystemNotifications(NotificationPosition position) {
        var hwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");

        if (position == NotificationPosition.TopRight) {
            Rectangle notificationRect = GetWindowRectangle(hwnd);
            int notificationWidth = notificationRect.Width - notificationRect.X;
            int x = Screen.PrimaryScreen.Bounds.Width - notificationWidth - Constants.SystemNotificationMargin;
            SetWindowPos(hwnd, IntPtr.Zero, x, Constants.SystemNotificationTopMargin, 0, 0, SWP_SHOWWINDOW);
        } else {
            SetWindowPos(hwnd, IntPtr.Zero, Constants.SystemNotificationLeftMargin, Constants.SystemNotificationTopMargin, 0, 0, SWP_SHOWWINDOW);
        }
    }


    private static Rectangle GetWindowRectangle(IntPtr hwnd) {
        Rectangle rectangle = new Rectangle();
        GetWindowRect(hwnd, ref rectangle);
        return rectangle;
    }


    private static void ExitMenuItem_Click(object sender, EventArgs e) {
        Application.Exit();
    }

    private static class Constants {
        public const int NotificationMargin = 15;
        public const int NotificationTopMargin = 15;
        public const int NotificationWidth = 100;
        public const int NotificationLeftMargin = 0;
        public const int HiddenYPosition = -9999;
        public const int HiddenWidth = -9999;
        public const int SystemNotificationMargin = 50;
        public const int SystemNotificationTopMargin = -50;
        public const int SystemNotificationLeftMargin = 0;
    }

}

public enum NotificationPosition {
    TopRight,
    TopLeft
}
