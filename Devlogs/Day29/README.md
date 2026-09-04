# 29일차 개발 일지 — 특수 능력 훅(이동·공격·피해·턴 시작/종료)

**날짜**: 2026-09-04  
**기준 커밋**: `ae884fe94c7f8b95cdc529b42b1ea0d911d872d8`  
**목표**: 기획서 6단계 원안 82일차에 해당하는 "공격 전/후, 피해 전/후, 이동 전/후, 턴 시작/종료 훅"을 추가해, 앞으로의 기물 고유 능력·보스 기믹이 `BoardInputController`나 `BattleController`의 이동·공격·턴 코드를 직접 고치지 않고도 공통 전투 파이프라인에 꽂힐 수 있게 한다. 동시에 28일차에 하드코딩했던 상태 이상 정산 호출을 이 훅의 첫 실사용 사례로 전환한다.

## 오늘 한 일

### 1. BattleHooks 이벤트 버스 추가
`ProjectEta.Battle`에 `CombatResolver`와 나란히 놓이는 새 클래스. 전투 1회당 하나씩 생성되는 인스턴스로 8개 C# 이벤트를 제공한다.

- `BeforeMove` / `AfterMove` (piece, origin, destination)
- `BeforeAttack` / `AfterAttack` (attacker, defender / CombatResult)
- `BeforeDamage` / `AfterDamage` (DamageContext / target, appliedAmount)
- `TurnStart` / `TurnEnd` (TurnState, turnNumber)

구독 방식은 기존 코드베이스 패턴(`BoardInputController.AttackResolved += HandleAttackResolved;` / `OnDestroy`에서 `-=`)을 그대로 따른다.

### 2. 피해 적용을 DamageResolver 한 곳으로 통합
- `DamageContext` : `Target`, `Source`, 조정 가능한 `Amount`를 담는 가변 컨텍스트.
- `DamageResolver.ApplyDamage(target, amount, source, hooks)` : `BeforeDamage` 발행 → `Amount`를 반영해 HP 차감 → `AfterDamage` 발행 순서로 통일.
- 기존에 `CombatResolver.ResolveAttack`과 `StatusEffectTickResolver.ResolveTurnEndDamage`가 각각 직접 `piece.CurrentHp -= damage`로 처리하던 부분을 모두 `DamageResolver.ApplyDamage` 호출로 교체.
- 두 메서드 모두 `BattleHooks hooks = null` 선택적 매개변수를 추가해, 훅이 없으면(테스트 등) 기존과 100% 동일하게 동작하도록 유지.
- 이제 보호막(`ShieldEffect`) 같은 향후 능력이 `BeforeDamage`를 구독해 `Amount`를 줄이기만 하면 되는 자리가 생겼다.

### 3. 이동 전/후 — MovePieceTo 한 곳에 연결
`ExecuteMove`와 `ExecuteAttack`의 처치 후 전진 이동이 모두 `MovePieceTo`를 거치므로, 이 메서드 하나에 `BeforeMove`(좌표 변경 전)·`AfterMove`(보드·화면 반영 후)를 걸어 두 경로를 모두 자동으로 커버했다.

### 4. 공격 전/후 — ExecuteAttack에 연결
`CombatResolver.ResolveAttack` 호출 직전에 `BeforeAttack(attacker, defender)`를, 사망 처리·전진까지 모두 끝난 뒤 `AfterAttack(result)`를 발행하도록 연결했다.

### 5. 턴 시작/종료 — 28일차 하드코딩을 훅으로 흡수
- `BattleController.HandleTurnChanged`가 `PlayerTurn` 진입을 감지하면 `BattleHooks.TurnStart`를 발행.
- 28일차에 `CompleteDummyEnemyTurnAfterDelay`에서 직접 호출하던 `_boardInputController?.ApplyTurnEndStatusEffects();`를 제거하고, 그 자리에서 `_battleHooks?.RaiseTurnEnd(...)`를 발행하도록 교체.
- `BoardInputController`가 `Bind()` 시점에 `HandleBattleHooksTurnEnd`를 `BattleHooks.TurnEnd`의 구독자로 등록해, 훅이 발행되면 기존 `ApplyTurnEndStatusEffects()`를 그대로 실행.
- 즉 28일차 독·화상 정산 로직이 "훅 시스템의 첫 실사용 사례"가 되어, 훅이 장식용 인프라로 남지 않고 실제로 살아있음을 바로 증명한다.

### 6. 기존 호출부 호환성 유지
- `CombatResolver.ResolveAttack`, `StatusEffectTickResolver.ResolveTurnEndDamage`, `BoardInputController.Bind` 모두 새 매개변수를 선택적으로 추가해, 기존 호출부(테스트 포함 10곳 이상)를 전혀 수정하지 않고도 그대로 컴파일된다.
- `BoardInputController.OnDestroy`에도 `BattleHooks.TurnEnd` 구독 해제를 추가해, 기존 `TurnManager.TurnChanged` 구독 해제와 동일한 정리 패턴을 유지했다.

