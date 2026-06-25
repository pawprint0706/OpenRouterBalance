using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace OpenRouterBalance;

public partial class App : System.Windows.Application
{
    // 시작프로그램 등록에 사용하는 레지스트리 위치/이름
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "OpenRouterBalance";

    private WinForms.NotifyIcon _trayIcon = null!;
    private WinForms.ToolStripMenuItem _startupItem = null!;
    private MainWindow _mainWindow = null!;

    // 트레이 "종료" 메뉴를 통한 실제 종료 여부
    public bool ReallyExit { get; private set; }

    private void App_Startup(object sender, StartupEventArgs e)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Show();

        InitTrayIcon();
    }

    private void InitTrayIcon()
    {
        var menu = new WinForms.ContextMenuStrip();

        var openItem = new WinForms.ToolStripMenuItem("열기");
        openItem.Click += (_, _) => ShowMainWindow();

        _startupItem = new WinForms.ToolStripMenuItem("시작프로그램 등록")
        {
            CheckOnClick = true,
            Checked = IsStartupRegistered()
        };
        _startupItem.CheckedChanged += StartupItem_CheckedChanged;

        var exitItem = new WinForms.ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => ExitApplication();

        menu.Items.Add(openItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Visible = true,
            Text = "OpenRouter Balance",
            ContextMenuStrip = menu
        };
        // 트레이 아이콘 더블클릭 시 메인 윈도우 표시
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private static Icon LoadTrayIcon()
    {
        // 패키징된 .ico 가 있으면 사용, 없으면 기본 애플리케이션 아이콘 사용
        try
        {
            var uri = new Uri("pack://application:,,,/app.ico");
            var info = GetResourceStream(uri);
            if (info != null)
            {
                using var stream = info.Stream;
                return new Icon(stream);
            }
        }
        catch
        {
            // 무시하고 기본 아이콘으로 폴백
        }
        return SystemIcons.Application;
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApplication()
    {
        ReallyExit = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Shutdown();
    }

    // ---- 시작프로그램(레지스트리) 처리 ----

    private void StartupItem_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_startupItem.Checked)
                RegisterStartup();
            else
                UnregisterStartup();
        }
        catch (Exception ex)
        {
            WinForms.MessageBox.Show($"시작프로그램 설정에 실패했습니다.\n{ex.Message}",
                "OpenRouter Balance", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
            // 실패 시 실제 등록 상태로 체크 표시 되돌리기
            _startupItem.CheckedChanged -= StartupItem_CheckedChanged;
            _startupItem.Checked = IsStartupRegistered();
            _startupItem.CheckedChanged += StartupItem_CheckedChanged;
        }
    }

    private static string ExecutablePath =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

    private static bool IsStartupRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(RunValueName) as string;
        return !string.IsNullOrEmpty(value);
    }

    private static void RegisterStartup()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        key.SetValue(RunValueName, $"\"{ExecutablePath}\"");
    }

    private static void UnregisterStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(RunValueName, false);
    }
}
