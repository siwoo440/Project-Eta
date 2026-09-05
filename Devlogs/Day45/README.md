# 45일차 : 스테이지 노드 생성·판 전환 및 전투 재구성 구축

## 개발 목표

44일차에 구현한 동일 10×10 체스판의 경로 지도와 킹 이동을 실제 다음 스테이지 진입으로 연결한다.

킹이 경로 노드에 도착하면 `StageDefinitionId`를 실제 `StageDefinition`으로 해석하고, 스테이지 타입에 따라 전투·엘리트·보상·상점·이벤트·중간 보스·최종 보스 흐름으로 분기하도록 구성한다.

전투형 스테이지에서는 같은 Battle 씬을 유지한 채 새로운 `BattleState`를 만들고 기존 `BoardView`, `BoardInputController`, 카드 UI, 턴 시스템을 새 전투 상태에 다시 연결한다.

## 주요 개발 내용

### StageRouteGenerator

깊이별 다음 스테이지 노드를 생성하는 `StageRouteGenerator`를 추가했다.

일반 깊이에서는 2~3개의 다음 노드를 생성하고, 현재 킹 위치에서 체스 킹의 8방향 1칸 규칙 안에 들어오도록 좌표를 구성한다.

5단계는 `MidBoss`, 10단계는 `FinalBoss` 단일 노드로 강제한다.

일반 분기에는 다음 스테이지 타입을 사용한다.

- Battle
- Elite
- Reward
- Shop
- Event

### StageDefinitionCatalog

`StageNode.StageDefinitionId`를 실제 `StageDefinition`으로 변환하는 런타임 카탈로그를 추가했다.

현재 프로토타입에서는 기존 `RoundDefinition` 리소스를 재사용한다.

일반·엘리트 전투는 `PrototypeRound36`, 중간·최종 보스는 `PrototypeBossRound40`을 기본 전투 데이터로 사용한다.

보상·상점·이벤트 노드는 전투용 `RoundDefinition` 없이 비전투 흐름으로 분기한다.

### StageDefinition 확장

기존 `StageDefinition` 구조를 유지하면서 런타임 설정을 지원하도록 확장했다.

스테이지 정보는 다음 항목을 가진다.

- StageId
- DisplayName
- StageType
- RoundDefinition
- RewardProfileId

`Battle`, `Elite`, `MidBoss`, `FinalBoss`는 `RequiresBattle`이 true가 되어 실제 전투판 재구성 대상으로 처리된다.

### RouteMapState 실제 분기 생성 연결

기존 `PreparePrototypeAfterBattle()` 호출부를 유지하면서 내부 생성 로직을 `StageRouteGenerator`와 연결했다.

직전 선택 경로의 X 위치를 유지해 현재 노드를 만들고, 다음 깊이의 2~3개 후보를 생성해 `NextNodeIds`에 연결한다.

기존 43·44일차의 경로 선택 인터페이스를 크게 변경하지 않고 45일차의 실제 스테이지 타입을 연결했다.

### 지도 노드 타입별 표시

`RouteMapBoardController`가 각 노드의 `StageDefinition`을 조회해 스테이지 타입별 색상을 다르게 표시하도록 확장했다.

일반 전투, 엘리트, 보상, 상점, 이벤트, 중간 보스, 최종 보스를 지도에서 시각적으로 구분할 수 있다.

킹 이동 연출이 끝난 뒤 `StageNodeSelected` 이벤트를 발생시켜 실제 스테이지 전환을 시작한다.

### StageTransitionController

지도에서 선택한 노드를 실제 게임 흐름으로 연결하는 `StageTransitionController`를 추가했다.

진입 흐름은 다음과 같다.

`StageNode 선택 → StageDefinition 해석 → 현재 깊이 갱신 → 이전 전투 런타임 정리 → 전투형/비전투형 분기`

전투형 노드는 새로운 `BattleState`를 만들고 기존 Battle 씬을 재사용한다.

비전투형 노드는 Reward·Shop·Event 흐름으로 전환한다.

### 새 BattleState 재구성

전투형 스테이지 진입 시 `RunState.ResetBattleState()`를 호출해 새 `BoardState`와 `HandState`를 생성한다.

이후 `BattleController.Initialize()`를 다시 호출해 기존 `BoardView`, `BoardInputController`, Hand UI, Deck UI, Fusion UI 등 기존 런타임 시스템을 새 상태에 재바인딩한다.

이전 전투의 화면 기물은 새 BattleState가 만들어진 경우 다시 표시하지 않고 제거한다.

### 다음 전투 손패 재구성

런 전체 카드 보유 상태는 유지하고 전투 임시 손패만 다시 구성한다.

다음 전투 시작 전 죽은 카드 더미를 보유 카드 풀에 복귀시키고 드로우 더미를 재구성한다.

플레이어 킹 카드를 먼저 손패에 넣은 뒤 기본 시작 손패 수까지 추가 드로우한다.

### TurnManager 재사용

같은 Battle 씬에서 다음 전투를 시작할 수 있도록 `TurnManager.ResetForNewBattle()`을 추가했다.

새 스테이지 진입 시 다음 상태를 초기화한다.

- DeploymentTurn
- TurnNumber 1
- InitialDeployment true
- InitialKingPlaced false
- HasPlayerActed false
- DeployedCardCount 0
- BattleOutcome None
- 플레이어 행동 연출 대기 상태 제거

