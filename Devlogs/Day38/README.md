# 38일차 개발 일지 — 2×2 보스 전투·2페이즈·텔레그래프 통합

**날짜**: 2026-09-05  
**비교 기준 커밋**: `9b7cb8b2b4adea1c7194e0305d729690f4aeb611` — `37일차 : 2×2 보스 점유 및 수평형 대형 모델 구현`  
**목표**: 37일차에 완성한 2×2 보스의 보드 점유 기반을 실제 전투 가능한 보스로 확장하고, 기본 이동·공격부터 HP 50% 이하 2페이즈, 위험 칸 텔레그래프, 다음 적 턴 범위 공격까지 한 일차에 통합한다.

## 37일차 대비 핵심 변화

37일차의 보스는 다음 기능까지 갖춘 상태였다.

```text
2×2 점유
4칸 = 하나의 PieceRuntimeState
전체 점유 해제
중복 카운트 방지
수평형 대형 모델
```

38일차에서는 이 기반 위에 다음 전투 루프를 추가했다.

```text
EnemyTurn
↓
보스 행동 평가
↓
Phase 1
├─ 인접 플레이어 공격
└─ King 방향 2×2 이동
↓
HP 50% 이하
↓
Phase 2
├─ 1회 증원
├─ 위험 칸 텔레그래프
├─ PlayerTurn 회피 기회
└─ 다음 EnemyTurn 실제 범위 공격
```

---

## 1. 보스 기본 행동 데이터 추가

보스 전용 행동 타입을 추가했다.

```text
Move
Attack
```

각 행동 후보는 다음 데이터를 가진다.

```text
Actor
ActionType
Origin
Target
TargetPiece
Score
```

일반 적 AI와 마찬가지로 점수 기반으로 비교할 수 있도록 구성했다.

## 2. BossActionPlanner 추가

2×2 보스 전용 행동 후보 생성기를 추가했다.

기본 규칙:

```text
2×2 외곽에 플레이어가 있음
→ 공격

공격 대상 없음
→ 상/하/좌/우 1칸 이동 후보 생성
```

보스는 Anchor 한 칸만 기준으로 하지 않고 전체 `OccupancySize`를 기준으로 공격 범위와 이동 가능 영역을 계산한다.

## 3. 2×2 전체 외곽 공격

보스 몸체 주변 한 칸을 기본 근접 공격 범위로 사용한다.

```text
□ X X □
X ■ ■ X
X ■ ■ X
□ X X □
```

`■`은 보스 점유 칸이고 `X`는 기본 공격 가능 영역이다.

따라서 보스 네 칸 중 어느 쪽에 플레이어가 접근해도 같은 보스가 공격 대상으로 평가한다.

## 4. King 우선 공격

보스 공격 후보에는 다음 우선순위를 적용했다.

```text
공격 기본 점수
+ King 직접 공격 보너스
+ 처치 가능 보너스
+ BaseAtk 피해 가치
```

King과 일반 기물이 동시에 공격 가능하면 King을 우선한다.

## 5. 2×2 상하좌우 이동

보스 기본 이동은 한 번에 상하좌우 1칸으로 제한했다.

새 Anchor가 정해지면 전체 2×2 영역을 검사한다.

```text
새 Anchor
↓
CanOccupyArea()
↓
4칸 전체 확인
↓
보드 밖 / 장애물 / 다른 기물 없음
↓
이동 가능
```

한 칸이라도 막혀 있으면 해당 이동 후보를 만들지 않는다.

## 6. King 접근 이동

공격 가능한 대상이 없으면 플레이어 King과의 거리가 줄어드는 이동에 높은 점수를 준다.

```text
이동 전 거리
↓
이동 후 거리
↓
감소한 거리만큼 점수 증가
```

이동 후 바로 다음 공격 범위에 King이 들어오는 위치에는 추가 보너스를 적용한다.

## 7. BossActionExecutor 추가

보스 행동 후보를 실제 보드와 전투 시스템에 적용하는 실행기를 추가했다.

### 이동

```text
BeforeMove
↓
기존 2×2 점유 전체 해제
↓
새 Anchor 적용
↓
새 2×2 네 칸 점유
↓
모델 위치 보정
↓
AfterMove
↓
EnemyTurn 종료
```