### 7. Day29 전투 훅 통합 테스트 추가
`Day29BattleHooksTests`를 추가했다. `AttackExecutionTests`와 동일하게 실제 `BoardInputController`를 `GameObject.AddComponent`로 생성해 붙이는 방식으로, 훅이 실제 입력 흐름과 완전히 통합됐는지 검증한다.

주요 검증 항목:

- 일반 이동 시 `BeforeMove`가 좌표 변경 전에, `AfterMove`가 변경 후에 정확히 한 번씩, 올바른 좌표로 발행됨.
- 공격 시 `BeforeAttack`(피해 적용 전 상태) → `AfterAttack`(실제 판정과 일치하는 `CombatResult`) 순서로 발행됨.
- `BeforeDamage` 구독자가 `Amount`를 0으로 바꾸면(가짜 보호막) 공격은 정상 실행되지만 실제 HP는 깎이지 않음 — 훅이 실제로 "개입 가능"함을 증명.
- `BattleHooks.TurnEnd`를 직접 발행하면 `BattleController` 없이도 28일차 독 틱 피해가 구독을 통해 그대로 적용됨 — 하드코딩 제거 후 회귀 없음을 확인.

## 29일차 최종 구조

```text
BattleHooks (전투 1회당 1개)
├─ BeforeMove / AfterMove
├─ BeforeAttack / AfterAttack
├─ BeforeDamage / AfterDamage
└─ TurnStart / TurnEnd

DamageResolver.ApplyDamage
├─ BeforeDamage 발행 (Amount 조정 가능)
├─ HP 차감
└─ AfterDamage 발행

CombatResolver.ResolveAttack ────┐
StatusEffectTickResolver.ResolveTurnEndDamage ─┴─→ DamageResolver.ApplyDamage 경유로 통합

BoardInputController
├─ MovePieceTo        → BeforeMove / AfterMove
├─ ExecuteAttack       → BeforeAttack / AfterAttack
└─ HandleBattleHooksTurnEnd (TurnEnd 구독) → ApplyTurnEndStatusEffects (28일차 로직 재사용)

BattleController
├─ HandleTurnChanged(PlayerTurn) → TurnStart 발행
└─ CompleteDummyEnemyTurnAfterDelay → TurnEnd 발행(28일차 직접 호출 제거)
```

## 완료 기준 체크

- [x] `BattleHooks` 이벤트 버스 8종 추가.
- [x] `DamageContext`/`DamageResolver`로 피해 적용을 한 곳에 통합.
- [x] `CombatResolver`·`StatusEffectTickResolver`가 `DamageResolver`를 거치도록 전환(기존 호출부 무수정 호환).
- [x] `MovePieceTo`에 이동 전/후 훅 연결.
- [x] `ExecuteAttack`에 공격 전/후 훅 연결.
- [x] `BattleController`의 턴 시작/종료 지점에 훅 연결.
- [x] 28일차 상태 이상 정산을 하드코딩 호출에서 `TurnEnd` 훅 구독으로 전환.
- [x] 이동·공격·피해 개입·턴 종료 통합 회귀 테스트 4종 추가.
- [ ] Unity Editor Test Runner에서 Day27·Day28·Day29 전체 테스트와 기존 12~28일차 회귀 테스트 Run All 최종 확인(리팩터링 영향 범위 포함).
- [ ] Battle 씬에서 실제로 이동·공격·턴 종료를 진행해 훅 리팩터링 이후에도 기존 동작(비치명/치명 공격, 독 틱 피해)이 그대로 유지되는지 수동 확인.

29일차는 새 능력을 만들기보다, 앞으로의 모든 기물 고유 효과와 보스 기믹이 공통으로 올라탈 통지 지점을 만드는 데 집중했다. `BattleHooks`라는 얇은 이벤트 버스 하나로 이동·공격·피해·턴의 8개 시점을 노출했고, 특히 피해 적용을 `DamageResolver` 한 곳으로 모아 향후 보호막 같은 "피해를 가로채는" 능력이 들어올 자리를 미리 마련했다. 무엇보다 이 훅이 장식으로 그치지 않도록, 28일차에 만들었던 독·화상 정산 로직을 리팩터링해 `TurnEnd` 훅의 첫 실사용 구독자로 전환했다 — 새 인프라가 처음부터 실제 기능을 지탱하는 상태로 시작한다. 다음 30일차는 이 훅들 위에 공격 연출 상태 머신(상승→접근→타격→결과 대기)과 비치명/치명 결과에 따른 복귀·착지·전도 연출을 얹는 데 집중한다.
