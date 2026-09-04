# 34일차 개발 일지 — 기본 역할별 AI 및 F1 AI 점수 디버그 창

**날짜**: 2026-09-04  
**기준 커밋**: `c64437259a80afd0c47d617f3cb25003e97a1abb`  
**목표**: 33일차 공통 AI 평가 코어 위에 근접형·슬라이더·도약형의 역할별 성격을 추가하고, F1 디버그 창에서 현재 AI 후보별 점수를 직접 확인할 수 있게 한다.

## 오늘 한 일

### 1. 기본 AI 역할 분류 추가
`EnemyAIBasicRole`을 추가했다.

34일차에서 사용하는 기본 성격은 다음 세 가지다.

```text
Melee
Slider
Jumper
```

`None`은 34일차 기본 역할 보정을 적용하지 않는 기물에 사용한다.

### 2. PieceRoleTag 기반 역할 분류
`EnemyAIRoleClassifier`를 추가했다.

`PieceDefinition.RoleTags`를 기준으로 AI 성격을 결정한다.

우선순위:

```text
Slider
→ Jumper
→ Melee
```

여러 역할을 동시에 가진 기물은 위 우선순위에 따라 대표 역할 하나를 사용한다.

다음 분류는 35일차 이후 별도 AI 대상으로 남겨 두었다.

```text
Special
Monster
Boss
```

따라서 Cannon, Grasshopper 등 특수 기물은 34일차 기본 역할 점수에 포함하지 않는다.

### 3. 근접형 AI
근접형은 플레이어 King에게 접근하는 행동을 더 높게 평가한다.

현재 프로토타입 역할 보너스:

```text
King과 거리 1칸 감소    +60
King 바로 옆 도달      +180
```

King과 멀어지는 이동은 반대로 감점된다.

근접형 AI의 기본 성격:

```text
King 접근
↓
근접 압박 위치 확보
↓
기존 33일차 공격 점수로 공격 선택
```

### 4. 슬라이더 AI
Rook, Bishop처럼 직선·대각선으로 넓게 움직이는 기물은 무조건 King에게 붙기보다 열린 공격선과 이동 공간을 선호하도록 구성했다.

현재 프로토타입 역할 보너스:

```text
이동 후 합법 선택지 1칸당   +4
King 공격선 확보          +350
```

후보 위치에 실제로 기물이 이동했다고 가정한 뒤 기존 `MovementResolver`를 다시 사용해 다음 행동 가능 범위를 계산한다.

### 5. 도약형 AI
Knight, Camel, Zebra 같은 도약형은 착지 후 다음 공격 위치와 기동성을 더 중요하게 평가한다.

현재 프로토타입 역할 보너스:

```text
착지 후 합법 선택지 1칸당   +6
착지 후 King 직접 위협     +420
```

도약 규칙 역시 AI 내부에 다시 구현하지 않고 기존 `MovementResolver`를 재사용한다.

### 6. 33일차 공통 점수 유지
34일차 역할 점수는 기존 점수를 교체하지 않는다.

최종 계산:

```text
33일차 Base Score
+
34일차 Role Bonus
=
Final Score
```

따라서 기존의 다음 규칙은 그대로 유지된다.

- 즉시 공격 우선.
- 처치 가능 공격 보너스.
- 예상 피해 점수.
- King 직접 공격 최우선.
- 결정론적 동점 처리.

### 7. EnemyAIRolePlanner 추가
`EnemyAIRolePlanner`를 추가했다.

흐름:

```text
EnemyAIPlanner
↓
합법 후보 + Base Score
↓
EnemyAIRoleScoreEvaluator
↓
Role Bonus 추가
↓
Final Score
↓
최고 행동 선택
```

33일차 `EnemyAIPlanner`와 `EnemyAIActionExecutor`는 그대로 유지한다.

### 8. EnemyAITurnDriver 연결 변경
실제 EnemyTurn에서는 기존 공통 플래너 대신 `EnemyAIRolePlanner`를 사용하도록 연결했다.

AI 행동 실행 방식 자체는 변경하지 않고 역할별 평가 단계만 추가했다.

### 9. 기본 역할 AI 테스트 추가
`Day34BasicRoleAITests`를 추가했다.

검증 항목:

- 근접형이 King에게 가까워지는 이동을 선호.
- 슬라이더가 King 공격선을 만드는 이동을 선호.
- 도약형이 다음 행동에 King을 위협하는 착지점을 선호.
- Special 기물은 34일차 역할 보너스에서 제외.
- 역할 점수가 기존 Base Score에 추가되는지 확인.
- 역할 보정 이후에도 즉시 공격 우선순위 유지.

## F1 AI 점수 디버그 창

### 10. 전역 디버그 창 추가
`ProjectEtaDebugWindow`를 추가했다.

별도 Canvas 또는 Inspector 연결 없이 런타임에 자동 생성되며 게임 실행 중 `F1`로 열고 닫을 수 있다.

