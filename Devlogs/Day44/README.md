# 44일차 : 체스판 경로 지도·킹 이동 및 전투 연출 배속·턴 전환 개선

## 개발 목표

43일차에 구축한 `RunFlowState`·`RouteMapState`·`StageNode` 기반을 실제 10×10 체스판 화면과 연결한다.

전투 승리 후 기존 체스판을 경로 지도 모드로 전환하고, 현재 위치의 플레이어 킹과 선택 가능한 다음 스테이지 노드를 표시해 킹이 연결된 인접 노드로 직접 이동할 수 있도록 한다.

추가로 전투 연출을 확인하기 쉽도록 1·2·3배속 순환 버튼을 구성하고, 플레이어 공격 연출이 끝나기 전에 적 AI가 움직이던 턴 전환 흐름을 보정한다.

## 주요 개발 내용

### 동일 체스판의 경로 지도 전환

전투용으로 사용하던 기존 10×10 체스판을 별도 Map 씬 없이 그대로 경로 지도 화면으로 재사용하도록 구성했다.

`BoardMode.Map`에 진입하면 기존 전투 입력과 전투 기물 표시를 임시 차단하고, `RouteMapState`가 가진 현재 위치와 다음 스테이지 후보를 보드 좌표에 맞춰 표시한다.

지도 모드에서는 전투 이동·공격 입력, 손패·덱·합성·개발용 전투 UI를 숨기고 현재 노드, 다음 선택 가능 노드, 경로선, 지도 전용 플레이어 킹을 표시한다.

### RouteMapBoardController와 RouteMapNodeView

`RouteMapBoardController`가 런의 `BoardMode` 변화를 감지해 Battle과 Map 표시를 전환한다.

`RouteMapNodeView`는 선택 가능, 마우스 오버, 선택 완료, 비선택 상태를 색으로 구분하며 한 노드를 선택하면 나머지 후보는 흐리게 표시한다.

### 지도용 킹과 이동 규칙

지도 킹은 전투 `PieceRuntimeState`와 분리된 표시 전용 오브젝트로 구성해 지도 이동이 전투 킹의 HP, 상태 이상, 공격 판정, BoardState 점유에 영향을 주지 않도록 했다.

이동은 현재 노드의 `NextNodeIds`에 연결되어 있으면서 X·Y 차이가 각각 1 이하인 인접 8방향 1칸일 때만 허용한다.

정상 이동 시 `KingMapPosition`, `CurrentNodeId`, `SelectedNodeId`, `CurrentDepth`, `Visited`를 갱신하며 한 지도 단계에서는 하나의 스테이지만 선택할 수 있다.

### 전투 배속 시스템

현재 프로젝트의 기존 전투 속도를 3배속 기준으로 정의했다.

- 1배속: `Time.timeScale = 1 / 3`
- 2배속: `Time.timeScale = 2 / 3`
- 3배속: `Time.timeScale = 1`

승리·패배 버튼 위에는 단일 배속 버튼을 두고 현재 배속을 글자로 표시한다. 클릭할 때마다 `1배속 → 2배속 → 3배속 → 1배속` 순서로 순환하며 Play Mode 시작 기본값은 기존 속도인 3배속이다.

### 치명 공격 이동 연출

치명 공격에서 공격자가 처치한 대상 칸을 논리적으로 즉시 점유해 순간이동처럼 보이는 문제를 `LethalAttackVisualBridge`로 보정했다.

공격 판정 전 원래 좌표를 기억하고, 치명 공격 시 화면상의 공격자를 원래 위치에서 다시 시작시킨 뒤 위로 떠오르며 목표 칸으로 이동하고 착지하도록 했다. 원거리 역할 기물은 해당 근접 전진 연출에서 제외한다.

### 플레이어 공격 연출 후 적 턴 시작

기존에는 플레이어 공격 판정 직후 `TryCompletePlayerAction()`이 바로 `EnemyTurn`으로 전환되어 플레이어 기물의 상승·접근·타격·복귀 연출과 적 AI 행동이 겹칠 수 있었다.

`TurnManager`에 플레이어 행동 완료와 실제 EnemyTurn 전환을 분리하는 지연 상태를 추가하고, `PlayerActionTurnDelayController`가 연출 대기 후 적 턴을 해제하도록 구성했다.

흐름은 `플레이어 행동 완료 → 추가 입력 차단 → 공격 연출 대기 → EnemyTurn 전환 → 적 AI 행동` 순서다.

전투가 연출 대기 중 승리·패배로 끝나면 예약된 적 턴 전환은 취소된다.

### 연출 완료 대기 시간 보정

기본 비치명 근접 공격 연출은 상승 0.12초, 접근 0.13초, 타격 0.08초, 복귀 0.15초로 총 0.48초다.

최신 커밋의 대기값 0.45초는 복귀 완료보다 0.03초 짧아 적이 약간 먼저 움직일 가능성이 있으므로, 44일차 마무리 단계에서 `PlayerVisualSettleSeconds`를 0.55초로 보정했다.

`WaitForSeconds`를 사용하므로 1·2·3배속의 `Time.timeScale` 변화와 함께 자연스럽게 느려지고 빨라진다.

### 회귀 테스트

`Day44RouteMapTests`에서 연결된 인접 노드 이동, 비연결·장거리 이동 차단, 선택 상태 갱신과 중복 선택 차단을 검증하도록 구성했다.

`Day44CombatSpeedTests`에서 기본 3배속, 1·2배속 시간 배율, 단일 버튼 순환 규칙과 잘못된 입력 차단을 검증하도록 구성했다.

`Day44PlayerActionTurnDelayTests`에서 지연 모드의 플레이어 추가 행동 차단, 연출 완료 후 EnemyTurn 진입, 전투 종료 시 예약된 EnemyTurn 취소를 검증하도록 구성했다.

GitHub에는 별도 CI 상태 검사가 등록되어 있지 않으므로 실제 Unity 컴파일과 EditMode 테스트 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/RouteMapState.cs`
- `Assets/ProjectEta/Scripts/Board/RouteMapBoardController.cs`
- `Assets/ProjectEta/Scripts/Board/RouteMapNodeView.cs`
- `Assets/ProjectEta/Scripts/Battle/CombatSpeedSettings.cs`
- `Assets/ProjectEta/Scripts/Battle/TurnManager.cs`
- `Assets/ProjectEta/Scripts/Pieces/LethalAttackVisualBridge.cs`
- `Assets/ProjectEta/Scripts/UI/DebugCombatSpeedButtons.cs`
- `Assets/ProjectEta/Scripts/UI/PlayerActionTurnDelayController.cs`
- `Assets/ProjectEta/Tests/EditMode/Day44RouteMapTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day44CombatSpeedTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day44PlayerActionTurnDelayTests.cs`

## 결과

43일차에서 데이터 상태로만 존재하던 경로 지도가 실제 10×10 체스판 위에 표시되고, 플레이어 킹으로 연결된 다음 스테이지를 직접 선택할 수 있게 되었다.

전투에서는 기존 속도를 3배속 기준으로 한 1·2·3배속 전환, 치명 공격자의 떠서 이동하는 연출, 플레이어 기물 복귀 완료 후 적 턴 진입을 추가해 전투 흐름을 눈으로 확인하기 쉬워졌다.

다음 45일차에서는 선택된 `StageNode`의 `StageDefinition`을 조회하고, 선택 결과에 따라 새로운 전투·엘리트·보상·상점·이벤트 스테이지로 체스판 상태를 재구성하는 기능을 연결한다.
