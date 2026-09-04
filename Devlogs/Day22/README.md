# 22일차 개발 일지 — 합성 등급 규칙·수량 제한·숨김 합성식 + 9종 통합 회귀 테스트

**날짜**: 2026-09-04
**목표**: 문서 49~60일차(원래 22·23일차로 나눠져 있던 범위)를 한 일차로 통합해, 등급을 데이터가 아닌 실제 규칙으로 승격시키고 4단계를 마무리한다.

## 일정 재조정 배경

21일차 작업이 문서 23일차 범위를 상당 부분 앞당겨 처리한 상태였습니다. 착수 전에 겹침을 확인한 결과:

- **문서 23일차 "Archbishop/Chancellor/Amazon 3종 레시피 실전 연결"** — 21일차에 이미 완료(3종 `PieceDefinition` + 3종 `FusionRecipe` + `FusionRecipeDatabase` + `PieceDatabase` 등록).
- **문서 23일차 "배치 턴에서 합성 결과를 같은 턴에 바로 배치"** — 21일차에 이미 동작(결과 카드가 손패에 들어가는 즉시 드래그 소환 가능). 이번 일차에는 명시적 테스트만 추가.
- **문서 22일차 "합성 미리보기"** — 21일차에 결과 슬롯(이름·등급·ATK·HP·설명)까지 완료. 이번 일차에는 재료 대비 증감 비교만 보강.
- **문서 22일차 "동일 카드 특수 레시피"** — `HasCardsAvailableForFusion`이 동일 카드 2장 보유 판정을 이미 처리 중. 예외 규칙과 데이터만 추가.

남은 실작업이 2일치가 되지 않아 사용자 확인 후 **22·23일차를 22일차 하나로 통합**하고 다음 일차부터 5단계(기물 확장 및 페어리 체스 규칙)로 넘어가기로 했습니다.

## Amazon 등급 처리 (A안)

Amazon이 `_grade: 5`인데 Queen(1성) + Knight(1성)에서 나오고 있어, 등급 규칙을 켜는 순간 4단계 점프로 차단되는 상태였습니다. 두 가지 선택지 중 사용자가 **(A) Amazon을 2성으로 강등**을 선택했습니다.

- `Amazon.asset`: `_grade` 5 → 2, `_baseHp` 5 → 4, `_baseAtk` 5 → 4 (Archbishop 3/3, Chancellor 4/3 사이에서 가장 강한 2성 위치).
- 5성 최종 합성 체인은 5단계 페어리 기물 26종 확장 때 중간 등급 기물과 함께 재설계 예정. 지금 중간 기물을 만들면 5단계에서 다시 갈아엎게 되므로 미룸.

## 오늘 한 일

### 1. 합성 규칙 검증 계층 신설
- `Scripts/Fusion/FusionBlockReason.cs` 신규 — 합성이 막힌 구체적 사유를 UI·테스트에 그대로 전달하는 열거형(턴 위반, 조합 없음, 재료 분류 위반, 등급 점프, 손패 부족, 수량 제한 등 8종).
- `Scripts/Fusion/FusionRuleValidator.cs` 신규 — 기획서 5.7 규칙을 한 곳에 모은 정적 검증기.
  - `IsGradeStepValid`: 결과 등급 == 재료 최고 등급 + 1. **2단계 이상 점프 차단.**
  - `IsFusableMaterial`: `Basic`·`Fusion` 분류만 재료로 허용(King은 `Special`이라 제외).
  - `GetOwnedLimit`: 4성 2개, 5성 1개(기획서 "5성은 동일 최상위 기물 1개" 기본안), 1~3성 무제한.
  - `DescribeBlockReason`: 차단 사유를 합성 패널에 그대로 띄울 한글 문구로 변환.

### 2. 동일 카드 특수 레시피 예외
- `FusionRecipe`에 `_ignoresGradeStepRule` 추가 — 동일 카드 특수 레시피처럼 등급 규칙을 데이터에서 명시적으로 예외 처리할 수 있는 escape hatch. `UsesIdenticalMaterials` 프로퍼티로 동일 카드 레시피 여부도 판별 가능.
- 현재 출하 데이터 중 이 플래그를 켠 레시피는 없음(전부 규칙 준수). 예외 경로는 EditMode 테스트로만 검증.
- 동일 카드 레시피 데이터 1종 추가: `Recipe_AmazonFromQueenPair` (Queen + Queen → Amazon, 숨김 레시피).

