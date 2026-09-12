# 75일차 : Stage 전체 편성·Elite Route 노출 및 전투 보상 경로 통합

## 개발 목표

74일차에서 Mid Boss와 Final Boss 데이터를 분리한 뒤, 75일차에서는 지금까지 구현한 전투 콘텐츠가 실제 한 런의 RouteMap과 보상 흐름에서 연결되도록 정리했다.

이번 작업의 핵심은 다음 두 가지다.

- 전체 RouteMap에서 Elite 전투를 실제 선택 가능한 스테이지로 노출
- 일반 Battle / Elite / Mid Boss의 승리 보상을 서로 다른 보상 경로와 품질 규칙으로 분리

기존 Reward / Shop / Event / Boss / Enemy Encounter 구조는 유지하고, Stage 편성과 카드 보상 연결을 확장하는 방식으로 진행했다.

---

## RouteMap Elite 노드 연결

기존 전체 Route 생성의 선택형 Stage 후보에는 다음 세 종류만 포함되어 있었다.

- Reward
- Shop
- Event

Elite는 기존 단일 다음 층 생성 호환 경로에는 존재했지만, `CreateFullRoute()`가 사용하는 전체 Route 후보에는 포함되어 있지 않았다.

75일차에서는 전체 Route 후보를 다음과 같이 확장했다.

```text
Battle
Elite
Reward
Shop
Event
```

각 일반 깊이에서는 기존처럼 최소 하나의 일반 Battle을 유지하고, 나머지 분기에서 Elite / Reward / Shop / Event를 Seed 기반으로 선택한다.

---

## 후반 Elite 최소 노출

Elite 시스템이 구현되어 있어도 Seed에 따라 실제 플레이에서 한 번도 만나지 못하는 상황을 줄이기 위해 8단계에는 최소 하나의 Elite 노드가 존재하도록 규칙을 추가했다.

따라서 75일차 이후 전체 Route는 다음 조건을 갖는다.

- 1단계 : 시작 Battle 고정
- 5단계 : Mid Boss 고정
- 8단계 : 최소 1개 Elite 선택지 보장
- 10단계 : Final Boss 고정
- 그 외 일반 깊이 : Battle + 선택형 콘텐츠 분기

Route 생성은 기존 `MapSeed` 기반 결정론적 난수 구조를 그대로 사용하므로 같은 Seed에서는 같은 노드 ID, StageDefinition, 좌표 구성을 재현한다.

---

## 카드 보상 Source 확장

기존 카드 보상은 다음 두 Source만 구분했다.

```text
BattleVictory
RewardNode
```

75일차에서는 전투 난이도와 보상 차이를 실제 카드 보상 시스템에 반영하기 위해 다음처럼 확장했다.

```text
BattleVictory
RewardNode
EliteVictory
MidBossVictory
```

이를 통해 일반 전투, Elite, Mid Boss가 더 이상 하나의 `BattleVictory` 규칙을 공유하지 않는다.

Final Boss는 별도 카드 보상 Source를 만들지 않고 기존 Run Complete 흐름을 유지한다.

---

## Elite 보상 품질

`PrototypeEliteReward` 프로필과 `EliteVictory` Source가 실제 `CardRewardQualityRules`에서 소비되도록 연결했다.

현재 Elite 카드 등급 가중치는 다음과 같다.

### Stage 1~3

```text
1성 55
2성 38
3성 7
```

### Stage 4~6

```text
1성 30
2성 50
3성 20
```

### Stage 7~10

```text
1성 15
2성 50
3성 35
```

일반 Battle보다 2성·3성 비중을 높여 Elite 선택의 위험 대비 보상을 강화했다.

---

## Mid Boss 보상 연결

74일차에서 `StageDefinitionCatalog`에 추가했던 `MidBossReward74`가 실제 보상 품질 계산에 사용되도록 연결했다.

현재 Mid Boss 보상은 `Advanced` 품질을 사용한다.

### Stage 1~6 기준

```text
1성 20
2성 50
3성 30
```

### Stage 7~10 기준

```text
1성 10
2성 45
3성 45
```

따라서 Mid Boss 보상은 같은 진행 구간의 일반 전투보다 높은 고등급 카드 확률을 갖도록 구성했다.

---

## CardRewardController 전투 종류 판별

전투 종료 시 `StageBattleRuntimeController`의 현재 `StageDefinition`을 조회하여 방금 완료한 전투 종류를 판별하도록 수정했다.

판별 결과는 다음 보상 Source로 변환한다.

```text
Battle   → BattleVictory
Elite    → EliteVictory
MidBoss  → MidBossVictory
FinalBoss → 카드 보상 없음
```

StageDefinition을 직접 확인할 수 없는 경우에는 현재 RouteMap 노드를 통해 다시 StageDefinition을 조회한다.

---

## Phase 전환 전 보상 정보 보존

Mid Boss 승리 시 5페이즈 진행 구조에 따라 다음 Phase의 RouteMap이 즉시 준비될 수 있다.

이 경우 보상 화면을 열기 전에 현재 Stage 번호나 StageDefinition 정보가 바뀌면 잘못된 품질로 보상을 생성할 가능성이 있다.

