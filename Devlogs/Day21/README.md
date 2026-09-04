# 21일차 개발 일지 — 합성 시스템 핵심 순환 + 합성 UI

**날짜**: 2026-09-03
**목표**: `FusionRecipe`를 실전 조회 가능하게 만들고, 배치 턴에 "합성" 버튼을 눌러 재료 카드 2장을 선택하면 중간에 결과 카드의 상세 정보가 미리 표시되고, 다시 "합성" 버튼으로 확정해 결과 카드를 손패로 받는 흐름을 구현한다.

## 작업 중 발견한 것 — 동시 편집

작업 도중 `BoardInputController.cs`가 이미(제가 손대기 전부터) 수정돼 있는 것을 발견했습니다. 확인해보니 **다른 세션이 같은 21일차 합성 백엔드**(`_fusionRecipeDatabase` 필드, `TryFindFusionRecipe`, `TryFuseCards`, `HasCardsAvailableForFusion`)를 동시에 구현하고 있었습니다. 사용자 확인 후 "그대로 계속 진행"하기로 하여, **그 세션이 만든 핵심 합성 실행 로직은 그대로 두고 그 위에 재료 선택 모드·미리보기·UI 레이어만 추가**하는 방식으로 진행했습니다(같은 메서드를 다시 만들지 않고 `TryFuseCards`를 내부적으로 호출).

## 오늘 한 일

### 1. 합성 레시피 데이터베이스
- `Scripts/Fusion/FusionRecipeDatabase.cs`(다른 세션이 생성) — 재료 2장(순서 무관)으로 등록된 `FusionRecipe`를 찾는 `TryFindRecipe`.
- 실제 데이터 자산 3종 생성: `Recipe_ArchbishopFromBishopKnight`, `Recipe_ChancellorFromRookKnight`, `Recipe_AmazonFromQueenKnight` + `FusionRecipeDatabase.asset`(3개 등록) — Battle 씬의 `BoardInputController._fusionRecipeDatabase`에 연결.
- 합성 결과 기물 3종도 함께 생성(문서 23일차 데이터를 앞당김): `Archbishop`(Bishop+Knight, 2성), `Chancellor`(Rook+Knight, 2성), `Amazon`(Queen+Knight, 5성). 이동 규칙은 7일차에 이미 완성된 `MovementResolver`의 Archbishop/Chancellor/Amazon 분기를 그대로 사용. `PieceDatabase`에도 등록.

### 2. 재료 선택 모드 (제가 추가)
- `BoardInputController`에 `IsFusionModeActive`, `FusionMaterials`(최대 2장), `CurrentFusionRecipe` 상태와 `SetFusionModeActive`/`TryToggleFusionMaterial`/`TryConfirmFusionSelection`/`FusionSelectionChanged` 이벤트 추가.
- 합성 모드는 배치 턴에서만 켤 수 있고, 배치 턴을 벗어나면 `Update()`에서 자동으로 꺼짐.
- 합성 모드 중에는 카드 좌클릭이 드래그 소환 대신 재료 선택/해제로 동작(`CardView.OnBeginDrag`가 `HandUI.IsFusionModeActive`일 때 드래그를 시작하지 않도록 가드).
- 재료로 선택된 카드는 금색 테두리 오버레이로 강조(`CardView.SetFusionSelected`).

### 3. 합성 UI
- `Scripts/UI/FusionPanelUI.cs` 신규 — 손패 위쪽에 "합성" 토글 버튼, 누르면 재료 A/B 슬롯 + "→" + 결과 미리보기 슬롯(이름·등급·ATK·HP·설명) + "합성"/"취소" 버튼이 있는 패널이 뜸.
- `DeckPanelUI`와 달리 전체 화면을 가리는 배경이 없음 — 손패 카드를 계속 클릭해서 재료를 고를 수 있어야 하기 때문.
- 재료가 덜 모였거나 매칭되는 레시피가 없으면 "합성" 확정 버튼이 비활성화되고 안내 문구가 표시됨.
- 확정 후에도 합성 모드 자체는 유지돼 같은 배치 턴에 연속으로 합성 가능("취소" 버튼으로 모드 종료).
- `BattleController.EnsureFusionPanelUI()` 추가, `BindState()`에서 `EnsureDeckPanelUI()` 다음으로 호출.

### 4. 테스트 (`Tests/EditMode/FusionTests.cs`, 신규)
- `FusionRecipeDatabase.TryFindRecipe`가 재료 순서와 무관하게 매칭되는지, 미등록 조합은 실패하는지.
- 배치 턴이 아니면 합성 모드 진입이 거부되는지.
- 재료 2장을 선택하면 미리보기가 계산되고, 확정하면 손패에서 재료가 빠지고 결과가 들어오며 재료 선택만 비워지고 합성 모드는 유지되는지.
- 매칭 레시피가 없으면 확정이 거부되고 손패가 그대로인지.

## 오늘 하지 않은 것

- 등급 상승 검증(2단계 점프 차단), 동일 카드 특수 레시피 예외, 숨김 레시피 발견 기록, 4·5성 보유·배치 제한 — 22일차.
- 합성 결과를 같은 배치 턴에 바로 배치하는 것은 이미 됨(손패에 들어오는 즉시 드래그 가능하므로 39/59일차 요구사항이 자연스럽게 만족됨).
- Archbishop/Chancellor/Amazon을 시작 손패나 적 손패에 자동으로 넣는 것은 하지 않음 — 오직 합성으로만 얻을 수 있음(기획서 취지에 맞음).

## 완료 기준 체크

- [x] 재료 2장(순서 무관)으로 레시피 조회.
- [x] 배치 턴에 "합성" 버튼으로 재료 선택 모드 진입.
- [x] 재료 선택 중 결과 카드 상세 정보(이름·등급·ATK·HP·설명) 미리 표시.
- [x] "합성" 버튼으로 확정 시 재료 소모 + 결과 카드 획득.
- [x] 관련 EditMode 테스트 작성.
- [x] Unity 에디터에서 실제 컴파일 확인(Console Error 0) — 동시 편집으로 인한 `CreatePanel` 튜플(`(GameObject, RectTransform, Image)`)에 없는 `.transform` 참조, `HandUI`/`FusionTests`의 `IReadOnlyList<PieceDefinition>.Contains` 확장 메서드 누락(`System.Linq`) 등 컴파일 에러를 사용자 피드백을 받아 수정 완료.
- [x] Battle 씬 Play로 실제 손패에서 Bishop+Knight 등을 합성해 결과를 얻는지 확인.
- [x] Test Runner에서 신규 테스트 통과 확인.

## 확인 후 수정한 컴파일 에러

- `FusionPanelUI.cs`: `CreatePanel`이 반환하는 튜플 `(GameObject gameObject, RectTransform rect, Image image)`에는 `transform` 필드가 없는데 `body.transform`/`slot.transform`으로 참조하던 부분을 전부 `body.rect`/`slot.rect`(RectTransform은 Transform이므로 그대로 사용 가능)로 수정.
- `HandUI.cs`, `Tests/EditMode/FusionTests.cs`: `IReadOnlyList<PieceDefinition>.Contains(...)` 확장 메서드를 쓰기 위해 `using System.Linq;` 추가.

## 결과

사용자가 Unity 에디터에서 컴파일·Play 모드·Test Runner를 모두 확인했고 문제가 없어 이번 일차 작업을 커밋합니다.
