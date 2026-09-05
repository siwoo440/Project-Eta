# 43일차 : 런 모드·스테이지 경로 구조 및 전투 결과 디버그 UI 구축

## 개발 목표

42일차에 분리한 `RunState`·`RoundState`·`BattleState` 위에 전투와 경로 지도를 오가는 상위 런 흐름을 추가하고, 전투 승리 후 즉시 다음 라운드로 넘어가지 않고 동일한 10×10 체스판을 다음 스테이지 선택용 경로 지도 상태로 전환할 수 있는 기반을 구축한다.

개발 중 런 흐름을 빠르게 검증할 수 있도록 기존 합성 버튼 위에 강제 승리·패배 버튼도 추가한다.

## 주요 개발 내용

### Battle / Map 보드 모드 분리

동일한 10×10 체스판이 상황에 따라 서로 다른 역할을 가지도록 `BoardMode`를 추가했다.

- `Battle`
  - 기존 기물 이동·공격·배치가 이루어지는 전투판 상태
- `Map`
  - 전투 종료 후 플레이어 킹으로 다음 스테이지를 선택하는 경로 지도 상태

43일차에서는 실제 판의 그래픽 변환과 킹 이동까지 구현하지 않고, 다음 일차에서 시각화와 입력을 연결할 수 있도록 상태 계층을 먼저 구축했다.

### RunFlowState 추가

로그라이트 런의 상위 진행 단계를 `RunFlowState`로 분리했다.

- `Battle`
  - 현재 스테이지 전투 진행
- `Map`
  - 다음 스테이지 경로 선택
- `Completed`
  - 최종 스테이지 승리로 런 완료
- `Failed`
  - 전투 패배로 런 종료

`RunFlowState`는 현재 `BoardMode`도 함께 관리해 런 흐름과 체스판 역할이 서로 어긋나지 않도록 구성했다.

### RouteMapState 경로 지도 상태

`RouteMapState`를 추가해 실제 화면 표시와 분리된 경로 지도 데이터를 관리한다.

현재 관리하는 주요 상태는 다음과 같다.

- 현재 완료 깊이
- 현재 노드 ID
- 플레이어 킹 지도 좌표
- 현재 경로의 전체 노드
- 현재 위치에서 선택 가능한 다음 노드

전투 승리 후 다음 깊이의 프로토타입 후보를 생성해 44일차의 체스판 미니맵 표시와 킹 이동 기능이 바로 연결될 수 있도록 했다.

### StageNode 경로 노드 구조

체스판 위 하나의 스테이지 목적지를 나타내는 `StageNode`를 추가했다.

각 노드는 다음 정보를 가진다.

- 노드 ID
- 10×10 체스판 좌표
- 스테이지 깊이
- 연결할 `StageDefinition` ID
- 다음 이동 가능한 노드 ID 목록
- 방문 여부

다음 노드 연결은 ID 기반으로 관리하고 중복 연결을 제거해 이후 분기형 경로 생성에 사용할 수 있도록 구성했다.

### StageDefinition 기반 스테이지 데이터

경로 노드의 위치 정보와 실제 스테이지 내용을 분리하기 위해 `StageDefinition`을 추가했다.

현재 정의한 `StageType`은 다음과 같다.

- `Battle`
- `Elite`
- `Reward`
- `Shop`
- `Event`
- `MidBoss`
- `FinalBoss`

전투형 스테이지는 기존 `RoundDefinition`을 연결할 수 있도록 해 이후 노드 선택 결과에 따라 적 구성·보스·턴 제한 등 기존 라운드 데이터를 재사용할 수 있는 형태로 준비했다.

### 전투 결과와 런 흐름 연결

기존 `RoundStateBattleBridge`를 43일차 런 구조에 맞게 확장했다.

전투 종료 결과는 `RunState.HandleBattleOutcome()`으로 전달된다.

승리 시:

- 현재 `RoundState`를 `Cleared`로 기록
- 1~9단계에서는 다음 깊이 경로 후보 준비
- `RunFlowState`를 `Map`으로 변경
- `BoardMode`를 `Map`으로 변경
- 10단계 승리 시 경로 지도로 넘어가지 않고 `Completed` 처리

패배 시:

- 현재 `RoundState`를 `Failed`로 기록
- `RunFlowState`를 `Failed`로 변경
- 다음 경로 후보를 생성하지 않음