실행 직전 새 영역을 다시 검사하며 실패하면 원래 위치를 복구한다.

### 직접 공격

```text
BeforeAttack
↓
CombatResolver.ResolveAttack()
↓
BattleHooks
↓
피해·사망 처리
↓
AfterAttack
↓
EnemyTurn 종료
```

보스 전용 피해 공식을 새로 만들지 않고 기존 고정 ATK 전투 파이프라인을 재사용한다.

## 8. 플레이어 사망 및 King 패배 연결

보스 공격으로 플레이어 기물이 죽으면:

```text
보드 점유 해제
↓
기존 사망 연출
↓
DeadPile 이동
↓
덱 UI 갱신
```

흐름을 사용한다.

King이 공격받은 경우 실제 기물 HP를 `RunState.KingHp`와 동기화하고 HP 0에서 기존 `BattleOutcome.Defeat` 흐름을 사용한다.

## 9. 일반 AI와 보스 AI 통합

`EnemyAITurnDriver`에서 일반 적 AI와 보스 AI를 같은 EnemyTurn에 연결했다.

Phase 1에서는:

```text
EnemyAIAdvancedPlanner
+
BossActionPlanner
↓
최고 후보 점수 비교
↓
행동 하나 실행
```

일반 적은 `EnemyAIActionExecutor`, 보스는 `BossActionExecutor`를 사용한다.

한 EnemyTurn에서 일반 적과 보스가 동시에 두 번 행동하지 않도록 선택된 행동 하나만 실행한다.

---

# CountPieces 회귀 수정

## 10. 기존 테스트 회귀 확인

보스 전투 작업 중 다음 기존 테스트 실패가 확인되었다.

```text
CountPieces_ReturnsExactCountPerSide
EnemyTurn_SummonOneCard_ImmediatelyCompletesEnemyTurn
TryEnemySummonOneCard_RefillsHandFromDrawPile_WhenHandEmpty
```

적 카드 소환 자체는 정상 로그가 출력됐지만 최종 `CountPieces(false)`가 0을 반환하고 있었다.

## 11. 회귀 원인

37일차에서 실제 죽은 2×2 보스를 남은 적 수에서 제외하기 위해 `CountPieces()`에 사망 기물 제외를 추가했다.

기존 EditMode 테스트는 `BaseHp`를 별도로 설정하지 않은 임시 `PieceDefinition`을 사용한다.

```text
BaseHp = 0
↓
PieceRuntimeState.CurrentHp = 0
↓
IsDead = true
```

이 때문에 테스트용 기물까지 전부 사망 기물로 간주돼 카운트에서 빠졌다.

## 12. CountPieces 호환 보정

다음 규칙으로 수정했다.

```text
BaseHp > 0이고 HP 0
→ 실제 사망 기물로 제외

BaseHp = 0인 레거시/테스트 임시 정의
→ 보드에 점유되어 있으면 정상 카운트
```

동시에 2×2 카운트 규칙은 유지한다.

```text
살아 있는 2×2 Boss = 1기
실제 사망한 2×2 Boss = 0기
```

---

# Phase 2 및 텔레그래프 통합

## 13. 보스 페이즈 상태 추가

보스 한 기마다 런타임 페이즈 상태를 관리한다.

```text
Phase 1
↓
현재 HP <= 최대 HP 50%
↓
Phase 2
```

프로토타입 보스가 HP 30이면:

```text
16~30 → Phase 1
1~15  → Phase 2
0     → 사망
```

같은 보스가 Phase 2 진입 이벤트를 여러 번 실행하지 않도록 한 번만 전환된다.

## 14. Phase 2 진입 1회 증원

Phase 2에 처음 진입하면 기존 `PlayerStartingDeck26` 카탈로그와 `BoardInputController.SpawnTestEnemy()`를 재사용해 증원을 시도한다.

프로토타입 구성:

```text
Knight 1기
Pawn   1기
```

빈 적 진영 후보 칸을 순서대로 검사하며 기존 기물을 덮어쓰지 않는다.

증원은 같은 보스에서 한 번만 호출된다.

## 15. 텔레그래프 상태 추가

Phase 2 공격은 즉시 피해를 주지 않고 다음 EnemyTurn까지 실행 대기 상태를 보관한다.

저장 정보:

```text
Boss
PatternType
DisplayName
TargetCells
PlannedTurn
```

중요한 점은 `TargetCells`를 예고 순간 복사해 고정한다는 것이다.

```text
EnemyTurn A
→ TargetCells 계산
→ 위험 칸 표시
→ 피해 없음
→ 턴 종료

PlayerTurn
→ 플레이어 회피

EnemyTurn B
→ 저장된 동일 TargetCells 공격
```

## 16. Phase 2 패턴 1 — 주변 강타

2×2 보스 주변 1칸 전체를 공격한다.

```text
X X X X
X ■ ■ X
X ■ ■ X
X X X X
```

보드 중앙의 2×2 보스 기준 총 12칸이 위험 영역이다.

보스 자신의 점유 칸은 범위에서 제외한다.

## 17. Phase 2 패턴 2 — 왕을 겨누는 직선

현재 플레이어 King 위치를 기준으로 가장 가까운 주축 방향을 선택한다.

기본 크기:

```text
보스 몸체 폭 2칸
×
길이 3칸
```

예:

```text
■ ■ X X X
■ ■ X X X
```

King이 상하 방향에 있으면 같은 구조가 세로 방향으로 회전한다.

## 18. 패턴 교대

Phase 2에서 한 패턴만 반복하지 않도록 두 패턴을 번갈아 사용한다.

```text
주변 강타
↓
왕을 겨누는 직선
↓
주변 강타
↓
왕을 겨누는 직선
```

## 19. 텔레그래프와 실제 공격 범위 일치

화면 표시용 범위와 공격 판정용 범위를 따로 계산하지 않는다.

```text
BossTelegraphState.TargetCells
        ↓
├─ BossTelegraphOverlay
│  └─ 화면 위험 칸
│
└─ BossActionExecutor
   └─ 실제 범위 공격
```

따라서 화면에 표시된 위험 칸과 실제 피해 판정이 동일한 데이터를 사용한다.

## 20. 위험 칸 시각화

보드 위에 얇은 런타임 경고 타일을 생성한다.

```text
주변 강타
→ 붉은색

왕을 겨누는 직선
→ 주황색
```

경고 타일의 Collider는 제거해 기존 보드 클릭을 가로채지 않도록 했다.

## 21. 예고 공격 실행

다음 EnemyTurn이 시작되면 기존 `TargetCells`를 읽어 현재 그 칸에 남아 있는 플레이어만 공격한다.

```text
위험 칸에서 이동함
→ 피해 없음

위험 칸에 남음
→ CombatResolver 피해
```

같은 플레이어 기물이 여러 위험 칸과 겹쳐도 `HashSet<PieceRuntimeState>` 기준으로 한 번만 피해를 받는다.

위험 칸에 아무도 없어도 공격 행동 자체는 정상적으로 소비되고 EnemyTurn이 종료된다.

## 22. Phase 2 상단 상태 UI

기존 상단 Turn UI와 Round UI 아래에 보스 상태 줄을 추가했다.

예:

```text
BOSS PHASE 2    예고 : 주변 강타
```

Phase 전환 직후 증원이 성공하면:

```text
BOSS PHASE 2    다음 패턴 준비    증원 발생
```

형식으로 표시한다.

## 23. Phase 2 우선 EnemyTurn 처리

Phase 2 보스가 존재하는 경우 `EnemyAITurnDriver`에서 일반 AI보다 먼저 Phase 2 상태를 확인한다.

```text
EnemyTurn
↓
Phase 2 보스 확인
├─ Pending Telegraph 있음
│  → 예고 공격 실행
│
└─ Pending Telegraph 없음
   → 다음 패턴 예고
```

Phase 2 보스가 이번 턴을 소비하면 같은 EnemyTurn에서 일반 적이나 Phase 1 보스가 추가 행동하지 않는다.

Phase 2 보스가 없으면 기존 38일차 일반 적/보스 AI 흐름을 그대로 사용한다.

---

# 테스트 추가

## 24. 보스 기본 전투 테스트

다음 내용을 검증하는 테스트를 추가했다.

