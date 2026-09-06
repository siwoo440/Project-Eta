# 53일차 : 전투·경로·보상·상점·이벤트 전체 로그라이트 루프 통합

## 개발 목표

41~52일차에 개별적으로 구축한 로그라이트 시스템을 하나의 실제 런 진행 흐름으로 연결한다.

53일차의 핵심은 새로운 대형 기능을 추가하는 것이 아니라 기존 전투, Route Map, 카드 보상, Shop, Event, Boss, Run Flow를 동일한 스테이지 진행 규칙으로 통합하는 것이다.

목표 흐름은 다음과 같다.

`Battle → Map → StageNode 선택 → Battle / Reward / Shop / Event → Map → ... → Stage 5 MidBoss → ... → Stage 10 FinalBoss → Completed`

전투 패배 시에는 같은 흐름에서 `Failed`로 종료한다.

## RunStageFlowService

스테이지 진행 상태를 한 곳에서 관리하기 위해 `RunStageFlowService`를 추가했다.

이 서비스는 다음 역할을 담당한다.

- 선택된 StageNode와 실제 지도 King 위치 검증
- `StageNode.Depth`와 `RunState.CurrentRound` 동기화
- Reward / Shop / Event 진입
- 비전투 스테이지 완료
- Battle 결과를 기존 `RunState.HandleBattleOutcome`에 연결
- 전투 승리 카드 보상 완료 후 Map 복귀

기존 각 Controller에 흩어져 있던 Round 완료, RouteMap 갱신, Flow 전환 코드를 공통 규칙으로 모았다.

## StageNode.Depth 기준 통일

스테이지 진입 시 `StageNode.Depth`를 현재 스테이지 번호의 기준으로 사용한다.

진입 전에 다음 상태가 모두 일치하는지 확인한다.

- `RouteMap.HasSelectedNode`
- `RouteMap.SelectedNodeId`
- `RouteMap.CurrentNodeId`
- `RouteMap.CurrentDepth`
- 선택한 `StageNode.Depth`

실제로 지도에서 선택한 노드와 현재 King 위치가 일치하지 않으면 스테이지 진입을 중단한다.

검증에 성공한 경우에만 다음을 적용한다.

`RunState.CurrentRound = StageNode.Depth`

이를 통해 Route Map의 깊이와 실제 Battle/Reward/Shop/Event의 현재 스테이지 번호가 서로 어긋나는 상황을 줄인다.

## Battle 결과와 Run Flow 연결

`BattleController.EndBattle()`이 `TurnManager`의 전투 종료 상태만 변경하던 흐름을 RunState까지 연결했다.

처리 순서는 다음과 같다.

1. 진행 중인 더미 적 턴 코루틴 정리
2. `TurnManager.EndBattle(outcome)`
3. `BattleEnded` 이벤트 발행
4. 기존 전투 결과 구독자가 결과 감지
5. `RunStageFlowService.CompleteBattle`
6. `RunState.HandleBattleOutcome`

기존 `RunState.HandleBattleOutcome`의 규칙은 그대로 재사용한다.

- 일반 Battle Victory → 현재 Stage 완료 → Route Map
- Stage 10 Victory → `Completed`
- Defeat → `Failed`

전투 결과 처리 로직을 중복 구현하지 않고 기존 RunState를 최종 상태 소유자로 유지한다.

## 전투 승리 카드 보상

기존 CardRewardController는 Battle Victory를 감지하면 즉시 보상을 시작하지 않고 `_combatVictoryPending`을 설정한다.

RunState의 전투 승리 처리가 완료되어 실제 Flow가 `Map`이 된 뒤 카드 보상을 시작한다.

흐름은 다음과 같다.

`Battle Victory → RunState 전투 완료 → Map 준비 → BattleVictory Reward → 카드 선택 → Map`

전투 승리 보상은 이미 완료된 Battle Stage를 다시 완료 처리하지 않는다.

`CompleteBattleReward()`는 다음 조건을 확인한다.

- 현재 Flow가 Reward
- 현재 Round가 이미 Cleared
- Route Map이 준비됨
- 아직 다음 StageNode를 선택하지 않음

조건을 만족하면 Round와 RouteMap을 다시 변경하지 않고 Map으로만 복귀한다.

## Reward Node 완료

독립 Reward StageNode는 전투 승리 보상과 별도로 처리한다.

흐름은 다음과 같다.

`Map → Reward Node 선택 → Reward Flow → 카드 선택 → 현재 Reward Stage 완료 → 다음 Depth 개방 → Map`

카드를 선택하지 못하는 상황이나 보상 후보가 없는 경우에도 동일한 통합 완료 규칙을 사용해 진행이 막히지 않도록 구성했다.

## Shop / Event 통합

`StageActivityController`의 Shop과 Event 완료 로직도 `RunStageFlowService.CompleteNonBattleStage()`를 사용하도록 변경했다.

