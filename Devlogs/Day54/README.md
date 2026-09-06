# 54일차 : Boot·메인 메뉴·씬 전환 및 런 진입 안정화

## 개발 목표

53일차까지 연결한 전체 로그라이트 루프를 실제 게임 실행 흐름에서 안정적으로 시작하고 종료할 수 있도록 런타임 진입 구조를 정리한다.

54일차의 핵심은 새로운 전투 규칙을 추가하는 것이 아니라 다음 실행 흐름을 하나의 정식 런타임 경로로 만드는 것이다.

`Boot → MainMenu → Battle → Run 진행 → Completed / Failed → MainMenu`

또한 직접 `Battle` 씬에서 테스트할 때와 `Boot → MainMenu → Battle` 순서로 진입할 때 전투 환경과 런타임 관리자가 동일하게 준비되도록 초기화 차이를 제거한다.

## SceneFlowController

씬 전환 규칙을 `SceneFlowController`에 모았다.

정식 런타임 씬은 다음 세 개를 사용한다.

- `Boot`
- `MainMenu`
- `Battle`

공통 전환 API는 다음 역할을 담당한다.

- Boot에서 MainMenu 진입
- 새 게임 시작
- 안전 저장 데이터 이어하기
- 런 종료 후 MainMenu 복귀
- 게임 종료
- 중복 씬 전환 차단

`SceneTransitionGate`를 통해 하나의 씬 전환이 완료되기 전에 동일 입력이 여러 번 들어오는 상황을 차단한다.

## Boot 초기화

`BootController`를 추가해 게임 실행 직후 필요한 최소 초기화를 수행한다.

Boot 단계에서는 다음 상태를 먼저 확인한다.

- 영구 성장 데이터 로드
- 현재 Run Save의 이어하기 가능 여부
- MainMenu 씬 로드 가능 여부

초기화가 끝나면 자동으로 `MainMenu`로 이동한다.

Boot 씬은 실제 게임 콘텐츠를 직접 생성하지 않고 메타 진행과 런 저장 상태를 준비한 뒤 MainMenu로 넘기는 진입점 역할만 담당한다.

## MainMenu

`MainMenuController`를 추가해 런타임 메인 메뉴를 구성했다.

현재 메뉴에서 제공하는 기능은 다음과 같다.

- 새 게임
- 이어하기
- 영구 성장
- 설정
- 게임 종료

`이어하기` 버튼은 단순히 `run_save.json` 파일 존재 여부만 보지 않고 실제 복원 가능한 안전 저장 데이터인지 검증한 뒤 활성화한다.

영구 성장 패널에서는 기존 Meta Progress의 토큰, 기물, 킹, 패시브 해금 상태를 그대로 사용한다.

설정 화면은 54일차에서는 메뉴 골격만 제공하며 실제 해상도, 음량, 그래픽 옵션은 이후 UI/UX 단계에서 확장한다.

## 이어하기 안전성 검증

`RunSaveSystem`에 `CanContinue`와 `IsContinueDataValid()`를 추가했다.

자동 이어하기를 허용하려면 저장 데이터가 다음 조건을 만족해야 한다.

- 현재 `RunSaveData.CurrentVersion`과 동일한 저장 버전
- 유효한 `runId`
- Stage 1~10 범위의 `currentRound`
- Route Map 노드 데이터 존재
- 현재 노드 ID 존재
- 현재 노드와 `currentDepth` 일치
- `currentRound`와 Route Map 깊이 일치

현재 자동 이어하기를 허용하는 안전 지점은 다음과 같다.

- 다음 노드를 아직 선택하지 않은 `Map`
- 선택 노드와 현재 King 노드가 일치하는 `Reward`
- 선택 노드와 현재 King 노드가 일치하는 `Shop`
- 선택 노드와 현재 King 노드가 일치하는 `Event`

`Battle`, `Completed`, `Failed` 상태는 자동 이어하기 대상으로 사용하지 않는다.

## 새 게임과 기존 Run Save

새 게임을 선택하면 이전 Run Save가 남아 새 런에 섞이지 않도록 `TryDeleteForNewRun()`을 통해 기존 저장 파일과 임시 저장 파일을 먼저 정리한다.

삭제가 실패한 경우 새 게임 진입을 중단해 이전 런이 의도치 않게 복원되는 상황을 차단한다.

영구 성장 데이터는 Run Save와 분리되어 있으므로 새 게임을 시작해도 Meta Progress는 유지된다.

## 런 종료 후 MainMenu 복귀

`RunResultMainMenuController`를 추가했다.

현재 `RunState.CurrentFlowPhase`가 다음 상태에 들어가면 결과 UI를 표시한다.

- `Completed`
- `Failed`

결과 UI에서 `메인 메뉴` 버튼을 누르면 `MainMenu` 씬으로 돌아간다.

씬 전환 중에는 버튼 입력을 잠가 중복 로드를 방지한다.

