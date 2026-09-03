# 20일차 개발 일지 — 규칙 확정 및 적 덱 시스템

**날짜**: 2026-09-03
**목표**: 17일차부터 실제로 구현돼 있던 "배치 턴 자유 배치"를 기획서 원문 대신 확정 규칙으로 공식 채택하고, 적에게도 플레이어와 동일한 보유 풀→드로우 더미→죽은 카드 더미 구조를 붙인 뒤 적 소환 위치를 무작위화한다.

## 오늘 한 일

### 1. 배치 수 제한 규칙 확정 (`Docs/CoreRules_Checklist.md`)
- 기획서 원문 "배치 턴당 기본 1개 기물만 신규 배치"를 폐기하고, 17일차부터 실제 구현된 **"시작 배치·주기 배치 턴 모두 자유 배치(장수 제한 없음)"**를 [확정] 규칙으로 갱신.
- 코드 변경 없음 — 문서를 실제 구현에 맞춤(사용자 확인).

### 2. 적 전용 DeckState 추가 (`Board/BoardInputController.cs`)
- `_enemyDeck`(`DeckState`) 필드 추가 — 플레이어의 `RunState.Deck`과 완전히 같은 구조(`OwnedCardPool`/`DrawPile`/`DeadCardPile`).
- `EnsurePrototypeEnemyStartingHand()`을 플레이어 시작 덱 구성과 동일한 패턴으로 재작성: 적 카드 5종(Pawn/Knight/Bishop/Rook/Queen)을 `_enemyDeck.OwnedCardPool`에 등록 → `RebuildDrawPileFromOwnedPool()`로 셔플 → 초기 손패 3장만 뽑고 나머지 2장은 드로우 더미에 남김.
- `TryEnemySummonOneCard()`: 손패가 비면 `_enemyDeck.TryDrawToHand()`로 자동 리필 후 소환 시도 — 드로우 더미까지 비어야 더미 적 턴(대기)으로 넘어감.
- `RemovePieceFromBoard`: 적 기물이 죽으면 `_enemyDeck.MoveToDeadPile(definition)`으로 이동(아군은 기존 19일차 로직 그대로 `RunState.Deck`으로).
- 디버그 한 줄 요약(`OnGUI`)에 `Dead`, `EnemyDraw`, `EnemyDead` 항목 추가.

### 3. 적 소환 위치 무작위화
- 기존 `FindFirstFreeEnemyPlacementTile`(후방부터 순서대로 첫 빈 칸)을 `FindRandomFreeEnemyPlacementTile`로 교체.
- 적 배치 영역(10×5)의 빈 칸을 모두 모은 뒤 `UnityEngine.Random.Range`로 하나를 무작위 선택.
- 매 호출마다 새 리스트를 할당하지 않도록 `_freeEnemyTileBuffer`를 재사용.

### 4. 테스트 (`Tests/EditMode/CardFlowTests.cs`)
- `EnsurePrototypeEnemyStartingHand_BuildsDeckAndInitialHand`: 적 보유 풀 5장/초기 손패 3장/드로우 더미 2장 구성 검증.
- `TryEnemySummonOneCard_RefillsHandFromDrawPile_WhenHandEmpty`: 초기 손패 3장을 모두 소환한 뒤에도 드로우 더미에서 자동 리필되어 계속 소환 가능함을 검증(4번째 소환까지 확인).
- `EnemyPieceDeath_MovesCardToEnemyDeadPile`: 적 기물이 죽으면 카드가 적 전용 죽은 카드 더미로 이동하고 아군 죽은 카드 더미에는 영향이 없는지 검증.

## 오늘 하지 않은 것

- 적 승리/라운드 클리어에 해당하는 개념이 없어 적 죽은 카드 더미를 다시 보유 풀로 복귀시키는 로직은 추가하지 않음(플레이어도 19일차에 "다음 라운드 전환" 자체가 없어 같은 상태).
- 적이 어떤 카드를 낼지 선택하는 로직은 여전히 "손패 첫 장 고정" — 소환 카드 자체의 무작위화나 우선순위 판단은 요청 범위 밖(위치만 무작위화).
- 적 손패 상한(플레이어처럼 10장 제한)은 별도로 걸지 않음 — 적 카드 풀이 5종뿐이라 실질적으로 초과할 일이 없음.

## 완료 기준 체크

- [x] `Docs/CoreRules_Checklist.md`가 실제 자유 배치 구현과 일치하도록 갱신.
- [x] 적이 플레이어와 동일한 보유 풀·드로우 더미·죽은 카드 더미 구조를 가진다.
- [x] 적 손패가 비면 드로우 더미에서 자동으로 리필된다.
- [x] 적 기물이 죽으면 카드가 적 전용 죽은 카드 더미로 이동한다.
- [x] 적 소환 위치가 고정 순서 대신 적 영역 내에서 무작위로 선택된다.
- [x] 관련 EditMode 테스트 작성.
- [x] Unity 에디터에서 실제 컴파일 확인(Console Error 0).
- [x] Battle 씬 Play로 적 소환 위치가 매번 달라지고, 여러 번의 적 턴 이후에도 계속 소환됨을 확인.
- [x] Test Runner에서 신규 테스트 통과 확인.

20일차 완료 기준을 모두 만족해 20일차를 종료한다.
