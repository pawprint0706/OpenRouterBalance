using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Web.WebView2.Core;

namespace OpenRouterBalance;

public partial class MainWindow : Window
{
    // 크레딧(잔액 조회/충전) 페이지
    private const string TargetUrl = "https://openrouter.ai/settings/credits";

    // 모바일 페이지를 띄우기 위한 모바일 User-Agent
    private const string MobileUserAgent =
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/124.0.0.0 Mobile Safari/537.36";

    // 버튼 크기 + 우측/하단 여백
    private const double FabSize = 52;
    private const double FabMargin = 16;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_LoadedFab()
    {
        // WebView2(HWND) 위에 항상 보이도록 Popup 으로 플로팅 버튼 표시
        fabPopup.CustomPopupPlacementCallback = PlaceFab;
        fabPopup.IsOpen = true;

        // 창 이동/크기 변경 시 위치 재계산
        SizeChanged += (_, _) => RefreshFab();
        LocationChanged += (_, _) => RefreshFab();
        StateChanged += (_, _) => RefreshFab();
        IsVisibleChanged += (_, _) => fabPopup.IsOpen = IsVisible;
    }

    private void RefreshFab()
    {
        if (!fabPopup.IsOpen) return;
        // 콜백을 다시 트리거하여 위치 갱신
        fabPopup.HorizontalOffset += 0.01;
        fabPopup.HorizontalOffset -= 0.01;
    }

    private CustomPopupPlacement[] PlaceFab(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset)
    {
        // 대상(rootGrid) 좌상단 기준으로 우측 하단 모서리에 배치
        var x = targetSize.Width - FabSize - FabMargin;
        var y = targetSize.Height - FabSize - FabMargin;
        return new[] { new CustomPopupPlacement(new System.Windows.Point(x, y), PopupPrimaryAxis.None) };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MainWindow_LoadedFab();

        // 쿠키/세션 유지를 위해 사용자 데이터 폴더를 고정
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenRouterBalance", "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await webView.EnsureCoreWebView2Async(env);

        var settings = webView.CoreWebView2.Settings;
        settings.UserAgent = MobileUserAgent;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsZoomControlEnabled = true;

        webView.CoreWebView2.Navigate(TargetUrl);
    }

    private void FabReload_Click(object sender, RoutedEventArgs e)
    {
        // 안드로이드 버전과 동일하게 대상 경로로 재접속
        if (webView.CoreWebView2 != null)
            webView.CoreWebView2.Navigate(TargetUrl);
    }

    // 메인 윈도우를 닫으면 종료하지 않고 트레이로 숨김
    protected override void OnClosing(CancelEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        if (!app.ReallyExit)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
