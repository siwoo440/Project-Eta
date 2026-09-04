# 33일차 개발 일지 — 적 AI 평가 코어 및 기존 전투 기능 회귀 복구

**날짜**: 2026-09-04  
**기준 커밋**: `5d38232230ce7758449eb96665ed7a3e1e72286f`  
**목표**: 적 기물이 현재 보드에서 합법 행동을 생성하고 점수 기반으로 한 수를 선택하도록 공통 AI 코어를 구축하며, 기존 상태 효과·저장·전투 연출 기능과 충돌하지 않도록 통합한다.

## 오늘 한 일

### 1. AI 행동 후보 구조 추가
`AIActionType`과 `AIActionCandidate`를 추가했다.

AI가 평가하는 행동 하나는 다음 정보를 가진다.

```text
Actor
Origin
Target
ActionType = Move / Attack
TargetPiece
Score
```

AI는 직접 보드 상태를 수정하지 않고, 우선 가능한 행동을 후보 데이터로 만든 뒤 평가 단계와 실행 단계를 분리한다.

### 2. 공통 적 AI 플래너 구현
`EnemyAIPlanner`를 추가했다.

현재 보드의 모든 살아 있는 적 기물을 순회하고 기존 `MovementResolver`를 사용해 합법 이동·공격 후보를 생성한다.

중요한 원칙:

```text
플레이어 이동 규칙
        +
적 AI 이동 규칙
        ↓
같은 MovementResolver 사용
```

Knight, Rook, Cannon, Grasshopper 같은 기물의 이동법을 AI 코드에 다시 구현하지 않는다.

### 3. 기본 AI 점수 체계 추가
33일차에서는 역할별 고급 AI보다 공통 점수 체계를 우선 구현했다.

현재 임시 평가값:

```text
일반 이동 기본값             +10
킹과 거리 1칸 감소           +25
즉시 공격 가능             +1000
처치 가능한 공격            +400
예상 피해 1당                +40
킹 직접 공격               +5000
```

현재 HP도 동일 조건에서 작은 보조 가중치로 사용한다.

수치는 AI 구조 검증을 위한 프로토타입 값이며 이후 역할별 AI와 밸런싱 단계에서 조정할 예정이다.

### 4. 결정론적 동점 처리
동일 보드 상태에서 테스트 결과가 매번 달라지지 않도록 무작위 동점 처리를 사용하지 않는다.

우선순위:

```text
높은 Score
↓
Attack 우선
↓
PieceId 순
↓
행동 주체 좌표 순
↓
목표 좌표 순
```

같은 보드를 입력하면 같은 행동을 선택하도록 구성했다.

### 5. 기존 전투 파이프라인을 사용하는 AI 실행기 추가
`EnemyAIActionExecutor`를 추가했다.

AI가 선택한 행동은 새 전투 규칙으로 별도 처리하지 않고 기존 시스템을 사용한다.

```text
AI 행동 선택
↓
MovementResolver 재검증
↓
CombatResolver
↓
BattleHooks
↓
CombatMovementPolicy
↓
보드 점유 변경
↓
턴 종료
```

이를 통해 기존 피해 판정, Cannon 원거리 처치 정책, 전투 훅, 상태 효과 턴 정산을 최대한 그대로 공유한다.

### 6. AI 이동 처리
AI 이동 직전 다시 `MovementResolver`로 합법 여부를 확인한다.

정상 이동 시:

- 원래 타일 점유 해제.
- `PieceRuntimeState.BoardPosition` 갱신.
- 새 타일 점유.
- `BeforeMove` / `AfterMove` 훅 발생.
- 화면에 `BoardView`가 있으면 실제 `PieceView` 이동 연출 연결.
- Chameleon은 기존 `BoardPosition` 갱신 규칙에 따라 이동 순환 단계 진행.

### 7. AI 공격 처리
공격 역시 실행 직전 공격 가능 좌표인지 다시 확인한다.

공격 시:

- `BeforeAttack` 훅.
- 기존 `CombatResolver.ResolveAttack()` 사용.
- 사망하면 플레이어 기물을 보드에서 제거.
- 플레이어 카드의 `DeadCardPile` 생명주기 반영.
- 근접 기물은 기존 점유 정책에 따라 처치 칸 점유.
- Cannon 같은 원거리 기물은 기존 `CombatMovementPolicy`에 따라 원위치 유지.
- `AfterAttack` 훅 발생.

### 8. 킹 처치·패배 처리
AI가 플레이어 King을 공격하면 실제 King 런타임 HP와 `RunState.KingHp`를 동기화한다.

King HP가 0이 되면:

```text
BattleEnded
+
BattleOutcome.Defeat
```

로 즉시 전환한다.

### 9. 적 턴 자동 AI 연결
`EnemyAITurnDriver`를 추가했다.

Battle 씬에서 별도 Inspector 설정 없이 자동 생성되고 `EnemyTurn`을 감지해 AI 행동을 수행한다.

33일차부터는 기존 프로토타입 흐름인:

```text
적 카드 1장 자동 소환
→ 즉시 턴 종료
```

대신:

```text
현재 적 기물 조사
→ 행동 후보 생성
→ 점수 평가
→ 최고 행동 선택
→ 이동 또는 공격
→ 적 턴 종료
```

를 사용하도록 구성했다.

기존 적 카드 자동 소환용 손패와 DrawPile은 AI 드라이버가 비워 보드 기물 AI 행동과 중복되지 않게 한다.

### 10. 행동 불가능 상태 안전 처리
다음 상황에서는 AI가 무한 대기하지 않도록 안전하게 적 턴을 종료한다.

- 적 기물이 없음.
- 기절로 이동·공격이 모두 불가.
- 보드가 완전히 막힘.
- 후보가 실행 직전에 무효화됨.

