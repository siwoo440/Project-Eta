# 68일차 : 시스템 UI 정식화·Pause·최초 튜토리얼 및 저장 피드백 통합

## 개발 목표

68일차에서는 전투와 런 진행 중 항상 접근해야 하는 시스템 UI를 정식화하고, 최초 플레이 안내와 저장·불러오기 피드백을 하나의 흐름으로 정리했다.

이번 일차의 중심은 다음과 같다.

- Boot 로딩 화면 정식화
- Battle ESC Pause 메뉴 정식화
- Pause 내부 조작법·설정·메인 메뉴 이동 통합
- Pause 중 게임 시간과 전투·RouteMap 입력 중단
- 최초 플레이 5단계 튜토리얼 추가
- 튜토리얼 완료 상태 영구 저장
- 저장·불러오기 System Toast 추가
- 시스템 알림 Queue 기반 순차 처리
- 기존 설정 저장 데이터를 v3으로 마이그레이션
- Day68 EditMode 회귀 테스트 추가

68일차에서는 별도의 Scene·Prefab 의존을 늘리기보다 기존 시스템 소유자와 런타임 자동 생성 UI를 확장하는 방식으로 구성했다.

## Boot 로딩 화면 정식화

기존 Boot 초기화 흐름에 실제 로딩 상태를 보여주는 전용 UI를 추가했다.

Boot 화면에서는 다음 초기화 단계를 순서대로 표시한다.

- 설정 데이터 로딩
- 영구 성장 데이터 로딩
- Run Save 검사
- Continue 가능 여부 확인
- MainMenu 전환

화면에는 `PROJECT η` 제목과 현재 처리 상태, 회전 로딩 마크를 표시하며 `Time.unscaledTime`을 기준으로 최소 표시 시간을 보장한다.

설정은 `GameSettingsService.EnsureLoaded()`를 통해 Boot 단계에서 먼저 불러오며, 이후 영구 성장과 Run Save 상태를 확인한 뒤 MainMenu로 이동한다.

## Battle Pause 시스템 정식화

기존 설정 Overlay를 전투 중 시스템 메뉴 역할까지 담당하도록 확장했다.

ESC 입력 시 다음 Pause 화면으로 진입한다.

- 계속하기
- 조작법
- 설정
- 메인 메뉴

Pause 진입 시 현재 `Time.timeScale`과 입력 컴포넌트 상태를 저장한 뒤 다음 처리를 수행한다.

- `Time.timeScale = 0`
- `BoardInputController` 입력 비활성
- `RouteMapBoardController` 입력 비활성

Pause 종료 시에는 기존에 저장한 상태를 그대로 복원한다.

따라서 전투 화면과 RouteMap 화면에서 같은 Pause 시스템을 사용할 수 있으며, Pause 이전의 입력 활성 상태도 유지한다.

## Pause 화면 내비게이션 상태 분리

Pause의 화면 전환 상태를 `BattlePauseNavigationState`로 분리했다.

현재 패널 상태는 다음 네 가지다.

- Closed
- Pause
- Controls
- Settings

ESC 뒤로가기는 현재 하위 화면에서 Pause로 돌아가고, Pause 최상위에서 다시 ESC를 누르면 게임으로 복귀하도록 구성했다.

표시 UI와 화면 전환 상태를 분리해 단순한 EditMode 테스트로 내비게이션 흐름을 검증할 수 있게 했다.

## 조작법 화면 통합

Pause 메뉴에 현재 프로젝트 입력 기준 조작법 화면을 추가했다.

현재 안내 대상은 다음과 같다.

- 카드·기물 선택
- 배치 턴 종료 및 행동 완료
- ESC Pause
- RouteMap 노드 확인·선택
- UI 뒤로가기

조작법 화면에서는 `튜토리얼 다시 보기`를 통해 최초 안내를 언제든 다시 실행할 수 있다.

## 최초 플레이 튜토리얼

Battle 씬 최초 진입 시 설정 데이터의 완료 상태를 확인해 자동 튜토리얼을 표시하도록 구성했다.

튜토리얼은 총 5단계다.

1. 기물 배치
2. 이동과 공격
3. King 보호
4. Route Map
5. 카드 성장

튜토리얼 표시 중에는 전투 진행이 뒤에서 계속되지 않도록 다음 상태를 임시 중단한다.

- `BattleController`
- `BoardInputController`
- `RouteMapBoardController`
- `Time.timeScale`

완료 또는 건너뛰기를 선택하면 최초 튜토리얼 완료 상태를 저장하고 기존 게임 상태를 복원한다.

## 설정 저장 버전 v3

`GameSettingsData.CurrentVersion`을 3으로 변경하고 `FirstTutorialCompleted` 필드를 추가했다.

기존 설정 데이터는 다음 원칙으로 정규화한다.

- 기존 해상도 유지
- 기존 화면 모드 유지
- 기존 UI Scale 유지
- v2 이후 오디오 설정 유지
- v3 이전 저장은 최초 튜토리얼 미완료로 처리

튜토리얼 완료 상태는 일반 설정 편집과 분리해 저장할 수 있도록 `GameSettingsService.MarkFirstTutorialCompleted()`를 추가했다.

