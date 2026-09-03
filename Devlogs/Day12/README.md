# 12일차 개발 일지 — HP·ATK 전투 판정

**날짜**: 2026-09-03
**목표**: 11일차에서 감지만 하던 공격 후보 칸 클릭에 실제 HP·ATK 전투 판정을 연결한다. 기획서 확정 규칙대로 비치명 공격은 공격자 원위치 유지, 치명 공격은 공격자가 대상 칸 점유로 처리하고, 킹이 죽으면 런이 패배하도록 `RunState.KingHp`와 연동한다.

## 오늘 한 일

### 1. 전투 판정 로직 (`Battle/CombatResolver.cs`, `Battle/CombatResult.cs`)
- `CombatResolver.ResolveAttack(attacker, defender)`: 공격자의 `BaseAtk`만큼 대상 `CurrentHp`를 감소시키고, 실제 피해량과 사망 여부를 `CombatResult`로 반환.
- 방어력·회피 등 추가 계산 없이 고정 ATK 피해라는 확정 규칙만 반영(향후 확장 지점).

### 2. 공격 실행 연결 (`Board/BoardInputController.cs`)
- `ExecuteAttack(target)`으로 `HandleAttackCandidateClick`의 로그 전용 자리표시를 실제 판정으로 교체.
- 비치명 공격(대상 생존): HP만 감소, 양측 위치 그대로 유지.
- 치명 공격(대상 사망): `RemovePieceFromBoard`로 대상을 보드·화면에서 제거하고, `MovePieceTo`로 공격자를 대상 칸으로 이동(점유) — 이동 실행과 동일한 메서드를 재사용해 좌표·점유·`PieceView` 갱신을 한 곳에서 처리.
- 공격도 이동과 동일하게 `TurnManager.TryCompletePlayerAction()`을 호출해 플레이어의 이번 턴 행동으로 소비.
- `TryAttackSelectedPieceTarget(target)` 공개 진입점 추가(테스트·향후 AI에서도 재사용 가능).
- `AttackResolved` 이벤트 추가 — 전투 결과를 외부(킹 HP 동기화 등)에 알림.

### 3. 킹 HP·패배 연동 (`Battle/BattleController.cs`)
- `BoardInputController.AttackResolved`를 구독해, 피격 대상이 아군 킹(`IsPlayerPiece && MovementType == King`)이면 `RunState.KingHp`를 그 킹 기물의 실제 `CurrentHp`로 동기화.
- `RunState.IsDefeated`(킹 HP 0 이하)가 되면 `EndBattle()`을 호출해 턴 진행을 멈춤.

### 4. 테스트
- `Tests/EditMode/CombatResolverTests.cs`: 고정 피해 계산, 정확히 0에서 사망 판정, 초과 피해에도 HP가 음수로 내려가지 않음을 검증.
- `Tests/EditMode/AttackExecutionTests.cs`: 비치명 공격(HP 감소·위치 유지·`AttackResolved` 이벤트), 치명 공격(대상 제거·공격자 점유), 후보가 아닌 칸 공격 거부(상태·턴 불변)를 검증.

## 확인된 흐름

```
공격 후보(주황) 칸 클릭
    ↓
CombatResolver.ResolveAttack 실행
    ↓
대상 생존 → 공격자 원위치 유지        대상 사망 → 공격자가 대상 칸 점유 + 대상 제거
    ↓                                        ↓
              둘 다: TurnManager가 적 턴으로 전환
                        ↓ (대상이 아군 킹이었다면)
              RunState.KingHp 동기화 → 0 이하면 BattleController.EndBattle()
```

## 오늘 하지 않은 것 / 알려진 한계

- 방어력, 반격, 상태 이상 등 추가 전투 공식은 없음 — 고정 ATK 피해만 확정 규칙대로 구현.
- 킹 HP·패배 연동(`BattleController.HandleAttackResolved`)은 씬 부트스트랩(Awake 자동 생성)에 의존하는 코드라 EditMode 단위 테스트로 검증하지 않음 — 기존 `TurnStatusUITests`처럼 독립적으로 인스턴스화하기 어려운 컴포넌트라 Play 모드 수동 확인으로 대체.
- 적 전멸 승리 조건은 아직 연결하지 않음 — 다음 순서.

## 완료 기준 체크

- [x] 공격 후보 칸을 클릭하면 실제로 HP가 감소한다.
- [x] 비치명/치명에 따라 공격자 원위치 유지 vs 대상 칸 점유가 정확히 갈린다.
- [x] 공격도 턴을 소비해 적 턴으로 전환된다.
- [x] 아군 킹이 죽으면 `RunState.KingHp`가 갱신되고 전투가 종료된다(코드 연결 완료, Play 모드 확인 필요).
- [x] `CombatResolver`/공격 실행 EditMode 테스트 작성.
- [ ] Unity 에디터에서 실제 컴파일 확인(Console Error 0) *(에디터 작업 필요)*
- [ ] Battle 씬 Play로 공격 → HP 감소 → 킹 사망 시 전투 종료까지 실제 확인 *(에디터 작업 필요)*
- [ ] Test Runner에서 `CombatResolverTests`/`AttackExecutionTests` 통과 확인 *(에디터 작업 필요)*

## 남은 일 (사용자가 직접)

1. Unity 에디터를 열어 컴파일 에러가 없는지 확인.
2. Battle 씬 Play → King/Pawn을 배치해 서로 공격시켜 보며 HP 감소, 비치명 원위치, 치명 점유가 설명대로 동작하는지 확인.
3. 테스트용 킹 카드의 HP를 낮춰 실제로 킹이 죽었을 때 `TurnStatusUI`가 "전투 종료"로 바뀌는지 확인.
4. Test Runner(EditMode)에서 새 테스트가 통과하는지 확인.
5. 문제가 있으면 Console 오류를 기준으로 다음 턴에서 수정.
