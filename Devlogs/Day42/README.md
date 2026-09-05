# 42일차 : 런·라운드·전투 상태 분리 및 10라운드 진행 구조 구축

## 개발 목표

기존 `RunState`에 집중되어 있던 런 전체 상태와 전투 한 판의 임시 상태를 분리하고, 1~10라운드 진행 상태·보스 라운드 여부·전투 결과를 저장하고 복원할 수 있는 기반을 구축한다.

## 주요 개발 내용

### RunState·RoundState·BattleState 책임 분리

- `RunState`를 전체 로그라이트 런의 최상위 상태로 유지
- 현재 전투 한 판에서 사용하는 `BoardState`와 `HandState`를 `BattleState`로 분리
- 1~10라운드 진행 정보는 새 `RoundState`가 담당하도록 구성
- 기존 코드의 대규모 수정 없이 동작하도록 `RunState.Board`, `RunState.Hand`, `RunState.CurrentRound` 호환 접근을 유지
- `ResetBattleState()`를 통해 보드·손패만 새 전투 상태로 교체하고 덱·메타 재화·라운드 진행 정보는 유지

### 1~10라운드 진행 상태

라운드 진행 상태를 다음 네 단계로 구분한다.

1. `NotStarted`
   - 라운드 시작 전 상태

2. `InProgress`
   - 현재 라운드 전투 진행 중

3. `Cleared`
   - 전투 승리로 라운드 완료

4. `Failed`
   - 전투 패배로 라운드 실패

- 라운드 번호를 1~10 범위로 제한
- 5라운드와 10라운드를 보스 라운드로 자동 판정
- 라운드 번호 변경 시 이전 전투 결과와 진행 상태를 초기화
- 승리·패배 결과를 `BattleOutcome`과 연결해 라운드 상태로 변환

### 전투 결과 자동 연결

- `RoundStateBattleBridge`를 추가해 기존 `BattleController`와 `TurnManager`의 전투 종료 결과를 `RunState`에 연결
- Battle 씬에서 브리지를 런타임 자동 생성
- 기존 전투 시스템 초기화가 완료될 때까지 대기한 뒤 실제 `RunState`와 `TurnManager`에 연결
- 전투 종료 시 `Victory`는 `Cleared`, `Defeat`는 `Failed`로 기록
- 연결 이전에 전투가 이미 종료된 경우에도 현재 결과를 즉시 동기화
- 오브젝트 제거 시 턴 이벤트 구독을 정리해 중복 연결 방지

### 저장·복원 확장

`RunSaveData`에 다음 진행 정보를 추가했다.

- 현재 라운드 번호
- 현재 라운드 진행 상태
- 마지막 전투 결과
- 저장 시점 보스 라운드 여부

- 기존 킹 체력·메타 재화·손패·덱·죽은 카드·보드 기물·상태 효과·합성 발견 기록 저장 구조 유지
- 신규 필드가 없는 기존 세이브는 `NotStarted`·`BattleOutcome.None` 기본값으로 복원
- 보스 여부는 복원된 라운드 번호를 기준으로 다시 계산 가능
- 2×2 대형 기물의 단일 저장·전체 점유 복원 구조를 유지

### 42일차 회귀 테스트

`Day42RunStateTests`를 추가해 다음 항목을 검증하도록 구성했다.

- 새 런이 1라운드 `NotStarted` 상태로 시작
- 5·10라운드 보스 플래그 판정
- 승리 시 `Cleared` 전환
- 패배 시 `Failed` 전환
- `BattleState` 초기화 후 런 진행 데이터 유지
- 라운드 변경 시 라운드 진행 상태만 초기화
- 라운드 상태·전투 결과·보스 플래그 저장 및 복원
- 신규 필드가 없는 구버전 세이브 기본값 복원
- 1~10 범위를 벗어난 라운드 번호 보정

GitHub 최신 커밋 기준 별도의 CI 상태 검사는 등록되어 있지 않으므로 Unity Editor의 실제 컴파일과 EditMode 테스트 결과는 로컬 환경에서 최종 확인한다.

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/RunState.cs`
- `Assets/ProjectEta/Scripts/Run/RunSaveData.cs`
- `Assets/ProjectEta/Scripts/Run/RoundState.cs`
- `Assets/ProjectEta/Scripts/Run/BattleState.cs`
- `Assets/ProjectEta/Scripts/Round/RoundStateBattleBridge.cs`
- `Assets/ProjectEta/Tests/EditMode/Day42RunStateTests.cs`

## 결과

기존 전투 시스템의 호출 구조를 최대한 유지하면서 런 전체 상태와 전투 한 판의 임시 상태를 분리했다.

현재 라운드 번호, 진행 상태, 전투 결과, 5·10라운드 보스 여부가 하나의 런 데이터로 관리되고 저장·복원될 수 있는 구조가 마련되었다.

다음 개발에서는 이 상태 구조를 기반으로 라운드 종료 후 카드 정리, 보상 단계 진입, 다음 `RoundDefinition` 준비와 새 `BattleState` 생성 흐름을 연결할 수 있다.