### 3. 숨김 합성식 발견 기록
- `Scripts/Fusion/FusionDiscoveryLog.cs` 신규 — 런 단위 발견 기록(`IsDiscovered`/`TryMarkDiscovered`/`Restore`).
- `FusionRecipe._recipeId` 추가 — 저장 파일에 남길 안정적인 식별자(비어 있으면 에셋 이름 사용). 기존 3종 레시피에도 id 부여.
- `RunState.FusionDiscovery` 추가, `RunSaveData.discoveredRecipeIds`로 영속화. 목록이 없는 구버전 저장 파일도 안전하게 복원.
- `BoardInputController.HiddenRecipeDiscovered` 이벤트 추가 — 발견 시 콘솔 로그 + `FusionPanelUI`에 3초간 금색 알림 표시.
- 발견 전 숨김 레시피는 미리보기에서 이름·스탯·초상화를 전부 가리고 "???" / "숨김 합성식"으로 표시(합성 자체는 가능).

### 4. 4·5성 보유·배치 수량 제한
- `RunState.CountOwnedCopies` — 영구 마스터 목록인 `Deck.OwnedCardPool` 기준 동일 기물 보유 수(아래 11-(2) 참고).
- `RunState.CountDeployedCopies` — 보드 위 아군 기물 중 동일 기물 배치 수.
- 합성 시 결과 기물이 보유 상한에 도달했으면 `OwnedLimitReached`로 차단(미리보기 단계에서 이미 확정 버튼 비활성 + 사유 표시).
- `BoardInputController.IsWithinDeployLimit` 추가, `CanSummonCard`에 연결 — 4·5성은 보드 동시 배치 수도 같은 상한으로 제한(드래그 자체가 막힘).

### 5. 합성 판정 통합 진입점
- `BoardInputController.EvaluateFusion(materialA, materialB, out recipe)` 신규 — 턴·레시피·재료 분류·등급·손패 보유·수량 제한을 한 번에 판정해 `FusionBlockReason`을 반환.
- `TryFuseCards`와 `RecomputeFusionPreview`가 모두 이 진입점 하나를 사용하도록 정리. **미리보기와 실제 합성이 서로 다른 규칙으로 갈라질 수 없는 구조**가 됨.
- 규칙을 위반한 레시피는 미리보기 자체가 `null`이 되어 결과가 노출되지 않음.

### 6. 합성 미리보기 비교 표기 보강
- 결과 스탯을 재료 최고치와 비교해 `1성 → 2성 · ATK 4 (+1) · HP 4 (+1)` 형태로 표시.
- 결과가 없을 때 기존의 뭉뚱그린 "합성 가능한 조합이 아닙니다" 대신 **구체적 사유**를 표시(등급 위반, 수량 제한, 손패 부족 등).

### 7. 기본 6종 데이터 정리
- `King.asset`: `_category` `Basic` → `Special`. King은 합성 재료로도 결과로도 쓰이지 않는 특수 기물임을 데이터에 명시.
- 나머지 5종은 `Basic` / 1성 / 1×1 점유로 기준선 확정, 회귀 테스트로 고정.

### 8. 9종 통합 회귀 테스트 (`Tests/EditMode/PieceRosterRegressionTests.cs`, 신규)
실제 프로젝트 에셋을 `AssetDatabase`로 직접 불러와 검증합니다.

- 기본 6종의 PieceId·등급·스탯·점유 크기 검증.
- King만 합성 재료에서 제외되고 나머지 5종은 재료로 사용 가능한지 검증.
- 합성 3종이 `Fusion` 분류 / 2성인지 검증.
- **Data 폴더에 등록된 모든 레시피가 등급·재료 규칙을 만족하는지 검증** — 앞으로 규칙을 어긴 레시피 에셋을 추가하면 테스트가 즉시 실패.
- 레시피 데이터베이스로 3종 합성 결과가 모두 만들어지는지 검증.
- 9종 전부가 자기 이동 규칙으로 빈 보드에서 이동 후보를 계산하고, 보드 밖·제자리 좌표가 섞이지 않는지 검증.
- 합성형 3종의 이동 후보가 재료 기물(Bishop+Knight / Rook+Knight / Queen+Knight)의 이동 후보를 **모두 포함**하는지 검증.
- 9종 전부가 배치 → 사망 → 죽은 카드 → 라운드 종료 복귀까지 카드가 유실되지 않고 순환하는지 검증.
- 9종 전부가 저장·복원 후 종류·체력·진영을 유지하는지 검증.

