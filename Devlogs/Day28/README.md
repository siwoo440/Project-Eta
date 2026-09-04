# 28일차 개발 일지 — 독·화상·기절·속박 4종 구현

**날짜**: 2026-09-04  
**기준 커밋**: `ff703306aca8914bb35cf41d0626f487f23fe347`  
**목표**: 27일차에 완성한 상태 효과 코어(프레임워크·면역·저장 복원) 위에, 기획서 6단계 원안 77~80일차에 해당하는 독·화상·기절·속박 4종을 실제 전투 흐름(턴 종료 틱 피해, 이동·공격 후보 계산)에 연결한다.

## 오늘 한 일

### 1. StatusEffectDefinition에 틱 피해량 추가
- `_tickDamagePerStack` 필드와 `TickDamagePerStack` 프로퍼티(음수 방지) 추가.
- 독처럼 중첩되는 상태는 `중첩 수 × 틱 피해`로 자연히 스케일되고, 화상처럼 갱신형인 상태는 항상 1중첩이므로 고정 피해로 동작.
- 27일차 정의는 필드가 없어도 기본값 0으로 호환.
- 실제 수치는 아직 확정 밸런스가 없어 테스트에서는 임시값(중첩당 1)으로 검증.

### 2. StatusEffectTickResolver 추가
`ProjectEta.Battle`에 `CombatResolver`, `CombatMovementPolicy`와 나란히 놓이는 정적 클래스.

- `ResolveTurnEndDamage(PieceRuntimeState)` : 현재 걸린 상태 중 Poison·Burn만 순회해 `중첩 수 × TickDamagePerStack`을 합산, 기물 HP에 한 번에 적용하고 실제 적용된 피해량을 반환.
- 독과 화상이 동시에 걸려 있으면 같은 턴에 자연히 합산됨(별도 우선순위 처리 없음).

### 3. 기절·속박을 CanMove/CanAttack에 연결
- `PieceRuntimeState`에 이미 있었지만 프로젝트 전체에서 아무도 읽지 않던 `CanMove`/`CanAttack` 플래그를 실제로 살렸다.
- 상태를 걸거나(`ApplyStatus`), 강제로 떼거나(`RemoveStatus`), 지속 턴이 다해 사라지거나(`TickStatusEffects`), 저장 데이터에서 복원될 때(`RestoreStatusEffect`) 마다 `RefreshActionFlags()`를 호출해 두 플래그를 다시 계산.
  - 기절: `CanAttack = false`, `CanMove = false` (행동 자체가 스킵됨)
  - 속박: `CanMove = false`, `CanAttack = true` (제자리에서 공격은 허용)
- `MovementResolver.GetReachableTiles(PieceRuntimeState, BoardState)`가 이 두 플래그를 그대로 소비:
  - 기절이면 계산 자체를 생략하고 빈 `MovementResult` 반환.
  - 속박이면 정상적으로 계산한 뒤 `MoveTiles`만 비우고 `AttackTiles`는 그대로 반환.
- UI든 AI든 결국 이 메서드를 거치므로, 진영·기물 종류에 상관없이 자동으로 규칙이 적용된다.

### 4. 턴 종료 시점에 상태 이상 정산 연결
- `BoardInputController.ApplyTurnEndStatusEffects()` 추가: 보드 10×10을 순회하며 각 점유 기물에 대해
  1. `StatusEffectTickResolver.ResolveTurnEndDamage`로 독·화상 피해 적용
  2. `TickStatusEffects()`로 지속 턴 감소·만료 제거(기절·속박 해제 포함)
  3. 사망했으면 기존 `RemovePieceFromBoard`(보드 해제·화면 오브젝트 제거·죽은 카드 더미 이동)를 그대로 재사용해 정리
- 지속 피해로 사망한 경우 공격자가 없으므로 칸 점유 로직은 자연히 발생하지 않음(기획서 7.6 규칙 그대로).
- `BattleController`가 `_turnManager.CompleteEnemyTurn()`으로 "플레이어+적 행동 1회씩 = 1턴"이 끝났다고 판단하는 바로 그 지점에 `ApplyTurnEndStatusEffects()` 호출을 한 줄 추가해, 실제 전투 루프에 연결.

### 5. Day28 상태 효과 전투 통합 테스트 추가
`Day28StatusEffectCombatTests`를 추가했다.

주요 검증 항목:

