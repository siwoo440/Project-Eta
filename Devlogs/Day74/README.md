# 74일차 : Mid Boss·Final Boss 전투 데이터 분리 및 보스 콘텐츠 정식화

## 개발 목표

73일차에서 일반 Enemy Encounter와 Elite 전투 편성을 정리한 뒤, 74일차에서는 기존에 하나의 프로토타입 Boss Round를 공유하던 **Mid Boss와 Final Boss를 서로 다른 전투 콘텐츠로 분리**했다.

기존 2×2 Boss 전투 시스템, Boss AI, Phase 2, Telegraph, Boss HP UI 등 이미 구축된 보스 런타임 구조는 그대로 재사용하고, 이번 작업에서는 실제 Stage에서 사용하는 Boss PieceDefinition과 RoundDefinition을 분리하는 데 집중했다.

---

## 기존 보스 구조

기존 `StageDefinitionCatalog`에서는 `MidBoss`와 `FinalBoss`가 모두 동일한 `PrototypeBossRound40`을 사용했다.

따라서 중간 보스와 최종 보스가 StageType으로는 구분되더라도 실제 전투 데이터는 같은 Boss Piece와 같은 지원 적 편성을 사용하는 프로토타입 상태였다.

74일차에서는 이 공통 경로를 분리하여 다음 구조로 변경했다.

```text
MidBoss
└─ MidBossRound74
   └─ MidBoss74

FinalBoss
└─ FinalBossRound74
   └─ FinalBoss74
```

---

## Mid Boss 데이터

새로운 `MidBoss74` PieceDefinition을 추가했다.

현재 기본 데이터는 다음과 같다.

- PieceId : `mid_boss_74`
- 표시 이름 : `중간 보스 · 철갑 성채`
- Category : Boss
- Grade : 5
- HP : 16
- ATK : 2
- Occupancy : 2×2
- 상태 이상 면역 값 유지
- 기존 Boss Phase 2·Telegraph 시스템 사용

중간 보스는 최종 보스보다 낮은 기본 압박을 갖는 기준 Boss로 구성했다.

---

## Final Boss 데이터

새로운 `FinalBoss74` PieceDefinition을 추가했다.

현재 기본 데이터는 다음과 같다.

- PieceId : `final_boss_74`
- 표시 이름 : `최종 보스 · 검은 왕좌`
- Category : Boss
- Grade : 5
- HP : 24
- ATK : 3
- Occupancy : 2×2
- 상태 이상 면역 값 유지
- 기존 Boss Phase 2·Telegraph 시스템 사용

최종 보스는 Mid Boss보다 높은 HP와 ATK를 사용해 기본 위협도를 높였다.

---

## Mid Boss Round 분리

`MidBossRound74`를 추가했다.

현재 구성은 다음과 같다.

### Boss

- `MidBoss74`
- Anchor : `(0, 8)`
- Turn Limit : 30

### 시작 지원 적

- Pawn
- Knight
- Bishop

### 증원

- Turn 4 : Rook
- Turn 6 : Cannon

기존 Boss 본체만 상대하는 구조가 아니라 지원 적과 턴 증원을 함께 사용하는 보스 전투로 구성했다.

---

## Final Boss Round 분리

`FinalBossRound74`를 추가했다.

현재 구성은 다음과 같다.

### Boss

- `FinalBoss74`
- Anchor : `(0, 8)`
- Turn Limit : 30

### 시작 지원 적

- Rook
- Knight
- Bishop
- Queen

### 증원

- Turn 3 : Queen
- Turn 5 : Cannon
- Turn 7 : Rook

Mid Boss보다 시작 지원 적 수와 증원 수를 늘리고 강한 기물을 포함하여 최종 전투의 압박을 높였다.

---

## StageDefinitionCatalog 연결

`StageDefinitionCatalog`에서 Boss Round 로드 규칙을 분리했다.

기존에는 다음과 같이 공통 Boss Round를 사용했다.

```text
MidBoss ─┐
         ├─ PrototypeBossRound40
FinalBoss┘
```

현재는 다음과 같이 분리된다.

```text
StageType.MidBoss
→ Resources/MidBossRound74

StageType.FinalBoss
→ Resources/FinalBossRound74
```

일반 Battle과 Elite는 기존 `PrototypeRound36`을 그대로 사용한다.

---

## 보상 Profile ID 분리

StageDefinition에 기록되는 Boss 보상 Profile ID도 분리했다.

- Mid Boss : `MidBossReward74`
- Final Boss : `FinalBossReward74`

이 변경으로 두 StageType이 동일한 Boss 보상 ID를 공유하지 않게 됐다.