기존 플레이어 공격 연출 지연 설정은 유지해 다음 전투에서도 기물 복귀 후 적 턴이 시작되는 흐름을 사용한다.

### StageBattleRuntimeController

선택된 전투형 `StageDefinition`의 `RoundDefinition`을 새 전투판에 적용하는 런타임 컨트롤러를 추가했다.

초기 적, 지정 턴 증원, 보스 구성, 턴 제한을 기존 전투 시스템에 연결한다.

엘리트 스테이지는 별도 전용 데이터 제작 전 프로토타입 단계에서 일반 전투 구성에 추가 적을 배치하는 방식으로 차이를 둔다.

보스 설정이 존재하면 기존 대형 기물 유틸리티를 사용해 점유 크기와 보스 시각을 적용한다.

### 비전투 스테이지 임시 진입

`Reward`, `Shop`, `Event`는 45일차에서 실제 콘텐츠를 완성하지 않고 정확한 스테이지 분기와 진입만 확인하도록 구성했다.

`StagePlaceholderUI`에서 현재 스테이지 종류를 표시하고 개발용 `계속` 버튼으로 완료 처리 후 다음 경로 지도로 복귀한다.

실제 카드 3개 보상 선택은 46일차, 상점과 이벤트 실제 기능은 47일차에서 연결한다.

### CS0104 Object 모호성 수정

`RouteMapBoardController`에 `System.Action` 이벤트를 추가하면서 `using System`과 `UnityEngine.Object`의 축약형 `Object`가 충돌해 `CS0104` 오류가 발생했다.

`using System`을 제거하고 이벤트 타입만 `System.Action<StageNode>`로 명시해 기존 `Object.FindFirstObjectByType` 호출이 다시 `UnityEngine.Object`로 해석되도록 수정했다.

### 지도 모드 BattleController 비활성화 수정

지도 모드에서 전투 UI를 숨기는 기존 처리에서 UI 컴포넌트가 붙어 있는 GameObject 자체를 비활성화하고 있었다.

여러 UI 컴포넌트가 `BattleController` GameObject에 함께 붙어 있어 결과적으로 `BattleController` 자체가 비활성화됐다.

이 상태에서 다음 전투의 `TurnManager.ResetForNewBattle()`이 `TurnChanged`를 발생시키면 `DeploymentTurnBannerUI`가 비활성 GameObject에서 코루틴을 시작하려 해 오류가 발생했다.

수정 후에는 시스템 호스트를 비활성화하지 않고 해당 컴포넌트 아래의 실제 `Canvas` GameObject만 숨긴다.

따라서 지도 모드에서도 `BattleController`, 턴 이벤트, 런타임 코루틴은 활성 상태를 유지한다.

### 회귀 테스트

`Day45StageFlowTests`에서 다음 흐름을 검증하도록 구성했다.

- 일반 깊이 2~3개 분기 생성
- 5단계 MidBoss 강제
- 10단계 FinalBoss 강제
- 기존 경로 선택 규칙 유지
- Reward·Shop·Event RunFlow 상태
- TurnManager 새 전투 초기화

`Day45RouteMapUiVisibilityTests`에서는 지도 UI 숨김 시 BattleController와 같은 시스템 호스트는 활성 상태를 유지하고 실제 Canvas만 숨겨졌다가 복원되는지 검증한다.

GitHub에는 별도 CI 상태 검사가 등록되어 있지 않으므로 실제 Unity 컴파일과 EditMode 테스트 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/StageRouteGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/StageDefinitionCatalog.cs`
- `Assets/ProjectEta/Scripts/Run/StageTransitionController.cs`
- `Assets/ProjectEta/Scripts/Run/StageBattleRuntimeController.cs`
- `Assets/ProjectEta/Scripts/Run/StageDefinition.cs`
- `Assets/ProjectEta/Scripts/Run/RouteMapState.cs`
- `Assets/ProjectEta/Scripts/Run/RunFlowState.cs`
- `Assets/ProjectEta/Scripts/Board/RouteMapBoardController.cs`
- `Assets/ProjectEta/Scripts/UI/StagePlaceholderUI.cs`
- `Assets/ProjectEta/Scripts/Battle/TurnManager.cs`
- `Assets/ProjectEta/Tests/EditMode/Day45StageFlowTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day45RouteMapUiVisibilityTests.cs`

## 결과

전투 승리 후 경로 지도에서 선택한 노드가 단순한 좌표 선택으로 끝나지 않고 실제 `StageDefinition` 기반 스테이지 진입으로 연결되었다.

일반·엘리트·보스 전투는 같은 Battle 씬에서 새 `BattleState`와 기존 `RoundDefinition`을 이용해 다시 구성할 수 있게 됐다.

Reward·Shop·Event는 실제 기능 구현 전 단계까지 진입 경로가 준비됐으며, 이후 46·47일차 콘텐츠를 같은 StageDefinition 전환 구조 위에 추가할 수 있다.

다음 46일차에서는 전투 승리 또는 Reward 노드에서 카드 3개 후보를 표시하고 하나를 선택해 런 덱에 추가한 뒤 다시 경로 지도로 복귀하는 카드 보상 흐름을 구현한다.
