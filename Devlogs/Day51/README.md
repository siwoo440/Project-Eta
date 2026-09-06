# 51일차 : 런 진행·경로·킹·경제 저장 및 안전 지점 자동 복원

## 오늘의 목표

로그라이트 런을 중간에 종료해도 다음 실행에서 이어갈 수 있도록 기존 `RunSaveData`와 `RunSaveSystem`을 확장한다.

단순히 현재 스테이지만 저장하는 것이 아니라 경로 지도, 현재 킹 위치, 선택한 경로, 런 전용 Gold, 선택 킹, 카드 강화 상태와 상위 Run Flow까지 함께 보존한다.

또한 종료된 런을 다시 불러오거나 같은 런의 메타 보상이 중복 지급되는 문제를 막는 안전 장치를 추가한다.

## 런 세이브 포맷 v2

`RunSaveData`를 51일차 저장 포맷으로 확장했다.

기존 킹 HP, 현재 라운드, 덱, 손패, 보드 기물, 상태 이상, 합성 발견 기록과 함께 다음 정보를 추가로 저장한다.

- `saveVersion`
- `runId`
- `flowPhase`
- `runCurrency`
- `selectedKingArchetype`
- `currentStageDefinitionId`
- `RouteMapSaveData`
- 강화 수치를 포함한 `CardSaveData`

기존 저장 구조를 폐기하지 않고 필요한 필드를 추가하는 방식으로 확장했다.

## 경로 지도 저장

`RouteMapState`가 현재 런의 지도 진행을 저장·복원할 수 있도록 확장됐다.

저장 대상은 다음과 같다.

- Map Seed
- 현재 깊이
- 현재 노드 ID
- 선택 노드 ID
- 지도 위 King 좌표
- 실제 선택 경로
- 방문 노드 이력
- 현재 지도 노드 목록
- 각 노드의 StageDefinition ID
- 노드 간 연결 관계
- 방문 여부

지도 노드를 이동했을 때 선택 경로와 방문 이력을 별도로 기록해 런의 실제 이동 기록이 저장 데이터에 남도록 구성했다.

## 런 전용 Gold 저장

47일차에 추가했던 `RunEconomyState`를 런 세이브와 연결했다.

`RunEconomyService.Restore`를 통해 저장된 Gold를 복원하며 새 런에서 사용하는 시작 Gold와 저장 런의 현재 Gold를 구분한다.

상점이나 이벤트를 거친 뒤 게임을 다시 실행하더라도 해당 런의 재화 상태를 유지할 기반을 마련했다.

## 선택 킹 저장

49~50일차에 구현한 킹 선택 구조를 세이브와 연결했다.

`KingRunStateService.Restore`를 추가해 현재 선택한 `KingArchetype`을 복원한다.

선택한 킹 타입은 유지하지만 다음 상태는 전투 한정 상태이므로 복원 시 초기화한다.

- 공격형 킹 Rage
- 방어형 킹 Barrier
- 현재 턴 King 이동 여부
- 전략형 킹 카드 선택 진행 상태

이로써 런 전체 선택은 유지하면서 전투 내부 임시 상태가 다음 실행에 잘못 이어지는 것을 막는다.

## 런타임 카드 강화 저장

47일차 상점 카드 강화는 원본 `PieceDefinition`을 런타임 복제해 HP와 ATK를 변경하는 구조이므로 PieceId만 저장하면 강화 상태가 사라진다.

이를 위해 `CardSaveData`에 다음 값을 저장한다.

- PieceId
- DisplayName
- Base HP
- Base ATK

`RuntimeCardUpgradeService.CreateRestoredCard`가 원본 PieceDefinition을 기준으로 저장된 스탯의 런타임 복제 카드를 다시 생성한다.

원본과 동일한 카드라면 불필요한 복제를 만들지 않고 원본 정의를 그대로 사용한다.

## 안전 지점 자동 저장

새 `RunPersistenceController`를 Battle 씬에서 자동 생성하도록 구성했다.

전투 도중 아무 프레임이나 저장하지 않고 실제로 다시 진입하기 쉬운 안정적인 진행 지점만 자동 저장한다.

