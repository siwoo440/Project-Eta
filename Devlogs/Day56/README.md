# 56일차 : 재사용 설정 패널 및 인게임 화면 설정 시스템 구현

## 개발 목표

55일차에서 정식화한 MainMenu의 설정 진입 구조를 실제 설정 시스템으로 확장하고, 별도 Settings Scene을 만들지 않고 동일한 설정 패널을 MainMenu와 Battle 런타임에서 재사용할 수 있도록 구성한다.

56일차의 핵심 목표는 다음과 같다.

- 해상도 설정
- 전체화면 창 / 창모드 설정
- UI Scale 설정
- 적용 / 취소 / 초기화
- 설정 저장 / 복원
- MainMenu와 인게임에서 동일한 설정 패널 재사용
- 인게임 설정 중 전투·RouteMap 입력 및 게임 진행 일시 정지

## 설정 데이터 구조

`GameSettingsData`를 추가해 화면과 UI 관련 설정을 별도의 직렬화 가능한 데이터로 분리했다.

현재 저장하는 값은 다음과 같다.

- ResolutionWidth
- ResolutionHeight
- ScreenMode
- UiScale

기본값은 다음과 같다.

- 1920 × 1080
- FullScreenWindow
- UI Scale 1.0

잘못된 저장값이 들어와도 최소 해상도와 UI Scale 범위를 벗어나지 않도록 `Normalized()`에서 값을 보정한다.

현재 UI Scale 허용 범위는 0.75~1.35다.

## 설정 편집 상태

`GameSettingsEditState`를 추가해 실제 적용된 설정과 사용자가 현재 편집 중인 설정을 분리했다.

상태는 다음처럼 관리한다.

- `Saved`: 마지막으로 적용된 설정
- `Editing`: 현재 UI에서 편집 중인 설정
- `IsDirty`: 적용되지 않은 변경 존재 여부

설정 패널의 적용·취소·초기화 동작은 이 편집 상태를 기준으로 처리한다.

### 적용

편집값을 현재 저장값으로 확정하고 실제 화면에 반영한 뒤 `settings.json`에 저장한다.

### 취소

적용하지 않은 편집값을 폐기하고 마지막 저장값으로 돌아간다.

UI Scale을 미리보기 중이었다면 저장된 배율로 복구한다.

### 초기화

편집값을 프로젝트 기본 설정으로 변경한다.

초기화만으로 바로 저장하지 않으며 사용자가 `적용`을 눌러야 실제 설정과 저장 파일에 반영된다.

## 해상도 목록

`GameSettingsResolutionCatalog`를 추가했다.

`Screen.resolutions`에서 현재 시스템이 제공하는 해상도 후보를 가져오고 다음 규칙으로 정리한다.

- 동일한 Width × Height 중복 제거
- 최소 지원 크기 미만 제외
- 총 픽셀 수가 높은 해상도부터 정렬
- 저장되어 있는 현재 해상도는 후보 목록에 포함

Refresh Rate는 56일차 UI에서는 별도 선택 항목으로 다루지 않고 화면 크기 기준으로만 정리한다.

## 설정 저장 시스템

`GameSettingsService`를 추가했다.

설정 파일은 Run Save 및 Meta Progress와 분리해 다음 이름으로 저장한다.

`settings.json`

주요 역할은 다음과 같다.

- 저장 설정 로드
- 저장 파일이 없거나 손상된 경우 기본값 fallback
- 해상도와 화면 모드 적용
- UI Scale 적용
- 설정 JSON 저장
- UI Scale 미리보기
- 새 Canvas에 현재 UI Scale 재적용

해상도와 화면 모드는 `Screen.SetResolution()`으로 적용한다.

## UI Scale 적용

프로젝트의 `Scale With Screen Size` Canvas에 UI Scale을 적용할 수 있도록 `GameSettingsCanvasScaleMarker`를 사용한다.

각 CanvasScaler가 처음 가진 기준 해상도를 저장하고 UI Scale에 따라 `referenceResolution`을 조절한다.

이를 통해 MainMenu뿐 아니라 Battle에서 런타임 생성되는 UI에도 같은 UI Scale 설정을 적용한다.

## 설정 런타임 부트스트랩

`GameSettingsRuntimeBootstrap`을 추가했다.

첫 Scene이 로드되기 전에 설정을 읽어 화면 설정을 적용하고, Scene이 바뀔 때마다 새로 생성된 Canvas에 저장된 UI Scale을 다시 반영한다.