설정 화면에서 값을 수정하거나 카테고리를 초기화해도 튜토리얼 완료 기록이 불필요하게 사라지지 않도록 편집 상태에도 해당 값을 유지한다.

## 시스템 알림 Queue

저장·불러오기와 같은 짧은 시스템 피드백을 공통 처리하는 `SystemNotificationQueue`를 추가했다.

알림은 등록 순서대로 하나씩 처리하며, 제목·본문·표시 시간을 하나의 메시지 단위로 관리한다.

이 구조를 통해 여러 저장 관련 알림이 같은 프레임에 발생해도 화면에서 서로 덮어쓰지 않도록 했다.

## System Toast UI

Battle 씬에서 공통으로 사용할 우측 상단 Toast UI를 추가했다.

현재 주요 출력 대상은 다음과 같다.

- 자동 저장 성공
- Continue 데이터 불러오기
- 최초 튜토리얼 완료

자동 저장 성공 시에는 현재 Stage와 흐름 정보를 함께 표시한다.

Continue 데이터가 존재하는 상태로 Battle에 진입하면 Stage·Flow·King HP를 포함한 `불러오기 완료` 알림을 표시한다.

최초 튜토리얼이 열려 있을 때는 Toast를 즉시 표시하지 않고 Queue에 유지해 튜토리얼 뒤에서 시스템 안내가 겹치지 않도록 했다.

## 자동 저장 피드백 연결

기존 `RunPersistenceController`의 안전 지점 저장 로직은 유지하고, 실제 저장 성공 시점에 System Toast를 연결했다.

저장 성공 시 다음 정보를 사용한다.

- 현재 RunFlowPhase
- 현재 Stage
- 현재 RouteMap Node

Continue로 복원된 안전 지점은 초기 연결 시 현재 체크포인트 키를 먼저 기록해, 복원 직후 동일 지점이 다시 저장되면서 `저장 완료` Toast가 중복 출력되는 상황을 줄였다.

## UI Scale 연동

68일차에 생성되는 런타임 Canvas도 기존 설정의 UI Scale 적용 대상에 포함했다.

적용 대상은 다음과 같다.

- Boot 로딩 UI
- Battle Pause
- 최초 튜토리얼
- System Toast

따라서 신규 시스템 UI도 기존 해상도·UI Scale 설정 흐름을 그대로 사용한다.

## EditMode 테스트 추가

68일차 구조에서 데이터와 상태 전환을 검증하기 위한 EditMode 테스트를 추가했다.

주요 테스트 대상:

- `GameSettingsData` v3 정규화
- 구버전 설정의 최초 튜토리얼 상태 마이그레이션
- Pause 화면 진입·하위 화면·뒤로가기
- System Notification Queue의 등록·순차 소비

런타임 UI 자체보다 재사용 가능한 상태 객체와 데이터 정규화 규칙을 우선 테스트 대상으로 분리했다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Settings/BattlePauseNavigationState.cs`
- `Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs`
- `Assets/ProjectEta/Scripts/UI/SystemNotificationQueue.cs`
- `Assets/ProjectEta/Scripts/UI/SystemToastUI.cs`
- `Assets/ProjectEta/Tests/EditMode/Day68GameSettingsDataTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day68PauseNavigationTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day68SystemNotificationQueueTests.cs`
- 신규 Unity 파일의 `.meta`
- `Devlogs/Day68/README.md`

### 수정

- `Assets/ProjectEta/Scripts/Run/RunPersistenceController.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/BootController.cs`
- `Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsData.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsEditState.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsService.cs`

### 삭제

- 없음

## 결과

68일차 작업으로 Boot부터 Battle 진입 이후까지 필요한 시스템 UI 흐름을 한 단계 정식화했다.

전투와 RouteMap에서 동일한 ESC Pause를 사용할 수 있게 되었고, Pause 내부에서 조작법·설정·메인 메뉴 이동을 처리한다. Pause와 최초 튜토리얼은 현재 게임 시간과 입력 상태를 보존한 채 중단·복원하도록 구성했다.

최초 플레이에서는 5단계 튜토리얼을 자동 표시하고 완료 상태를 설정 데이터에 저장한다. 이후에는 Pause의 조작법 메뉴를 통해 같은 안내를 다시 확인할 수 있다.

자동 저장과 Continue 복원에는 공통 System Toast를 연결했으며, Queue 기반으로 여러 시스템 알림이 순서대로 표시되도록 정리했다.

## 검증 상태

GitHub 최신 커밋 기준으로 67일차 커밋에서 68일차 커밋까지 1개 커밋이 추가되어 있으며, 68일차 변경 파일은 총 20개다.

변경 내역은 생성 파일과 수정 파일로만 구성되어 있고 삭제 파일은 없다.

GitHub 커밋 상태 검사와 연결된 Workflow Run은 현재 등록된 결과가 없어, GitHub CI 기준의 빌드·테스트 성공 여부는 확인할 수 없다.

Unity Editor 실행 환경에서의 실제 컴파일과 EditMode Test Runner 결과는 별도 확인이 필요하다.
