# 11일차 개발 일지 — 이동 실행 연결

**날짜**: 2026-09-03
**목표**: 7일차에 이미 구현해 둔 `MovementResolver`(기물별 이동/공격 후보 계산)를 실제 클릭 입력과 연결해, 보드 위 기물을 선택 → 후보 칸 확인 → 실제 이동까지 되게 한다. 10일차의 Space 키 임시 행동 완료 대신, 실제 이동을 플레이어의 턴 행동으로 사용한다.

## 오늘 한 일

### 1. 이동/공격 후보 시각 강조 (`Board/BoardView.cs`)
- 서브메시를 4개(밝은 칸/어두운 칸/설치가능/설치불가)에서 6개로 확장해 이동 가능(초록)·공격 가능(주황) 강조 색을 추가.
- `HighlightMoveCandidates(moveTiles, attackTiles)` / `ClearMoveCandidates()` 추가 — 여러 칸을 동시에 강조·해제.
- 기존 단일 칸 강조(`HighlightCell`/카드 배치용)와 새 다중 칸 강조가 동시에 표시되지 않도록 서로 진입 시 상대방을 먼저 해제하도록 연결.

### 2. 기물 선택·이동 실행 (`Board/BoardInputController.cs`)
- `TrySelectPieceAt(cell)`: 클릭한 칸에 내 기물이 있으면 선택하고 `MovementResolver.GetReachableTiles(...)`로 이동/공격 후보를 계산해 강조.
- `TryMoveSelectedPieceTo(destination)`: 선택된 기물을 이동 후보 칸으로 옮김 — 원래 칸 점유 해제, `PieceRuntimeState.BoardPosition` 갱신, 새 칸 점유, 연결된 `PieceView`도 같은 좌표로 이동.
- 이동 완료 시 `TurnManager.TryCompletePlayerAction()`을 직접 호출해 실제 이동이 턴을 소비하도록 연결(10일차 Space 키는 폴백으로 남김).
- 공격 후보 칸 클릭은 감지만 하고(`HandleAttackCandidateClick`) 실제 피해 계산은 아직 붙이지 않음 — 턴도 소비하지 않음.
- 카드 선택, 보드 밖 클릭, 적 턴 전환 시 진행 중이던 기물 선택도 함께 해제되도록 정리.
- 소환된 기물마다 `PieceRuntimeState → PieceView` 매핑을 `Dictionary`로 관리해 이후 이동 시 같은 화면 오브젝트를 옮길 수 있게 함.

### 3. `PieceView` 이동 지원 (`Pieces/PieceView.cs`)
- `MoveTo(boardPosition, tileSize)` 추가 — 최초 배치(`Initialize`)와 이동(`MoveTo`)이 같은 위치 계산 로직(`ApplyBoardPosition`)을 공유하도록 정리.

### 4. 테스트 (`Tests/EditMode/PieceMovementExecutionTests.cs`)
- 내 기물 선택 시 이동/공격 후보가 계산되는지 확인.
- 후보 칸으로 이동하면 보드 점유·좌표·`PieceView` 연결이 갱신되고 턴이 `EnemyTurn`으로 넘어가는지 확인.
- 후보가 아닌 칸으로는 이동이 거부되고 턴도 그대로인지 확인.
- 적 턴 중에는 기물 선택 자체가 거부되는지 확인.

## 확인된 흐름

```
내 기물 클릭 → 이동(초록)/공격(주황) 후보 강조
    ↓ 이동 후보 클릭
기물 이동 + 화면 위치 갱신 → 선택 해제 → TurnManager가 적 턴으로 전환
```

## 오늘 하지 않은 것 / 알려진 한계

- 공격 후보 칸 클릭 시 실제 HP·ATK 피해 계산은 아직 없음(감지만 함, 턴도 소비하지 않음) — 다음 순서(문서 24~26일차: 비치명/치명 공격, 칸 점유).
- 이동 연출은 즉시 이동(트윈/애니메이션 없음).
- 여전히 더미 적 턴(실제 AI 없음).

### 5. 에디터 검증 (사용자 확인)
- Unity 에디터에서 컴파일 에러 없이 새 스크립트·테스트가 인식됨을 확인.
- 새로 생성된 `.meta` 파일 반영.

## 완료 기준 체크

- [x] 보드 위 내 기물을 클릭하면 이동/공격 후보 칸이 강조된다.
- [x] 이동 후보 칸을 클릭하면 기물이 실제로 이동하고 화면 위치도 갱신된다.
- [x] 이동이 플레이어의 턴 행동으로 처리되어 자동으로 적 턴으로 전환된다.
- [x] 후보가 아닌 칸 클릭은 거부된다.
- [x] 관련 EditMode 테스트 작성.
- [x] Unity 에디터에서 실제 컴파일 확인(Console Error 0)

11일차 완료 기준을 모두 만족해 11일차를 종료한다.
