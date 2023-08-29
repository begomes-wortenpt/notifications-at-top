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

    public static void Main(string[] args) {
        NotifyIcon notifyIcon = new NotifyIcon();
        notifyIcon.Icon = new Icon("question_answer_white_24dp.ico");
        notifyIcon.Visible = true;

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
                HandleTeamsNotifications(LoadNotificationPosition());
                HandleSystemNotifications(LoadNotificationPosition());
                Thread.Sleep(10);
            }
        });

        Application.Run();
    }

    private static void SetCheckmarks(NotificationPosition position, MenuItem topLeftMenuItem, MenuItem topRightMenuItem) {
        topLeftMenuItem.Checked = position == NotificationPosition.TopLeft;
        topRightMenuItem.Checked = position == NotificationPosition.TopRight;
    }

    private static void ChangePosition(NotificationPosition newPosition, MenuItem topLeftMenuItem, MenuItem topRightMenuItem) {
        SetNotificationPosition(newPosition);
        SetCheckmarks(newPosition, topLeftMenuItem, topRightMenuItem);
        SetTrayIconPosition(newPosition);
    }

    private static void SetNotificationPosition(NotificationPosition position) {
        SaveNotificationPosition(position);
    }

    private static void SetTrayIconPosition(NotificationPosition position) {
        int x = position == NotificationPosition.TopLeft ? 15 : Screen.PrimaryScreen.Bounds.Width - 115;
        var hwnd = FindWindow("Shell_TrayWnd", null);
        if (hwnd != IntPtr.Zero) {
            SetWindowPos(hwnd, IntPtr.Zero, x, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
        }
    }

    private static void HandleTeamsNotifications(NotificationPosition position) {
        try {
            var teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");
            var chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");

            if (chromeHwnd != IntPtr.Zero) {
                Cooldown = 0;

                int x = position == NotificationPosition.TopLeft ? 15 : Screen.PrimaryScreen.Bounds.Width - 115;

                Rectangle notificationRect = GetWindowRectangle(teamsHwnd);
                int notificationWidth = notificationRect.Width - notificationRect.X;

                if (position == NotificationPosition.TopRight) {
                    x = Screen.PrimaryScreen.Bounds.Width - notificationWidth - 15;
                }

                SetWindowPos(teamsHwnd, IntPtr.Zero, x, 15, 100, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
            } else {
                if (Cooldown >= 30) {
                    SetWindowPos(teamsHwnd, IntPtr.Zero, 0, -9999, -9999, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    Cooldown = 0;
                }
                Cooldown += 1;
            }
        } catch {
            // User Doesn't Have Teams
        }
    }

    private static Rectangle GetWindowRectangle(IntPtr hwnd) {
        Rectangle rectangle = new Rectangle();
        GetWindowRect(hwnd, ref rectangle);
        return rectangle;
    }

    private static void HandleSystemNotifications(NotificationPosition position) {
        var hwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");
        int x = position == NotificationPosition.TopLeft ? 0 : Screen.PrimaryScreen.Bounds.Width - 150;

        SetWindowPos(hwnd, IntPtr.Zero, x, -50, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
    }

    private static void ExitMenuItem_Click(object sender, EventArgs e) {
        Application.Exit();
    }

}

public enum NotificationPosition {
    TopRight,
    TopLeft
}