현재 페이지 구성:

```text
Page 1 / 1
1. AI 점수 로그
```

향후 다른 디버그 페이지를 추가할 수 있도록 페이지 번호 구조를 사용한다.

### 11. AI 점수 스냅샷 구조
`AIDebugScoreSnapshot`과 `AIDebugScoreEntry`를 추가했다.

각 행동 후보에서 다음 값을 따로 기록한다.

```text
Actor
Origin
Target
ActionType
Role
BaseScore
RoleBonus
FinalScore
IsSelected
```

### 12. 실제 AI 선택과 같은 점수 계산
`AIDebugScoreSnapshotBuilder`는 현재 보드에서:

```text
EnemyAIPlanner
→ Base Score

EnemyAIRoleScoreEvaluator
→ Role Bonus

EnemyAIRolePlanner
→ 실제 선택 행동
```

을 다시 계산해 화면에 표시한다.

최종 점수는 실제 AI와 동일하게:

```text
Final = Base + Role
```

로 계산한다.

### 13. AI 점수 로그 1페이지
F1 디버그 창의 첫 페이지에서 모든 현재 행동 후보를 확인할 수 있다.

표시 예:

```text
▶ [Slider] Rook (4,8) -> (4,5) Attack
  | Base 1086
  | Role +350
  | Final 1436
```

의미:

```text
Base
= 33일차 공통 AI 점수

Role
= 34일차 역할 추가 점수

Final
= 최종 행동 평가 점수
```

실제로 AI가 선택할 행동에는 `SELECT`와 `▶` 표시가 붙는다.

### 14. 디버그 창 보조 기능
현재 창에서 다음 기능을 제공한다.

- F1 열기/닫기.
- 현재 Scene 표시.
- 현재 TurnState 표시.
- 전체 행동 후보 수 표시.
- 실제 선택 행동 상단 강조.
- Final Score 높은 순 정렬.
- 0.2초 간격 자동 갱신.
- `지금 갱신` 수동 새로고침.
- `Console 출력`으로 현재 후보 전체를 Unity Console에 기록.
- 창 드래그 이동.

### 15. AI 디버그 점수 테스트 추가
`Day34AIDebugScoreTests`를 추가했다.

검증 항목:

- Base Score와 Role Bonus가 분리되는지 확인.
- `FinalScore = BaseScore + RoleBonus` 확인.
- 실제 선택 행동에 `IsSelected` 표시.
- Special 기물 역할 보너스가 0인지 확인.

## 34일차 최종 AI 구조

```text
EnemyTurn
↓
EnemyAITurnDriver
↓
EnemyAIRolePlanner
├─ EnemyAIPlanner
│  └─ Base Score
│
└─ EnemyAIRoleScoreEvaluator
   ├─ Melee
   ├─ Slider
   └─ Jumper
↓
Final Score
↓
EnemyAIActionExecutor
↓
기존 전투 파이프라인
```

디버그 흐름:

```text
F1
↓
ProjectEtaDebugWindow
↓
AIDebugScoreSnapshotBuilder
├─ Base Score
├─ Role Bonus
├─ Final Score
└─ SELECT 행동
↓
Page 1 : AI 점수 로그
```

## 완료 기준 체크

- [x] 근접형 AI 역할 분류.
- [x] 슬라이더 AI 역할 분류.
- [x] 도약형 AI 역할 분류.
- [x] PieceRoleTag 기반 역할 선택.
- [x] Special / Monster / Boss를 35일차 대상으로 분리.
- [x] 근접형 King 접근 보너스.
- [x] 슬라이더 공격선·기동성 보너스.
- [x] 도약형 King 위협 착지·기동성 보너스.
- [x] 기존 33일차 Base Score 유지.
- [x] EnemyAIRolePlanner 추가.
- [x] EnemyTurn에 역할 플래너 연결.
- [x] Day34BasicRoleAITests 추가.
- [x] F1 런타임 디버그 창 추가.
- [x] 첫 페이지 AI 점수 로그 구현.
- [x] Base / Role / Final 점수 분리 표시.
- [x] 실제 SELECT 행동 표시.
- [x] Unity Console 점수 덤프 기능.
- [x] Day34AIDebugScoreTests 추가.
- [ ] GitHub CI 기반 Unity 컴파일 확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 최종 통과 확인.
- [ ] Battle 씬에서 F1 디버그 창과 실제 AI 선택 행동 수동 대조 확인.

34일차에서는 33일차 공통 AI 구조를 유지한 채 역할별 성격만 점수 보정층으로 추가했다. 또한 AI가 왜 특정 행동을 선택하는지 직접 확인할 수 있도록 F1 디버그 창을 추가해 이후 35일차 특수 AI와 위협 평가를 조정할 때 점수 원인을 추적할 수 있는 기반을 마련했다.
