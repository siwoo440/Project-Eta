# 19일차 개발 일지 — 카드 생명주기 및 덱/무덤 패널 UI

**날짜**: 2026-09-03
**목표**: 사망 기물의 카드를 죽은 카드 더미로 보내고, 배치 턴에 손패를 정리하며, 승리 시 죽은 카드가 보유 풀로 복귀하는 카드 생명주기를 마무리한다. 추가로 좌하단 "뽑을 카드 덱" / 우하단 "죽은 카드 덱" 버튼과, 누르면 뜨는 가로 5장 스크롤 패널 UI를 만든다.

## 오늘 한 일

### 1. 손패 정리 (문서 41일차)
- `DeckState.DiscardToBottom(card, hand)` 추가 — 손패에서 카드를 제거하고 드로우 더미 맨 아래(리스트 0번 인덱스, `TryDraw`가 맨 위로 쓰는 마지막 인덱스와 반대쪽)에 삽입.
- `BoardInputController.TryDiscardHandCardToBottom(card)` 추가 — 배치 턴(`CanUseDeploymentInput`)에서만 허용, 무료.
- `CardView`에 `IPointerClickHandler` 추가 — 카드 우클릭 시 `HandUI.TryDiscardCard`를 거쳐 실제 정리 실행.
- 배치 턴 조작 안내 문구에 "카드 우클릭 = 손패 정리" 추가.

### 2. 사망 기물 → 죽은 카드 더미 (문서 42일차)
- `BoardInputController.RemovePieceFromBoard`에서 제거되는 기물이 아군(`IsPlayerPiece`)이면 `RunState.Deck.MoveToDeadPile(definition)` 호출.
- 적은 아직 자체 `DeckState`가 없어(17일차부터 유지된 설계) 적용 대상에서 제외.
- 참고: 현재 적 AI는 카드 자동 소환만 하고 공격은 하지 않아, 이 경로가 실제로 아군 기물을 죽이는 상황은 아직 발생하지 않는다(적 공격 AI는 91~105일차 몫). 메커니즘 자체는 미리 연결해 뒀다.

### 3. 라운드 클리어 시 카드 복귀 (문서 43일차, 축소 범위)
- `BoardInputController.ReturnDeadPileToOwnedPool()` 추가 — 내부적으로 `DeckState.ReturnDeadPileToOwnedPool()`을 호출하고 `DeckChanged` 이벤트를 알림.
- `BattleController.HandleAttackResolved`에서 적 전멸로 `Victory` 처리하기 직전에 호출.
- "다음 라운드로 실제 전환"하는 시스템은 여전히 없음 — 지난 13~15일차 논의대로 범위 밖으로 유지.

### 4. 덱/무덤 버튼 및 카드 목록 패널 (신규 요청)
- `Scripts/UI/DeckPanelUI.cs` 추가 — `HandUI`와 같은 방식으로 `BoardInputController`에 `Bind`.
- 화면 좌하단에 "뽑을 카드 N장" 버튼, 우하단에 "죽은 카드 N장" 버튼을 런타임 생성. `DeckChanged` 이벤트로 장수 실시간 갱신.
- 버튼을 누르면 전체 화면 반투명 배경 + 중앙 패널이 열리고, `ScrollRect` + `GridLayoutGroup`(고정 5열)으로 카드를 가로 5장씩 배치, 세로로만 스크롤.
- 카드 썸네일은 초상화(Artwork 또는 이동 타입 약칭) + 이름 + "N성 · ATK n · HP n" 요약으로 구성(정식 손패 카드보다 단순한 형태).
- 배경 클릭 또는 우상단 "X" 버튼으로 닫기. 같은 버튼을 다시 누르면 토글로 닫힘.
- `BattleController`에 `EnsureDeckPanelUI()` 추가, `BindState()`에서 `EnsureHandUI()` 다음으로 호출.

### 5. 테스트
- `Tests/EditMode/DeckStateTests.cs`(신규): `DiscardToBottom`이 손패→드로우 더미 맨 아래로 정확히 이동하는지(드로우 순서까지 확인), 손패에 없는 카드는 실패하는지, `ReturnDeadPileToOwnedPool`이 정확히 동작하는지.
- `Tests/EditMode/CardFlowTests.cs`(추가): 배치 턴 손패 정리 성공, 일반 PlayerTurn에는 손패 정리가 거부되는지, `BoardInputController.ReturnDeadPileToOwnedPool()`이 실제로 보유 풀에 카드를 되돌리는지.
- 컴파일 에러 수정: `CardFlowTests.cs`에 `IReadOnlyList<PieceDefinition>.Contains()` 확장 메서드에 필요한 `using System.Linq;` 누락 추가.

### 6. Play 테스트 중 발견된 버그 2건 수정
- **`Screen position out of view frustum`(NaN 마우스 좌표)**: 좌하단/우하단 덱 버튼처럼 화면 UI를 클릭해도 `BoardInputController.Update()`가 같은 클릭을 3D 보드로도 흘려보내고 있었음. `EventSystem.current.IsPointerOverGameObject()`로 UI 클릭이면 보드 클릭 처리를 건너뛰도록 수정하고, `HandleBoardClick()`/`TryGetBoardCellFromScreenPoint()`에 NaN 좌표 방어 코드 추가.
- **`MissingComponentException: CanvasGroup`**: `GetComponent<T>() ?? AddComponent<T>()` 패턴이 Unity의 "가짜 null"(파괴됐지만 C# 참조는 남아있는 컴포넌트)을 걸러내지 못하는 알려진 함정이었음. `CardView.cs`의 `EnsureRequiredComponents()`/`EnsureVisualTree()`에서 `??` 대신 `if (x == null) x = ...` 명시적 null 비교로 교체.

## 오늘 하지 않은 것

- 적 AI의 실제 공격(따라서 아군 카드가 죽은 카드 더미로 가는 상황은 아직 플레이로는 재현 불가) — 91~105일차.
- 적 전용 `DeckState`/카드 생명주기 — 계속 보류.
- 다음 라운드로의 실제 전환(보드 재구성, 새 적 배치) — 8단계(로그라이트 런) 몫.
- 덱/무덤 패널 카드 썸네일의 정식 카드 프레임 디자인(현재는 손패 카드보다 단순한 버전).

## 완료 기준 체크

- [x] 배치 턴에 카드를 우클릭하면 손패에서 빠지고 드로우 더미 맨 아래로 이동한다.
- [x] 일반 PlayerTurn에는 손패 정리가 거부된다.
- [x] 아군 기물이 죽으면(메커니즘상) 카드가 죽은 카드 더미로 이동한다.
- [x] 승리 시 죽은 카드 더미가 보유 풀로 복귀한다.
- [x] 좌하단/우하단에 드로우·죽은 카드 더미 버튼이 표시되고 장수가 실시간 갱신된다.
- [x] 버튼을 누르면 가로 5장 그리드, 세로 스크롤 패널이 뜬다.
- [x] 관련 EditMode 테스트 작성.
- [x] Unity 에디터에서 실제 컴파일 확인(Console Error 0).
- [x] Battle 씬 Play로 손패 정리(우클릭), 덱/무덤 버튼과 패널 스크롤 동작 확인, Play 중 발견된 NaN 클릭·CanvasGroup 버그까지 수정 완료.
- [x] Test Runner에서 신규 테스트 통과 확인.

19일차 완료 기준을 모두 만족해 19일차를 종료한다.