현재 커밋에서 확인되는 변경 범위는 **StageDefinition의 Profile ID 분리까지**이며, 실제 카드 보상 생성 로직에서 이 ID를 별도 보상 테이블로 소비하는 추가 연결은 이번 커밋에 포함되지 않는다.

Final Boss 승리의 Run 완료 흐름은 기존 `RunStageFlowService`의 최종 Phase/Stage 처리 구조를 계속 사용한다.

---

## 기존 Boss 시스템 재사용

이번 작업에서는 기존 Boss 전투 코드 자체를 다시 만들지 않았다.

기존 시스템에서 이미 제공하는 다음 기능을 그대로 사용한다.

- 2×2 대형 기물 점유
- Boss 기본 행동 Planner
- Boss Action Executor
- Phase 2 전환
- 위험 영역 Telegraph
- Boss HP UI
- Boss Phase 상태 UI
- Phase 2 증원
- Large Piece 시각·점유 처리

`StageBattleRuntimeController`는 Boss Stage의 `RoundDefinition.BossResourceName`을 읽어 PieceDefinition을 Resources에서 로드하므로, 새 Mid/Final Boss Round 데이터가 기존 Boss 런타임 시스템에 연결된다.

---

## 기존 프로토타입 데이터 보존

다음 기존 데이터는 삭제하지 않았다.

- `PrototypeBoss37`
- `PrototypeBossRound40`

기존 테스트나 다른 프로토타입 코드가 해당 Resources를 참조할 가능성이 있으므로 74일차에서는 신규 데이터 추가와 Stage 연결 변경만 수행했다.

---

## 테스트 추가

`Day74BossContentTests`를 추가했다.

현재 테스트 항목은 다음과 같다.

- Mid Boss와 Final Boss가 서로 다른 RoundDefinition을 사용하는지 확인
- Mid Boss와 Final Boss의 RewardProfileId가 분리되었는지 확인
- 두 Boss Round가 서로 다른 Boss Resource를 사용하는지 확인
- Final Boss HP가 Mid Boss보다 높은지 확인
- Final Boss ATK가 Mid Boss 이상인지 확인
- 두 Boss 모두 2×2 점유인지 확인
- Final Boss 시작 지원 적 수가 Mid Boss 이상인지 확인
- Final Boss 증원 수가 Mid Boss 이상인지 확인

---

## 주요 변경 파일

### Boss Piece Data

- `Assets/ProjectEta/Resources/MidBoss74.asset`
- `Assets/ProjectEta/Resources/FinalBoss74.asset`

### Boss Round Data

- `Assets/ProjectEta/Resources/MidBossRound74.asset`
- `Assets/ProjectEta/Resources/FinalBossRound74.asset`

### Stage 연결

- `Assets/ProjectEta/Scripts/Run/StageDefinitionCatalog.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day74BossContentTests.cs`

각 신규 Unity Asset과 Test Script의 `.meta` 파일도 함께 추가했다.

---

## 현재 확인 상태

최신 `main`의 74일차 커밋에는 다음 내용이 반영되어 있다.

- Mid Boss PieceDefinition 신규 추가
- Final Boss PieceDefinition 신규 추가
- Mid Boss RoundDefinition 신규 추가
- Final Boss RoundDefinition 신규 추가
- `StageDefinitionCatalog`의 Boss Round 분리
- Boss Reward Profile ID 분리
- Day74 EditMode 테스트 추가

73일차 커밋 대비 총 11개 파일이 변경되었으며, 신규 파일 10개와 기존 `StageDefinitionCatalog.cs` 수정 1개로 구성된다.

GitHub 저장소에는 현재 이 커밋에 연결된 CI Status 또는 GitHub Actions Workflow Run이 없다.

따라서 원격 저장소에서 확인할 수 있는 것은 코드·에셋 반영 상태와 정적 구조까지이며, Unity Editor의 실제 컴파일 성공과 EditMode Test 실행 결과는 로컬 Unity 실행을 통해 최종 확인해야 한다.

---

## 다음 개발 방향

다음 단계에서는 Boss 개별 데이터 분리가 완료된 상태를 기반으로 전체 Stage 편성과 카드·기물 Pool을 정리하는 것이 우선이다.

주요 대상은 다음과 같다.

- 1~10 Stage의 Battle·Elite·Reward·Shop·Event 배치 정리
- Phase별 난이도 흐름 확인
- Enemy Pool 최종 정리
- 카드·기물 획득 Pool 정리
- Fusion 결과 데이터와 중복·미사용 데이터 확인
- Mid Boss와 Final Boss 보상 차등화의 실제 Reward 시스템 연결 검토