부트스트랩은 `DontDestroyOnLoad`로 유지되며 플레이 세션 동안 하나만 사용한다.

## 재사용 설정 패널

`SettingsPanelController`를 추가해 실제 설정 UI를 MainMenu와 Battle에서 공동 사용하도록 구성했다.

현재 UI 항목은 다음과 같다.

- 해상도
- 화면 모드
- UI Scale
- 초기화
- 취소
- 적용
- 현재 변경 상태 표시

해상도와 화면 모드는 좌우 선택 버튼으로 순환한다.

UI Scale은 Slider로 조절하며 변경 즉시 화면에 미리보기로 반영된다.

변경 사항이 없을 때는 `적용` 버튼을 비활성화한다.

## MainMenu 설정 연결

55일차의 `SettingsRoot` 구조를 유지하면서 `MainMenuSettingsBridge`를 추가했다.

MainMenu가 생성한 기존 설정 안내 Placeholder를 숨기고 같은 `SettingsRoot`에 `SettingsPanelController`를 연결한다.

따라서 MainMenu Scene을 직접 수정하거나 새 Settings Scene을 만들지 않고 기존 메뉴 내비게이션을 그대로 사용한다.

설정 패널에서 취소를 누르면 기존 55일차 MainMenu의 뒤로가기 흐름을 사용해 Main 화면으로 복귀한다.

## 인게임 설정

`BattleSettingsOverlayController`를 추가했다.

Battle Scene에서는 ESC로 설정 패널을 열고 다시 ESC를 누르면 닫는다.

설정이 열리면 다음 상태를 저장하고 일시적으로 차단한다.

- BoardInputController 활성 상태
- RouteMapBoardController 활성 상태
- 현재 Time.timeScale

설정 화면이 열려 있는 동안 다음 처리를 적용한다.

- BoardInputController 비활성화
- RouteMapBoardController 비활성화
- `Time.timeScale = 0`

설정을 닫으면 이전 입력 활성 상태와 TimeScale을 복원한다.

따라서 같은 Battle Scene에서 진행되는 전투와 RouteMap 흐름 중에도 플레이어가 설정을 변경할 수 있다.

## SceneRuntimeBootstrap 확장

기존 씬 초기화 구조에 다음 두 컴포넌트를 추가했다.

MainMenu:

- `MainMenuSettingsBridge`

Battle:

- `BattleSettingsOverlayController`

기존 Boot / MainMenu / Battle 3개 Scene 구조와 Build Settings는 변경하지 않았다.

## 테스트 소스

`Day56GameSettingsTests`를 추가했다.

현재 포함한 검증 항목은 다음과 같다.

- Apply / Cancel / Reset 편집 상태와 Dirty 상태
- 잘못된 설정값 Normalized 보정
- 해상도 후보 중복 제거 및 정렬
- Windowed / FullScreenWindow 화면 모드 편집
- SceneRuntimeBootstrap의 MainMenu 설정 Bridge 주입
- SceneRuntimeBootstrap의 Battle 설정 Overlay 주입

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Settings/GameSettingsData.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsEditState.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsResolutionCatalog.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsService.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsRuntimeBootstrap.cs`
- `Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs`
- `Assets/ProjectEta/Scripts/Settings/MainMenuSettingsBridge.cs`
- `Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs`
- `Assets/ProjectEta/Tests/EditMode/Day56GameSettingsTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`

### 삭제

없음.

## 결과

56일차 작업으로 설정 기능이 MainMenu에 종속된 임시 화면이 아니라 여러 게임 상태에서 재사용 가능한 독립 패널과 서비스 구조로 분리됐다.

게임 시작 시 저장된 화면 설정을 자동으로 불러오며, 사용자는 MainMenu뿐 아니라 Battle Scene의 플레이 도중에도 ESC로 같은 설정 패널을 열어 해상도·화면 모드·UI Scale을 변경할 수 있다.

설정은 Run Save 및 Meta Progress와 독립된 `settings.json`에 저장되며, UI Scale은 적용 전 미리보기를 지원한다.

57일차에서는 현재 설정 구조를 유지하면서 Master / BGM / SFX 볼륨 설정과 오디오 저장·적용 흐름을 확장한다.

## 검증 상태

GitHub의 최신 56일차 커밋에 Settings 시스템 파일, SceneRuntimeBootstrap 연결, Day56GameSettingsTests가 포함된 것을 확인했다.

최신 커밋에는 연결된 GitHub commit status 검사가 등록되어 있지 않다.

따라서 GitHub에 올라온 변경 내용과 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