턴 종료 시 기존 `TurnEnd` 훅도 발행해 상태 효과 정산 흐름을 유지한다.

### 11. Day33 AI 코어 테스트 추가
`Day33EnemyAICoreTests`를 추가했다.

주요 테스트:

- 플레이어 기물은 AI 행동 주체가 되지 않음.
- 공격 가능한 경우 이동보다 공격을 우선.
- King 직접 공격을 일반 기물 공격보다 우선.
- 이동·공격 권한이 모두 없는 적은 후보를 만들지 않음.
- 같은 보드에서는 같은 행동을 선택.
- AI 이동 후 보드 점유와 좌표 갱신.
- 적 행동 후 EnemyTurn 정상 종료.
- King 처치 시 Defeat.
- 적 행동 후보가 없으면 안전하게 턴 종료.

## 33일차 회귀 오류 및 복구

초기 33일차 적용 과정에서 일부 기존 파일이 오래된 버전으로 덮어써져 27~30일차 기능 일부가 사라지는 문제가 발생했다.

확인된 대표 컴파일 오류:

```text
PieceRuntimeState.StatusEffects 누락
PieceRuntimeState.TickStatusEffects 누락
RunState.FromSaveData 3인자 오버로드 누락
PieceView.PlayNonLethalStrikeAndReturn 누락
PieceView.PlayHitReaction 누락
PieceView.PlayDeathTogglingThenDestroy 누락
PieceView.SnapTo 누락
```

### 12. 상태 효과 API 복구
`PieceRuntimeState`를 32일차 기준 기능으로 복구했다.

복구 항목:

- `StatusEffects`
- `ApplyStatus`
- `HasStatus`
- `FindStatus`
- `RemoveStatus`
- `TickStatusEffects`
- `RestoreStatusEffect`
- Stun/Root에 따른 `CanMove`, `CanAttack` 갱신.

### 13. 상태 효과 저장·복원 복구
`RunSaveData`와 `RunState`의 27일차 상태 효과 저장 구조를 복구했다.

- `StatusEffectSaveData`.
- 각 기물의 상태 효과 목록 저장.
- 남은 지속 턴 저장.
- 중첩 수 저장.
- `StatusEffectDatabase`를 이용한 불러오기.
- `RunState.FromSaveData(data, database, statusEffectDatabase)` 3인자 경로 복구.

### 14. 상태 이상 이동 규칙 복구
`MovementResolver`에서 상태 이상 행동 제한을 다시 적용했다.

```text
Stun
→ 이동 불가
→ 공격 불가

Root
→ 이동 불가
→ 공격 가능
```

Chameleon의 현재 이동 순환 단계 처리도 유지한다.

### 15. PieceView 전투 연출 API 호환 복구
현재 26종 프로토타입 모델링을 유지하기 위해 `PieceView.cs`를 30일차 파일로 통째로 되돌리지 않고 `PieceViewCombatAnimationExtensions`를 추가했다.

제공 API:

- `SnapTo`
- `PlayNonLethalStrikeAndReturn`
- `PlayHitReaction`
- `PlayDeathTogglingThenDestroy`

이를 통해 현재 PieceView의 26종 모델 분기를 유지하면서 BoardInputController와 AI 실행기가 요구하는 30일차 전투 연출 API를 다시 사용할 수 있게 했다.

## 33일차 최종 구조

```text
EnemyTurn
↓
EnemyAITurnDriver
↓
EnemyAIPlanner
├─ MovementResolver
├─ Move 후보
└─ Attack 후보
↓
점수 평가
↓
AIActionCandidate 선택
↓
EnemyAIActionExecutor
├─ CombatResolver
├─ BattleHooks
├─ CombatMovementPolicy
├─ DeadCardPile
└─ King 패배 처리
↓
TurnManager
```

기존 기능과의 통합:

```text
PieceRuntimeState
├─ Chameleon 순환
├─ StatusEffects
├─ Stun
└─ Root

RunState
├─ 상태 효과 저장
└─ 상태 효과 복원

PieceView
└─ 전투 연출 호환 Extension
```

## 완료 기준 체크

- [x] AIActionCandidate / AIActionType 추가.
- [x] 적 기물 전체 합법 행동 후보 생성.
- [x] MovementResolver 공통 사용.
- [x] 기본 점수 평가 구조 추가.
- [x] King 직접 공격 우선 평가.
- [x] 결정론적 동점 규칙 추가.
- [x] 기존 CombatResolver 기반 공격 실행.
- [x] BattleHooks 이동·공격·턴 종료 연결.
- [x] Cannon 원거리 처치 정책 재사용.
- [x] King 처치 시 패배 처리.
- [x] 행동 후보 없음 안전 종료.
- [x] Day33EnemyAICoreTests 추가.
- [x] StatusEffects / TickStatusEffects 복구.
- [x] RunState 상태 효과 저장·복원 복구.
- [x] Stun / Root 이동 후보 처리 복구.
- [x] PieceView 전투 연출 호환 API 복구.
- [ ] GitHub CI 기반 Unity 컴파일 확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 최종 통과 확인.
- [ ] Battle 씬에서 실제 적 AI 이동·공격 수동 확인.

33일차에서는 적 AI가 별도의 체스 규칙을 갖는 방식이 아니라 기존 보드·이동·전투 시스템 위에서 행동 후보만 평가하도록 기반을 만들었다. 구현 과정에서 발견된 기존 상태 효과·저장·연출 회귀는 32일차 기능을 기준으로 복구했으며, 현재 구조는 34일차의 근접형·슬라이더·도약형 역할별 AI 가중치를 같은 플래너 위에 확장할 수 있는 상태를 목표로 한다.
