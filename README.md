# OpenRouter Balance (Windows)

OpenRouter의 크레딧 조회/충전 페이지를 즉시 확인할 수 있는 윈도우 웹뷰 앱입니다.
([안드로이드 버전](https://github.com/pawprint0706/open-router-balance) 동작을 동일하게 옮긴 데스크톱 버전)

대상 페이지: `https://openrouter.ai/settings/credits`

## 주요 기능

- **모바일 페이지 렌더링** — 모바일 User-Agent로 접속하여 모바일 레이아웃 표시
- **고정 창 크기** — 480 × 800 (세로형 모바일 비율), 리사이즈 불가
- **트레이 상주** — 시작 시 트레이 아이콘 생성, 메인 창을 닫으면 종료되지 않고 트레이로 숨김
- **종료 제한** — 트레이 아이콘 우클릭 → "종료" 메뉴로만 종료
- **새로고침 플로팅 버튼** — 화면 우측 하단 FAB, 클릭 시 대상 페이지 재접속
- **시작프로그램 등록** — 트레이 우클릭 메뉴의 체크 항목. 시작 시 레지스트리를 검사해 상태 반영, 체크/해제 시 등록/해제
- **세션 유지** — 쿠키/로그인 세션을 로컬 사용자 데이터 폴더에 보존

## 트레이 메뉴

| 항목 | 동작 |
|------|------|
| 열기 | 메인 창 표시 (트레이 아이콘 좌클릭과 동일) |
| 시작프로그램 등록 ☑ | Windows 시작 시 자동 실행 등록/해제 |
| 종료 | 앱 완전 종료 |

## 기술 스택

- .NET 10 (WPF)
- [Microsoft.Web.WebView2](https://learn.microsoft.com/microsoft-edge/webview2/)
- System.Windows.Forms `NotifyIcon` (트레이)

## 빌드

```powershell
dotnet build -c Release
```

## 단일 실행 파일 게시

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

결과물: `bin\Release\net10.0-windows\win-x64\publish\OpenRouterBalance.exe` (약 72MB, .NET 런타임 포함)

> 배포 시 `OpenRouterBalance.exe` 하나만 복사하면 됩니다.

## 요구 사항

- Windows 10/11
- **WebView2 런타임** — Windows 11에는 기본 내장. (.NET 런타임은 단일 exe에 포함되어 별도 설치 불필요)

## 프로젝트 구조

```
OpenRouterBalance/
├─ App.xaml(.cs)          트레이 아이콘 · 컨텍스트 메뉴 · 시작프로그램 레지스트리
├─ MainWindow.xaml(.cs)   WebView2 · 새로고침 플로팅 버튼
├─ app.manifest           Per-Monitor DPI 인식
├─ app.ico                앱/창/트레이 아이콘
└─ OpenRouterBalance.csproj
```

## 구현 메모

- WebView2는 네이티브 HWND라 일반 WPF 오버레이가 가려지므로, 플로팅 버튼은 `Popup` + `CustomPopupPlacementCallback`으로 항상 위에 표시합니다.
- 시작프로그램 등록은 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`의 `OpenRouterBalance` 값을 사용합니다.
- 창이 닫혀도 앱이 유지되도록 `ShutdownMode=OnExplicitShutdown`을 사용하며, 트레이 "종료"에서만 실제 종료합니다.

## 라이선스 / 상표

OpenRouter 로고 및 상표는 OpenRouter, Inc.의 자산입니다. 본 앱은 비공식 클라이언트로, 개인 편의를 위한 용도입니다.