이를 방지하기 위해 전투 종료 시점에 다음 값을 별도로 저장하도록 했다.

- 완료 전투의 `CardRewardSource`
- 완료 전투의 Stage 번호
- 완료 전투의 `RewardProfileId`

지도 전환 후 실제 카드 보상 UI를 열 때는 저장해 둔 값을 사용한다.

---

## 보상 Seed 분리

카드 보상의 결정적 Seed에도 Source별 Salt를 적용했다.

현재 다음 보상 경로가 서로 다른 Seed를 사용한다.

- 일반 Battle
- Reward Node
- Elite
- Mid Boss

따라서 같은 Stage와 같은 보유 카드 수라도 보상 발생 경로가 다르면 독립적인 카드 후보를 생성할 수 있다.

---

## Final Boss 처리

Final Boss 승리 시에는 일반 카드 3택 보상을 예약하지 않는다.

기존 `RunStageFlowService`의 마지막 Phase / Final Boss 완료 처리와 연결하여 Final Boss 승리는 Run Complete 흐름을 우선한다.

즉 현재 보상 구조는 다음과 같다.

```text
Battle 승리
→ 일반 카드 보상
→ RouteMap

Elite 승리
→ 강화 카드 보상
→ RouteMap

Mid Boss 승리
→ 전용 강화 카드 보상
→ 다음 Phase 진행

Final Boss 승리
→ 일반 카드 보상 생략
→ Run Complete
```

---

## Day52 기존 테스트 충돌 수정

75일차에서 전체 Route에 Elite를 정식 추가한 뒤, 기존 Day52 테스트의 요구사항과 충돌하는 문제가 확인됐다.

기존 테스트는 일반 깊이에서 다음 Stage만 허용했다.

```text
Battle
Reward
Shop
Event
```

그리고 `Elite`가 등장하면 실패하도록 작성되어 있었다.

이는 52일차 당시의 요구사항에는 맞지만, 75일차에서 Elite를 전체 Route에 정식 편성한 이후에는 더 이상 유효하지 않은 조건이다.

따라서 `Day52FullRouteTests`의 해당 테스트를 다음 의미로 갱신했다.

```text
일반 깊이 허용 Stage
Battle
Elite
Reward
Shop
Event
```

Route 연결, 분기 수, Boss 고정, Seed 재현성 등 Day52의 다른 검증 조건은 그대로 유지했다.

---

## Day75 테스트

`Day75StageContentIntegrationTests`를 추가하여 다음 항목을 검증하도록 했다.

- 전체 Route 8단계에 Elite 노드가 존재하는지 확인
- 동일 Seed에서 동일 노드 배치가 생성되는지 확인
- Elite 보상이 일반 Battle보다 높은 고등급 카드 가중치를 갖는지 확인
- Mid Boss가 전용 Advanced 보상 프로필을 사용하는지 확인
- StageDefinitionCatalog의 Elite / Mid Boss RewardProfileId 연결 확인

추가로 Day52 전체 Route 테스트의 허용 Stage 목록을 75일차 구조에 맞게 갱신했다.

---

## 주요 변경 파일

### RouteMap

- `Assets/ProjectEta/Scripts/Run/StageRouteGenerator.cs`

### Card Reward

- `Assets/ProjectEta/Scripts/Run/CardRewardState.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardQualityRules.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardController.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day75StageContentIntegrationTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day75StageContentIntegrationTests.cs.meta`
- `Assets/ProjectEta/Tests/EditMode/Day52FullRouteTests.cs`

---

## 현재 확인 상태

최신 `main`에는 75일차 Stage 편성·보상 통합 코드와 Day52 Elite 허용 테스트 수정이 함께 반영되어 있다.

확인된 항목은 다음과 같다.

- 전체 Route 후보에 Elite 포함
- 8단계 Elite 최소 노출 규칙
- `EliteVictory` / `MidBossVictory` 보상 Source 추가
- `PrototypeEliteReward` 실제 품질 규칙 연결
- `MidBossReward74` 실제 품질 규칙 연결
- Final Boss 카드 보상 제외
- Day75 통합 테스트 추가
- 기존 Day52 테스트와 Elite 요구사항 충돌 수정

현재 GitHub 커밋에는 연결된 CI Status가 없으므로 원격 저장소만으로 Unity 실제 컴파일 성공 및 전체 EditMode Test 통과 여부까지 확인할 수는 없다.

따라서 이 개발일지는 최신 코드 반영 상태와 정적 구조를 기준으로 작성했으며, 실제 Unity Test Runner 전체 결과는 로컬 실행 결과를 기준으로 최종 확인한다.

---

## 다음 개발 방향

75일차까지 Battle / Elite / Mid Boss / Final Boss와 각 보상 경로가 실제 RouteMap 흐름에 연결됐다.

다음 단계에서는 신규 전투 콘텐츠 추가보다 전체 게임의 배포 기반을 준비하는 방향으로 이동한다.

우선순위는 다음과 같다.

- 전체 콘텐츠 Pool 최종 점검
- Save / Load와 5페이즈 장기 진행 검증
- Steam Overlay 연결
- Steam Cloud 연결
- Achievement 연결
- 전체 UI와 Steam 기능 통합 전 QA