### 9. 합성 규칙 EditMode 테스트 (`Tests/EditMode/FusionTests.cs` 확장)
- 등급 한 단계 상승은 허용 / 2단계 점프는 `GradeStepViolation`으로 차단.
- 동일 카드 + 예외 플래그 레시피는 등급 점프 허용.
- King 분류 재료는 `MaterialNotFusable`로 차단.
- 4·5성 보유 상한이 기획서 기본안(4성 2개 / 5성 1개)과 일치.
- 5성 결과를 이미 1개 보유하면 추가 합성이 `OwnedLimitReached`로 차단되고 손패가 그대로 유지.
- 숨김 레시피는 발견 전 결과가 가려지고, 합성 성공 시 발견 기록 + 이벤트가 발생.
- 숨김 합성식 발견 기록이 저장·복원을 거쳐도 유지.
- 기존 21일차 테스트의 `CreateContext`도 등급을 명시(1성 + 1성 → 2성)하도록 갱신 — 새 등급 규칙을 통과하기 위함.

### 10. 재료 슬롯 클릭으로 개별 제외 (추가 요청)

재료를 바꿔가며 결과를 비교할 때, 기존에는 손패에서 해당 카드를 다시 찾아 클릭해야 재료가 빠졌습니다. 합성 패널의 재료 슬롯을 직접 눌러 그 자리만 비울 수 있도록 추가했습니다.

- `BuildSlot`이 슬롯 배경에 `Button`을 붙이고 색상 전환(마우스 오버 시 밝게, 누르면 진하게)을 적용.
- 슬롯 클릭 → `OnMaterialSlotClicked(slotIndex)` → 그 자리의 재료를 `TryToggleFusionMaterial`로 다시 토글(= 선택 해제). 손패 카드의 금색 강조도 같은 `FusionSelectionChanged` 이벤트로 함께 풀림.
- 재료가 든 슬롯에만 **"클릭하여 제외"** 안내 문구를 표시하고, 빈 슬롯은 `interactable = false`로 반응하지 않음.
- 이름·안내 텍스트는 `raycastTarget = false`로 두어 슬롯 클릭을 가로채지 않도록 처리.
- 빈 자리에는 손패에서 다른 카드를 바로 넣을 수 있어, **A 고정 + B만 교체하며 결과 비교**가 가능해짐.
- EditMode 테스트 `ToggleFusionMaterial_RemovesOnlyThatSlot_AndKeepsTheOther` 추가 — 한 장만 빠지고 나머지가 유지되는지, 미리보기와 차단 사유가 갱신되는지, 뺀 카드가 손패에 남아 있는지, 빈 자리에 새 카드를 넣을 수 있는지 검증.

### 11. Test Runner 실패 8건 정리

전체 EditMode 스위트를 처음 돌려 실패 8건을 확인하고 전부 수정했습니다.

**(1) 17일차 이후 방치된 이동·공격 테스트 7건**
`PieceMovementExecutionTests` 3건, `AttackExecutionTests` 4건이 실패하고 있었습니다. 원인은 22일차 작업과 무관합니다 — 두 파일은 15일차 이후 손대지 않았는데, **17일차에 "자유 배치 턴"이 도입되면서 시작 배치 턴이 킹 배치 + 명시적 종료 없이는 끝나지 않도록 바뀌었기 때문**입니다. 두 파일의 `CreateBoundContext`가 `TurnManager`를 만든 뒤 배치 턴을 벗어나지 않아, 일반 턴 전용인 이동·공격이 전부 거부되고 있었습니다.

두 파일의 `CreateBoundContext`에 `MarkInitialKingPlaced()` + `TryEndDeploymentTurn()`을 추가해 `PlayerTurn`에서 시작하도록 수정했습니다. 부수 효과로 `TrySelectPieceAt_Fails_DuringEnemyTurn`도 의도대로 동작하게 됐습니다 — 기존에는 `TryCompletePlayerAction()`이 배치 턴이라 실패해서, "적 턴이라 선택 불가"가 아니라 "배치 턴이라 선택 불가"로 통과하던 상태였습니다.