- 독(중첩형)은 중첩 수에 비례해 턴 종료 피해가 커짐.
- 화상(갱신형)은 재적용해도 중첩되지 않고 고정 피해가 반복됨.
- 독과 화상이 동시에 걸려 있으면 같은 턴에 피해가 합산됨.
- 지속 턴이 끝나 상태가 제거되면 더 이상 피해를 주지 않음.
- 기절 상태의 기물은 실제 `PieceDatabase`의 Rook을 이용해 이동·공격 후보가 모두 0개가 됨을 확인.
- 속박 상태의 기물은 이동 후보는 0개가 되지만, 인접한 적에 대한 공격 후보는 그대로 유지됨을 확인.
- 기절이 지속 턴 만료로 풀리면 `CanMove`/`CanAttack`이 복구되고 이동 후보도 다시 정상 계산됨.

테스트용 기물·상태 정의는 27일차와 동일하게 `ScriptableObject.CreateInstance` + `SerializedObject`로 즉석 구성해 기존 26종 데이터 에셋을 건드리지 않고, 기절·속박 검증에는 실제 `PieceDatabase.asset`의 Rook을 그대로 사용해 실제 이동 규칙과의 통합까지 확인했다.

## 28일차 최종 구조

```text
StatusEffectDefinition
└─ TickDamagePerStack (독·화상 전용, 임시값)

StatusEffectTickResolver
└─ ResolveTurnEndDamage : Poison + Burn 피해를 합산해 HP에 적용

PieceRuntimeState
└─ RefreshActionFlags (상태 변경마다 자동 호출)
    ├─ Stun  → CanMove=false, CanAttack=false
    └─ Root  → CanMove=false, CanAttack=true

MovementResolver.GetReachableTiles(PieceRuntimeState, board)
├─ CanMove && CanAttack 모두 false → 빈 결과
└─ CanMove만 false → MoveTiles 비움, AttackTiles 유지

BoardInputController.ApplyTurnEndStatusEffects
├─ 보드 전체 순회
├─ 틱 피해 적용 → 지속 턴 감소
└─ 사망 시 RemovePieceFromBoard 재사용

BattleController.CompleteDummyEnemyTurnAfterDelay
└─ CompleteEnemyTurn() 성공 시 ApplyTurnEndStatusEffects() 호출
```

## 완료 기준 체크

- [x] `StatusEffectDefinition`에 틱 피해량 필드 추가(임시값, 27일차 데이터 호환 유지).
- [x] `StatusEffectTickResolver`로 독·화상 턴 종료 피해 계산·적용.
- [x] 기절 시 이동·공격 후보 모두 차단.
- [x] 속박 시 이동만 차단, 공격은 유지.
- [x] 지속 턴 만료 시 기절·속박 자동 해제 및 행동권 복구.
- [x] `BattleController`의 실제 턴 종료 지점에 상태 이상 정산 연결.
- [x] 틱 사망 시 기존 사망 처리(보드 해제·죽은 카드 이동) 재사용.
- [x] 독·화상·기절·속박 통합 회귀 테스트 7종 추가.
- [ ] Unity Editor Test Runner에서 Day27·Day28 테스트 전체 Run All 최종 확인.
- [ ] Battle 씬에서 실제로 한 턴을 끝까지 진행해 `ApplyTurnEndStatusEffects` 호출 경로를 수동으로 확인(현재는 상태를 거는 카드/능력이 없어 피해 0으로 조용히 통과할 것).

28일차는 27일차에 만든 배관 위에 독·화상·기절·속박 4종을 실제로 얹는 데 집중했다. 독·화상은 새 `StatusEffectTickResolver`와 `BoardInputController.ApplyTurnEndStatusEffects`로 턴 종료 흐름에 연결했고, 기절·속박은 그동안 아무도 쓰지 않던 `PieceRuntimeState.CanMove`/`CanAttack` 플래그를 살려 `MovementResolver`가 소비하도록 만들어 UI·AI 구분 없이 규칙이 자동 적용되게 했다. 사망 처리는 기존 근접 전투용 `RemovePieceFromBoard`를 그대로 재사용해 중복 로직을 만들지 않았다. 다음 29일차는 이 4종과 이후 보스 기믹이 공통으로 올라탈 공격 전/후·피해 전/후·이동 전/후·턴 시작/종료 훅을 추가하는 데 집중한다.
