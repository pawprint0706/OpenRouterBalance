using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
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
        ApplyMainWindowIcon();
        _mainWindow.Show();

        InitTrayIcon();

        // 윈도우 다크/라이트(작업표시줄 색상) 전환 시 아이콘 갱신
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        // 테마 변경은 General 범주로 통지됨
        if (e.Category != UserPreferenceCategory.General) return;

        var old = _trayIcon.Icon;
        _trayIcon.Icon = LoadTrayIcon();
        old?.Dispose();

        ApplyMainWindowIcon();
    }

    // 작업표시줄(트레이)이 라이트 테마인지 여부.
    //  - 라이트(밝은 작업표시줄) → 검정 아이콘
    //  - 다크(어두운 작업표시줄) → 흰색 아이콘
    private static bool IsTaskbarLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
        // SystemUsesLightTheme: 1 = 라이트, 0 = 다크. 값이 없으면 다크로 가정.
        return key?.GetValue("SystemUsesLightTheme") is int v && v != 0;
    }

    // 현재 테마에 맞는 아이콘 리소스 경로
    private static string CurrentIconName =>
        IsTaskbarLightTheme() ? "TrayIcon-black.ico" : "TrayIcon-white.ico";

    // 메인 윈도우(작업표시줄 버튼/타이틀바) 아이콘도 테마에 맞춰 적용
    private void ApplyMainWindowIcon()
    {
        try
        {
            _mainWindow.Icon = BitmapFrame.Create(
                new Uri($"pack://application:,,,/icons/{CurrentIconName}"),
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch
        {
            // 실패 시 기본(ApplicationIcon) 유지
        }
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
        // 트레이 아이콘 좌클릭 한 번으로 메인 윈도우 표시 (우클릭은 컨텍스트 메뉴)
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                ShowMainWindow();
        };
    }

    private static Icon LoadTrayIcon()
    {
        // 작업표시줄 테마에 맞는 흑/백 .ico 를 사용, 없으면 기본 애플리케이션 아이콘 사용
        try
        {
            var uri = new Uri($"pack://application:,,,/icons/{CurrentIconName}");
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
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
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
