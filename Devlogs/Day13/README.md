# 13일차 개발 일지 — 테스트용 적 배치 + 승리 조건·턴 제한

**날짜**: 2026-09-03
**목표**: 12일차 전투 판정을 실제로 테스트할 방법이 없다는 문제(적 기물이 보드에 하나도 없음)를 먼저 해결하고, 기획서 확정/테스트 값 기준으로 승리 조건(적 전멸)과 라운드 턴 제한(30턴)을 연결한다.

## 오늘 한 일

### 1. 테스트용 적 배치 (`Board/BoardInputController.cs`, `Battle/BattleController.cs`)
- `TryPlayCardOnCell`의 기물 생성 로직을 `SpawnPiece(definition, tileState, isPlayerPiece, objectName)` 공용 메서드로 정리(중복 제거).
- `SpawnTestEnemy(definition, position)` / `SpawnTestEnemyPawn(position)` 추가 — 지정 좌표에 적 기물을 직접 배치하는 개발용 진입점. 이미 점유된 칸이면 실패(null) 반환.
- `BattleController.Awake()`에서 새 런을 만들 때 `SpawnTestEnemyPawn(_testEnemySpawnPosition)`(기본 좌표 (4,8))을 호출해 적 1기를 자동 배치.
- **이 스폰은 어디까지나 12~13일차 전투·승리 조건을 테스트하기 위한 임시 도구다.** 정식 적 배치(카드 기반 진영 구성, AI)는 91~105일차(적 AI·증원·보스) 단계에서 별도로 구현한다.

### 2. 전투 결과 구분 (`Battle/BattleOutcome.cs`, `Battle/TurnManager.cs`)
- `BattleOutcome { None, Victory, Defeat }` 추가.
- `TurnManager.EndBattle(BattleOutcome outcome = BattleOutcome.Defeat)`로 확장(`Outcome` 프로퍼티에 기록) — 인자 없는 기존 호출(`EndBattle()`)은 그대로 패배로 동작해 하위 호환 유지.
- `TurnStatusUI`가 전투 종료 시 `Outcome`에 따라 "N턴 · 승리" / "N턴 · 패배"를 구분해 표시.

### 3. 승리 조건 — 적 전멸 (`Board/BoardState.cs`, `Battle/BattleController.cs`)
- `BoardState.CountPieces(isPlayerPiece)` 추가 — 보드 위 아군/적군 기물 수를 센다.
- `BattleController.HandleAttackResolved`(12일차에 추가한 `AttackResolved` 구독)에 로직 추가: 적 기물이 죽었고 남은 적이 0이면 `EndBattle(BattleOutcome.Victory)`.

### 4. 라운드 턴 제한 — 테스트 값 30턴 (`Battle/BattleController.cs`)
- `_turnLimitTestValue = 30`([확정]이 아닌 [테스트 값], `Docs/CoreRules_Checklist.md` 기준) 추가.
- 더미 적 턴 종료(`CompleteDummyEnemyTurnAfterDelay`)에서 새 턴 번호가 제한을 넘으면 `EndBattle(BattleOutcome.Defeat)`.

### 5. 테스트
- `BoardStateTests`: `CountPieces`가 진영별 정확한 수를 반환하는지.
- `TurnManagerTests`: `EndBattle`에 전달한 결과가 그대로 기록되는지, 인자 없는 호출은 패배로 기본 처리되는지, 이미 종료된 전투는 결과가 덮어써지지 않는지.
- `AttackExecutionTests`: `SpawnTestEnemy`가 정상 배치/중복 점유 거부하는지, 마지막 적 처치 후 `CountPieces`가 0을 반환하는지(승리 판정이 의존하는 데이터).

## 확인된 흐름

```
새 런 시작 → BattleController가 적 영역에 테스트용 적 1기 자동 배치
    ↓
플레이어가 공격 → 적 처치 → 남은 적 0 → BattleOutcome.Victory → "N턴 · 승리"
    ↓ (또는)
30턴 초과(더미 적 턴 반복) → BattleOutcome.Defeat → "N턴 · 패배"
```

## 오늘 하지 않은 것 / 알려진 한계

- 정식 적 배치(카드/진영 구성)와 AI 행동 판단 — 여전히 더미 적 턴 + 고정 위치 테스트 스폰.
- 킹 HP·패배(12일차)와 적 전멸 승리(13일차)를 같은 판정에서 함께 확인하지만, 두 조건이 같은 프레임에 동시에 발생하는 극단적 케이스(예: 마지막 적을 죽였는데 그 반격으로 킹도 죽는 경우)는 아직 고려하지 않음 — 현재는 반격 시스템 자체가 없어(문서 확정: 기본 반격 없음) 실제로 발생하지 않는 상황.
- 킹 HP·승리/패배 전체 흐름(`BattleController.HandleAttackResolved`)은 씬 부트스트랩에 의존해 EditMode 테스트로 직접 검증하지 않음 — Play 모드 수동 확인으로 대체(기존 `TurnStatusUITests`와 같은 이유).

## 완료 기준 체크

- [x] 새 런 시작 시 적 기물이 자동으로 배치되어 전투를 바로 테스트할 수 있다.
- [x] 마지막 적을 처치하면 승리로 전투가 종료된다(코드 연결 완료).
- [x] 30턴을 넘기면 패배로 전투가 종료된다(코드 연결 완료).
- [x] `TurnStatusUI`가 승리/패배를 구분해 표시한다.
- [x] 관련 EditMode 테스트 작성.
- [ ] Unity 에디터에서 실제 컴파일 확인(Console Error 0) *(에디터 작업 필요)*
- [ ] Battle 씬 Play로 적 처치 → 승리, 30턴 초과 → 패배가 실제로 뜨는지 확인 *(에디터 작업 필요)*
- [ ] Test Runner에서 신규 테스트 통과 확인 *(에디터 작업 필요)*

## 남은 일 (사용자가 직접)

1. Unity 에디터를 열어 컴파일 에러가 없는지 확인.
2. Battle 씬 Play → 자동 배치된 적을 공격해 죽인 뒤 상단에 "N턴 · 승리"가 뜨는지 확인.
3. (선택) `_turnLimitTestValue`를 낮은 값(예: 2)으로 바꿔 턴 제한 패배가 실제로 뜨는지 확인 후 다시 30으로 되돌리기.
4. Test Runner(EditMode)에서 새 테스트가 통과하는지 확인.
5. 문제가 있으면 Console 오류를 기준으로 다음 턴에서 수정.