**(2) `AllNinePieces_SurviveFullCardLifecycle` 1건 — 제 코드 문제**
`CountOwnedCopies`가 손패·보유 풀·드로우 더미·죽은 카드·보드를 전부 합산했는데, `RebuildDrawPileFromOwnedPool`이 보유 풀을 **이동이 아니라 복사**하므로 같은 카드가 중복 집계됐습니다(King 1장이 2장으로 계산).

`DeckState`의 실제 규약은 **`OwnedCardPool`이 영구 마스터 목록**이고 드로우 더미·손패·보드·무덤은 라운드 내 위치입니다. 이에 맞춰:
- `CountOwnedCopies`를 `OwnedCardPool` 단독 집계로 변경.
- 합성이 이 규약을 지키도록 `DeckState.RemoveFromOwnedPool` 추가 후, `TryFuseCards`에서 **재료 2장을 보유 풀에서 제거하고 결과 1장을 보유 풀에 등록**하도록 수정. 이 처리가 없으면 합성해도 재료가 보유 풀에 남아 다음 라운드에 되살아나고, 합성 결과는 보유 풀에 없어 수량 제한 집계에서 누락됩니다.
- 회귀 테스트도 실제 규약에 맞게 정리(사망 시 보유 수 불변 → 라운드 종료 후 보유 풀에 잔존).

## 발견했지만 고치지 않은 것 — 라운드 종료 카드 중복

`DeckState.MoveToDeadPile`은 `OwnedCardPool`에서 카드를 빼지 않는데 `ReturnDeadPileToOwnedPool`은 `_ownedCardPool.AddRange(_deadCardPile)`로 다시 더합니다. 즉 **아군 기물이 죽고 라운드가 끝날 때마다 보유 카드가 1장씩 늘어납니다.**

`CardFlowTests.ReturnDeadPileToOwnedPool_ReturnsDeadCardsToOwnedPool`이 `ownedCountBefore + 1`을 명시적으로 기대하고 있어, 현재 동작이 테스트로 고정된 상태입니다. 19일차 카드 생명주기 규약을 다시 정하는 작업이라 22일차 범위를 넘어선다고 판단해 손대지 않았습니다. `MoveToDeadPile`이 보유 풀에서 카드를 빼도록 바꾸고 해당 테스트를 함께 수정하면 해결됩니다 — **별도 버그 수정 일차로 처리 권장**.

## 오늘 하지 않은 것

- 5성 최종 합성 체인과 중간 등급 기물 설계 — 5단계에서 페어리 기물 확장과 함께 진행.
- 숨김 합성식 도감 UI(발견 목록 조회 화면) — 발견 기록·알림까지만 구현. 도감 화면은 9단계 UI/UX 범위.
- 라운드 보상으로 얻는 4·5성 카드의 수량 제한 처리 — 보상 시스템 자체가 8단계 범위.

## 완료 기준 체크

- [x] Unity 에디터에서 컴파일 확인(Console Error 0) — 작업 중에도 Unity Roslyn(`csc`)으로 `ProjectEta.Runtime`·`ProjectEta.Tests.EditMode` 두 어셈블리를 직접 빌드해 에러 0개를 상시 확인
- [x] Test Runner EditMode 전체 통과 확인 — 실패 8건을 수정한 뒤 사용자가 재실행해 이상 없음 확인
- [x] Battle 씬 Play로 숨김 합성식 발견 알림·증감 표기·King 재료 차단·슬롯 클릭 제외 확인
- [x] 등급 2단계 점프 차단 규칙 구현·테스트
- [x] 동일 카드 특수 레시피 예외 처리 구현·테스트
- [x] 숨김 레시피 발견 기록·알림·저장 구현·테스트
- [x] 4·5성 보유/배치 수량 제한 구현·테스트
- [x] 기본 6종 데이터 정리(King Special 분류, 기준선 고정)
- [x] Amazon 2성 강등 및 스탯 조정
- [x] 9종 통합 회귀 테스트 작성
- [x] 재료 슬롯 클릭으로 개별 제외 구현·테스트

## 다음 일차

23일차부터 **5단계. 기물 확장 및 페어리 체스 규칙**(문서 61~75일차)으로 진입합니다. 첫 작업은 이동 규칙 모듈화 — `MovementResolver`의 `switch` 분기를 Step·Slide·Leap·Compound·Conditional 패턴으로 분리해, 새 기물을 데이터 조합만으로 추가할 수 있는 구조로 바꾸는 것입니다.