### 프로토타입 다음 스테이지 후보

43일차 상태 검증 단계에서는 전투 승리 후 다음 깊이에 세 개의 후보 노드를 준비한다.

예를 들어 3단계를 완료한 경우:

- 현재 킹 위치: `(4, 2)`
- 다음 4단계 왼쪽: `(3, 3)`
- 다음 4단계 중앙: `(4, 3)`
- 다음 4단계 오른쪽: `(5, 3)`

실제 스테이지 종류와 랜덤 경로 생성 규칙은 이후 개발에서 확장한다.

### 개발용 승리·패배 버튼

로그라이트 흐름을 매번 실제 전투로 끝까지 진행하지 않고 빠르게 확인할 수 있도록 `DebugBattleResultButtons`를 추가했다.

기존 우하단 합성 버튼 바로 위에 다음 버튼을 표시한다.

- `승리`
  - `BattleController.EndBattle(BattleOutcome.Victory)` 호출
  - 실제 전투 종료 파이프라인을 거쳐 43일차 Map 흐름 진입 확인
- `패배`
  - `BattleController.EndBattle(BattleOutcome.Defeat)` 호출
  - 실제 전투 종료 파이프라인을 거쳐 런 실패 흐름 확인

버튼은 Battle 씬에서 런타임 자동 생성되며 별도 씬·Inspector 설정이 필요하지 않도록 구성했다.

이미 전투가 종료됐거나 `Map`, `Completed`, `Failed` 상태인 경우 결과 버튼을 비활성화해 중복 결과 입력을 막는다.

### 43일차 회귀 테스트

`Day43RunFlowTests`를 추가해 다음 항목을 검증하도록 구성했다.

- 새 런이 `Battle` 흐름과 `BoardMode.Battle`로 시작
- 일반 스테이지 승리 후 `Map` 흐름 진입
- 승리 후 다음 깊이 후보 3개 준비
- 패배 시 지도 진입 없이 `Failed`
- 10단계 승리 시 지도 진입 없이 `Completed`
- 현재 노드 연결 관계에 따른 선택 가능 노드 판정

`Day43DebugBattleResultButtonsTests`에서는 다음 규칙을 검증하도록 구성했다.

- 전투 진행 중 결과 버튼 사용 가능
- 이미 종료된 전투에서는 결과 버튼 사용 불가
- 지도 흐름에서는 결과 버튼 사용 불가
- 런 완료 후 결과 버튼 사용 불가

GitHub에는 별도의 CI 상태 검사가 등록되어 있지 않으므로 Unity Editor의 실제 컴파일과 EditMode 테스트 결과는 로컬 환경에서 최종 확인한다.

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/BoardMode.cs`
- `Assets/ProjectEta/Scripts/Run/RunFlowState.cs`
- `Assets/ProjectEta/Scripts/Run/RouteMapState.cs`
- `Assets/ProjectEta/Scripts/Run/StageNode.cs`
- `Assets/ProjectEta/Scripts/Run/StageDefinition.cs`
- `Assets/ProjectEta/Scripts/Run/RunState.cs`
- `Assets/ProjectEta/Scripts/Round/RoundStateBattleBridge.cs`
- `Assets/ProjectEta/Scripts/UI/DebugBattleResultButtons.cs`
- `Assets/ProjectEta/Tests/EditMode/Day43RunFlowTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day43DebugBattleResultButtonsTests.cs`

## 결과

기존의 선형적인 라운드 진행 구조에서 벗어나 전투와 전투 사이에 동일 체스판을 사용하는 경로 선택 단계를 끼울 수 있는 데이터 기반이 마련되었다.

전투 승리·패배 결과가 런의 `Battle / Map / Completed / Failed` 상태로 직접 연결되고, 승리 시 다음 깊이의 선택 가능한 노드가 준비된다.

또한 개발용 승리·패배 버튼을 통해 실제 전투를 매번 끝까지 진행하지 않고도 전투 종료 → 지도 진입 또는 런 실패 흐름을 빠르게 검증할 수 있다.

다음 44일차에서는 현재 `RouteMapState` 데이터를 실제 10×10 `BoardView`에 표시하고, 전투 기물을 정리한 뒤 플레이어 킹 1개와 연결된 스테이지 노드를 시각화해 킹을 인접 8방향 노드로 직접 이동시키는 기능을 구현한다.