기존에 Controller 내부에서 직접 수행하던 다음 처리를 공통 서비스로 이동했다.

- Round를 Cleared + Victory로 기록
- 현재 RouteMap Stage 완료
- 전체 1~10 Route Graph 유지
- 다음 연결 노드 선택 가능 상태 개방
- Map Flow 복귀

Shop/Event의 Gold, 카드 제거, 회복, 강화, 이벤트 결과 자체는 기존 47일차 시스템을 그대로 사용한다.

## StageTransitionController 통합

`StageTransitionController`는 지도 King 이동 완료 후 실제 스테이지로 진입하는 중심 전환기로 유지한다.

선택 노드의 `StageDefinition`을 Resolve한 다음 `RunStageFlowService.TrySynchronizeSelectedStage()`를 실행한다.

검증된 노드만 다음 실제 흐름으로 진입한다.

- Battle / MidBoss / FinalBoss → Battle
- Reward → CardRewardController
- Shop → StageActivityController
- Event → StageActivityController

실제 Day47 `StageActivityController`가 존재하면 Shop/Event 개발용 Placeholder를 사용하지 않는다.

실제 관리자가 없는 예외 상황에서는 진행 불가를 막기 위해 기존 Placeholder를 fallback으로 유지한다.

## 전체 Route Graph 유지

52일차에서 구축한 1~10 전체 Route Graph는 53일차에서도 유지한다.

비전투 스테이지나 전투를 완료할 때 새로운 지도를 만드는 방식이 아니라 기존 `RouteMapState.PreparePrototypeAfterBattle()`을 통해 현재 전체 그래프 안에서 다음 깊이만 개방한다.

따라서 다음 정보가 런 전체에서 계속 유지된다.

- Map Seed
- 전체 1~10 노드
- 노드 연결 관계
- 현재 King 노드
- 현재 Depth
- 방문 노드
- 선택 경로

## Stage 5 / Stage 10 흐름

52일차에서 생성한 보스 경로를 실제 Run Flow와 연결한다.

Stage 4를 완료하면 다음 선택지는 Stage 5 MidBoss 단일 노드가 된다.

`Stage 4 → Map → Stage 5 MidBoss → Victory → Map → Stage 6`

Stage 10 FinalBoss에서 승리하면 다음 Map을 만들지 않는다.

`Stage 10 FinalBoss → Victory → RunFlowPhase.Completed`

전투 패배는 스테이지 번호와 관계없이 다음으로 종료된다.

`Battle Defeat → RunFlowPhase.Failed`

## Day53RogueliteLoopTests

전체 진행 상태를 검증하기 위해 `Day53RogueliteLoopTests`를 추가했다.

주요 검증 항목은 다음과 같다.

- 선택 StageNode의 Depth를 CurrentRound 기준으로 사용
- 지도에서 선택하지 않은 노드의 강제 진입 차단
- Reward / Shop / Event의 StageType별 Flow 연결
- 비전투 완료 후 전체 Route Graph 유지
- 비전투 완료 후 정확히 다음 Depth만 개방
- 전투 승리 카드 보상이 완료된 Stage를 다시 처리하지 않음
- Stage 4 승리 후 Stage 5 MidBoss 강제 연결
- Battle Defeat 후 Failed
- Stage 10 Victory 후 Completed
- Stage 1부터 FinalBoss까지 상태 기반 전체 Route Loop 진행

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Run/RunStageFlowService.cs`
- `Assets/ProjectEta/Tests/EditMode/Day53RogueliteLoopTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardController.cs`
- `Assets/ProjectEta/Scripts/Run/StageActivityController.cs`
- `Assets/ProjectEta/Scripts/Run/StageTransitionController.cs`

### 삭제

없음.

## 결과

53일차 작업으로 전투, Route Map, Reward, Shop, Event의 개별 진행 코드가 하나의 공통 스테이지 흐름으로 연결됐다.

지도에서 선택한 StageNode의 Depth를 현재 스테이지 기준으로 사용하고, 각 콘텐츠 완료 후 같은 전체 Route Graph 안에서 다음 깊이로 진행한다.

일반 전투 승리는 Map과 카드 보상으로 이어지고, Reward/Shop/Event는 완료 후 Map으로 돌아가며, Stage 5 MidBoss와 Stage 10 FinalBoss도 동일한 진행 규칙 안에서 처리한다.

FinalBoss 승리는 `Completed`, 전투 패배는 `Failed`로 연결된다.

54~55일차에서는 새로운 핵심 시스템 추가보다 실제 1~10 전체 런 반복 플레이를 통해 진행 불가, 저장·복원, UI 전환, 보상 중복, Stage 번호 불일치 같은 통합 회귀 문제를 집중적으로 안정화한다.

## 검증 상태

53일차 커밋의 변경 파일 구성과 통합 호출 관계를 확인했다.

GitHub에는 해당 커밋에 연결된 CI 상태 검사가 등록되어 있지 않으므로 Unity 컴파일 및 TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