### 자동 저장 대상

- 다음 노드를 선택하기 전 `Map`
- 선택한 Reward 노드 진입 상태
- `Shop`
- `Event`

### 자동 저장 제외

- Battle 진행 중
- 지도 노드를 선택한 직후 Stage 전환 중
- Completed
- Failed

같은 Shop, Event, Reward 화면에 머무는 동안 매 프레임 저장하지 않도록 현재 진행 위치로 Checkpoint Key를 만들고 동일 지점의 반복 저장을 차단한다.

## 안전 지점 자동 복원

`BattleController` 시작 시 신규 `RunState`를 만들기 전에 `RunSaveSystem.TryLoadSafe`를 먼저 호출한다.

정상적인 v2 안전 세이브가 있으면 기존 런을 복원하고, 없으면 기존 방식대로 새 런과 시작 덱·테스트 적을 생성한다.

복원 대상으로 인정되지 않는 전투 중간 상태나 종료 상태는 자동 Continue 대상으로 사용하지 않는다.

## 런 종료 처리

런이 `Completed` 또는 `Failed` 상태가 되면 진행 중인 `run_save.json`을 제거한다.

영구 성장 데이터인 `meta_progress.json`과 런 진행 세이브를 분리해 새 런에서 이전 진행 파일이 다시 로드되는 것을 막는다.

## 메타 보상 중복 지급 방지

각 `RunState`에 고유 `RunId`를 부여하고 세이브에 함께 저장한다.

`MetaProgressSaveData`에는 이미 보상을 받은 RunId 목록을 저장한다.

런 결과 처리 시 `TryClaimRunReward`를 먼저 확인해 동일한 런이 다시 종료되더라도 Meta Token을 중복 지급하지 않는다.

이미 지급된 RunId라면 추가 보상은 0으로 처리하면서 결과 UI는 정상적으로 표시한다.

## EditMode 테스트

`Day51RunPersistenceTests`를 추가해 다음 항목을 검증하도록 구성했다.

- RouteMap Seed·현재 노드·선택 경로 복원
- 방문 노드 이력 복원
- Run Flow 복원
- Run Gold 복원
- 선택 King 복원
- 런타임 강화 카드 스탯 저장
- 강화 카드 재생성
- 안전 지점 저장 조건
- 잘못된 RunFlowPhase fallback
- RunId 저장·복원
- 메타 보상 Claim 중복 방지 및 영속화

## 주요 변경 파일

- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`
- `Assets/ProjectEta/Scripts/King/KingRunStateService.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressRunResultController.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressSaveData.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressState.cs`
- `Assets/ProjectEta/Scripts/Run/RouteMapState.cs`
- `Assets/ProjectEta/Scripts/Run/RunEconomyState.cs`
- `Assets/ProjectEta/Scripts/Run/RunFlowState.cs`
- `Assets/ProjectEta/Scripts/Run/RunPersistenceController.cs`
- `Assets/ProjectEta/Scripts/Run/RunSaveData.cs`
- `Assets/ProjectEta/Scripts/Run/RunSaveSystem.cs`
- `Assets/ProjectEta/Scripts/Run/RunState.cs`
- `Assets/ProjectEta/Scripts/Run/RuntimeCardUpgradeService.cs`
- `Assets/ProjectEta/Tests/EditMode/Day51RunPersistenceTests.cs`

## 결과

51일차 작업으로 런의 전투 밖 핵심 진행 상태를 하나의 세이브 흐름으로 연결했다.

경로 지도와 King 위치, 런 Gold, 선택 King, 카드 강화 상태, 상위 Run Flow를 보존하고 안전 지점에서 자동 저장·복원하도록 구성했다.

또한 RunId 기반 메타 보상 Claim 기록을 추가해 저장·복원 이후 동일 런의 영구 보상이 중복 지급되는 문제를 방지했다.

현재 Map Seed는 저장 대상으로 포함돼 있으며, Seed를 이용해 1~10 전체 경로를 결정론적으로 다시 생성하는 단계는 이후 전체 경로 생성 구조와 연결할 수 있도록 남겨뒀다.