- 2×2 전체 외곽 공격 대상 탐색.
- 인접 King 우선 공격.
- 공격 대상이 없으면 King 쪽 이동.
- 막힌 2×2 영역 이동 후보 제외.
- 이동 후 새 네 칸 전체 점유.
- 기존 CombatResolver를 통한 보스 공격.
- 행동 후 EnemyTurn 종료.
- 2×2 네 칸 때문에 동일 보스 행동이 중복 생성되지 않는지 확인.

## 25. CountPieces 호환 테스트

- BaseHp 0 테스트 기물 정상 카운트.
- 살아 있는 2×2 기물 1기로 카운트.
- BaseHp가 설정된 사망 2×2 기물 제외.

## 26. Phase 2·텔레그래프 테스트

- HP 50% Phase 2 진입.
- Phase 2 중복 진입 방지.
- 증원 요청 1회 제한.
- 주변 강타 12칸 계산.
- King 방향 2×3 직선 계산.
- 텔레그래프 생성 시 즉시 피해 없음.
- 예고 칸에 남은 플레이어만 범위 피해.
- 두 패턴 교대.
- Phase 2 상태 UI 문자열.

---

# 38일차 최종 구조

```text
EnemyTurn
↓
BossPhase2Controller
├─ Phase 2 + Pending Telegraph
│  └─ 예고 공격 실행
│
├─ Phase 2 + Pending 없음
│  └─ 다음 패턴 텔레그래프
│
└─ Phase 1 / Phase 2 보스 없음
   ↓
   EnemyAIAdvancedPlanner
   +
   BossActionPlanner
   ↓
   점수 비교
   ↓
   일반 적 또는 보스 행동 1회
```

보스 진행 구조:

```text
2×2 보스 생성
↓
Phase 1 이동·직접 공격
↓
HP 50%
↓
Phase 2 진입
↓
증원 1회
↓
범위 공격 예고
↓
PlayerTurn 회피
↓
다음 EnemyTurn 실행
↓
다음 패턴 예고
```

# 38일차 완료 기준 체크

- [x] 2×2 보스 기본 행동 데이터 추가.
- [x] 보스 전용 행동 플래너 추가.
- [x] 2×2 전체 외곽 직접 공격.
- [x] King 우선 공격.
- [x] 상하좌우 1칸 2×2 이동.
- [x] 전체 점유 영역 이동 가능 검사.
- [x] King 접근 이동 점수.
- [x] 보스 전용 행동 실행기 추가.
- [x] 기존 CombatResolver / BattleHooks 재사용.
- [x] 플레이어 사망 / DeadPile 처리 연결.
- [x] King HP / Defeat 연결.
- [x] 일반 적 AI와 Phase 1 보스 AI 통합.
- [x] CountPieces 테스트 호환 회귀 수정안 적용.
- [x] HP 50% Phase 2 전환.
- [x] Phase 2 진입 1회 증원.
- [x] 주변 강타 텔레그래프.
- [x] King 방향 직선 텔레그래프.
- [x] 예고 순간 무피해.
- [x] PlayerTurn 회피 기회 제공.
- [x] 다음 EnemyTurn 동일 TargetCells 범위 공격.
- [x] 위험 칸 시각화.
- [x] Phase 2 상단 상태 UI.
- [x] Phase 2 우선 EnemyTurn 처리.
- [x] 보스 기본 전투 테스트 작성.
- [x] CountPieces 호환 테스트 작성.
- [x] Phase 2·텔레그래프 테스트 작성.
- [ ] CountPieces 수정 후 Unity Test Runner 전체 EditMode 테스트 재실행 확인.
- [ ] 38일차+Phase 2 통합본 전체 Unity 컴파일 확인.
- [ ] Battle 씬에서 Phase 1 이동·직접 공격 수동 확인.
- [ ] HP 50% 진입 후 증원·상단 UI·위험 칸 표시 수동 확인.
- [ ] 위험 칸에서 이동한 기물이 다음 EnemyTurn 피해를 받지 않는지 수동 확인.
- [ ] 일반 적과 Phase 2 보스가 함께 있을 때 한 EnemyTurn에 행동이 중복되지 않는지 수동 확인.

38일차는 37일차의 **“2×2 보스가 보드에 존재한다”**는 기반을 **“기본 AI로 싸우고, 체력이 줄면 2페이즈로 전환하며, 플레이어가 읽고 피할 수 있는 범위 패턴을 사용하는 보스전”**으로 확장한 일차다.