## SceneRuntimeBootstrap

`SceneRuntimeBootstrap`을 세션 공통 런타임 부트스트랩으로 추가했다.

이 객체는 `DontDestroyOnLoad`로 유지되며 `sceneLoaded` 이벤트를 통해 현재 씬에 필요한 런타임 관리자를 자동으로 보장한다.

Boot에서는 `BootController`, MainMenu에서는 `MainMenuController`, Battle에서는 전투·지도·보상·상점·이벤트·메타·킹·저장 관련 관리자를 준비한다.

이를 통해 씬에 모든 런타임 관리자를 수동으로 배치하지 않아도 동일한 실행 구조를 유지한다.

## Boot 경유 Battle 초기화 회귀 수정

54일차 검증 과정에서 직접 `Battle` 씬으로 시작할 때와 `Boot → MainMenu → Battle`로 진입할 때 화면 구성이 달라지는 문제가 확인됐다.

기존 37~41일차 일부 시스템은 `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 기반 자동 생성에 의존하고 있었다.

첫 씬이 Battle인 경우에는 정상 생성되지만 플레이 세션 중 나중에 Battle을 로드하면 해당 자동 생성기가 다시 호출되지 않아 전투방, 앉은 카메라, 2x2 보스, 보스 AI·HP UI 등의 초기화가 누락될 수 있었다.

`SceneRuntimeBootstrap.EnsureBattleRuntime()`에서 다음 레거시 런타임을 직접 보장하도록 보강했다.

- `Day41BattleRoomBootstrap`
- `LargePieceLifecycleController`
- `LargePiecePlayerAttackBridge`
- `LargePieceTurnEndStatusBridge`
- `EnemyAITurnDriver`
- `PrototypeBoss37Spawner`

이제 Boot 경유 Battle 진입도 직접 Battle 테스트와 같은 전투 환경 초기화 경로를 사용한다.

## Build Settings 정리

정식 실행 순서를 다음과 같이 고정했다.

1. `Assets/ProjectEta/Scenes/Boot.unity`
2. `Assets/ProjectEta/Scenes/MainMenu.unity`
3. `Assets/ProjectEta/Scenes/Battle.unity`

기존 개발용 `SampleScene`과 `Test`는 Build Settings의 정식 런타임 목록에서 제외했다.

씬 파일 자체는 개발 테스트를 위해 프로젝트에 남아 있을 수 있지만 실제 빌드 시작 순서에는 포함하지 않는다.

## Day54SceneFlowTests

54일차 씬 흐름과 회귀 조건을 검증하기 위해 `Day54SceneFlowTests`를 추가했다.

주요 검증 항목은 다음과 같다.

- 중복 씬 전환 차단
- 안전한 Map 체크포인트 이어하기 허용
- Reward / Shop / Event 안전 체크포인트 이어하기 허용
- Battle / 구버전 / 불완전 저장 데이터 이어하기 차단
- Boot 경유 Battle에서 필요한 레거시 자동 생성기 보장
- Build Settings의 Boot → MainMenu → Battle 순서 검증

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/SceneFlow/BootController.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneFlowController.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneTransitionGate.cs`
- `Assets/ProjectEta/Scripts/UI/MainMenuController.cs`
- `Assets/ProjectEta/Scripts/UI/RunResultMainMenuController.cs`
- `Assets/ProjectEta/Tests/EditMode/Day54SceneFlowTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Run/RunSaveSystem.cs`
- `ProjectSettings/EditorBuildSettings.asset`

### 삭제

정식 Build Settings 목록에서 개발용 `SampleScene`과 `Test`를 제외했다. 씬 파일 자체를 삭제한 것은 아니다.

## 결과

54일차 작업으로 프로젝트의 정식 실행 흐름이 `Boot → MainMenu → Battle` 구조로 정리됐다.

새 게임과 이어하기가 Run Save 규칙과 연결됐고, 영구 성장 상태는 MainMenu에서 확인·해금할 수 있으며, 런 종료 후 다시 MainMenu로 복귀할 수 있다.

또한 Boot를 경유해 Battle을 늦게 로드하는 경우에도 기존 전투방, 카메라, 대형 보스, AI·보스 UI 관련 런타임 관리자가 다시 준비되도록 보강해 직접 Battle 실행과 정식 실행 경로의 초기화 차이를 줄였다.

55일차에서는 새 기능 추가보다 실제 Boot 시작 기준으로 새 게임, 이어하기, Stage 1~10 진행, 실패, 클리어, 재시작을 반복하면서 남은 통합 회귀를 정리하는 안정화 작업을 이어간다.

## 검증 상태

최신 Day54 커밋의 변경 파일과 주요 호출 관계를 다시 확인했다.

GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 저장소 소스 구조와 정적 호출 관계는 확인했지만 Unity Editor 컴파일 및 EditMode/PlayMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
