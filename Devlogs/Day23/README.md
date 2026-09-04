# 23일차 개발 일지 — 데이터 기반 이동 규칙 모듈화 및 Wazir 추가

**날짜**: 2026-09-04  
**목표**: 기존 `PieceMovementType` 중심 이동 계산을 재사용 가능한 이동 규칙 객체 구조로 분리하고, 신규 기물을 전용 enum/switch 추가 없이 `PieceDefinition` 데이터만으로 확장할 수 있는 기반을 만든다. 동시에 사망 카드가 라운드 종료 시 중복 복귀하는 문제를 수정한다.

## 오늘 한 일

### 1. MovementResolver를 파사드 구조로 단순화
- 기존 `MovementResolver` 내부의 기물별 대형 switch와 직접 이동 계산 코드를 분리.
- 외부에서는 기존과 동일한 `MovementResolver.GetReachableTiles(...)` 진입점을 유지.
- 기존 `PieceMovementType` 기반 오버로드는 하위 호환용으로 유지.
- 신규 기물용으로 `PieceDefinition`을 직접 받는 데이터 기반 오버로드 추가.
- 실제 이동 계산은 `MovementRuleFactory`가 생성한 이동 규칙 객체에 위임하도록 변경.

### 2. 데이터 기반 이동 규칙 구조 추가
다음 규칙 구조를 새로 추가했다.

- `IMovementRule`
- `StepMovementRule`
- `SlideMovementRule`
- `LeapMovementRule`
- `CompoundMovementRule`
- `ConditionalMovementRule`
- `MovementRuleFactory`

기물 데이터 쪽에는 다음 타입을 추가했다.

- `MovementRuleKind`
- `MovementConditionType`
- `MovementRuleData`

이를 통해 앞으로 새 기물은 전용 이동 함수를 계속 추가하기보다 이동 패턴 데이터를 조합해 정의할 수 있게 했다.

### 3. PieceDefinition에 이동 규칙 데이터 추가
- `PieceDefinition`에 `_movementRules` 배열 추가.
- 외부에서는 `MovementRules` 프로퍼티로 null 없이 읽도록 구성.
- 기존 `_movementType`은 기존 9종, 저장 데이터, 테스트 호환을 위해 유지.
- 새 기물은 `MovementRules` 데이터가 존재하면 이를 우선 사용하도록 설계.

### 4. 기존 이동 API 하위 호환 유지
기존 호출:

```csharp
MovementResolver.GetReachableTiles(
    PieceMovementType.Rook,
    origin,
    isPlayerPiece,
    board);
```

은 그대로 유지한다.

새 데이터 기반 호출:

```csharp
MovementResolver.GetReachableTiles(
    pieceDefinition,
    origin,
    isPlayerPiece,
    board);
```

을 추가했다.

`BoardInputController`의 실제 기물 선택 흐름도 `PieceDefinition`을 직접 넘기는 방식으로 변경해 Battle 씬에서도 데이터 기반 이동 규칙을 사용하도록 연결했다.

### 5. Wazir 데이터 기반 기물 추가
- `Wazir.asset` 생성.
- `PieceMovementType.Custom`을 사용해 Wazir 전용 enum은 추가하지 않음.
- 이동 규칙은 `Step` 데이터로 구성.
- 이동 벡터:
  - `(1, 0)`
  - `(-1, 0)`
  - `(0, 1)`
  - `(0, -1)`
- 최대 이동 거리 1칸.
- 결과적으로 상/하/좌/우 1칸 이동.
- 대각선 이동 및 직교 2칸 이동은 불가.
- `PieceDatabase.asset`에도 Wazir 등록.

### 6. 기존 기물 이동 규칙 회귀 호환
기존 enum 기반 기물은 `MovementRuleFactory.CreateLegacy()` 경로를 통해 기존 동작을 유지하도록 구성했다.

주요 확인 대상:
- King
- Pawn
- Knight
- Bishop
- Rook
- Queen
- Archbishop
- Chancellor
- Amazon

특히 기존 복합 기물은 Compound 규칙으로 재사용할 수 있도록 기반을 마련했다.

### 7. Pawn Conditional 규칙 유지
프로젝트 η의 현재 Pawn 규칙을 유지했다.

- 아군은 +Y 방향 전진.
- 적은 -Y 방향 전진.
- 전방이 비어 있으면 1칸 이동 가능.
- 전방 1칸이 비어 있을 때 전방 2칸까지 이동 가능.
- 전방 대각선은 공격 후보.
- 기존 프로젝트 전투 규칙과 동일한 결과를 반환하도록 Conditional 이동 구조에 연결.

### 8. 죽은 카드 중복 복귀 버그 수정
기존 문제:

```text
OwnedCardPool에 카드 존재
↓
기물 사망
↓
DeadCardPile에도 추가
↓
라운드 종료 시 DeadCardPile을 OwnedCardPool에 추가
↓
카드 수가 +1 복제
```

수정 후:

```text
기물 사망
↓
OwnedCardPool에서 동일 카드 1장 제거
↓
DeadCardPile에 1장 추가
↓
라운드 종료
↓
DeadCardPile에서 OwnedCardPool로 복귀
↓
원래 카드 수 유지
```

`DeckState.MoveToDeadPile()`이 실제 카드 이동 역할을 하도록 수정했다.

### 9. 영구 보유 수 계산 보완
- 죽은 카드는 영구적으로 잃은 카드가 아니므로 4·5성 보유 제한 계산에는 계속 포함되어야 함.
- `RunState.CountOwnedCopies()`가:
  - `OwnedCardPool`
  - `DeadCardPile`
  두 위치의 동일 카드를 합산하도록 변경.
- 따라서 사망 중에도 합성·보유 제한 계산이 실제 영구 보유 수와 일치하도록 유지.

### 10. 23일차 EditMode 테스트 추가
`Day23MovementRulesTests`를 추가해 다음 항목을 검증하도록 구성했다.

- Wazir가 `Custom` 타입 + `MovementRules` 데이터로 동작.
- Wazir가 직교 1칸만 이동.
- Wazir 대각선 이동 불가.
- Wazir 직교 2칸 이동 불가.
- 아군 점유 칸은 이동/공격 불가.
- 적 점유 칸은 공격 후보.
- 기존 `PieceMovementType` 기반 API 호환.
- Rook / Knight / Archbishop 기존 이동 결과 유지.
- 프로젝트 η Pawn 2칸 전진 및 대각선 공격 규칙 유지.

`DeckStateTests`에도 죽은 카드 이동·복귀 후 카드 수가 중복되지 않는 회귀 검증을 추가했다.

### 11. BoardInputController 컴파일 오류 수정
23일차 덮어쓰기 과정에서 `BoardInputController.cs.meta`의 GUID가 잘못 생성되어 다음과 같은 연쇄 컴파일 오류가 발생했다.

```text
CS0246: BoardInputController could not be found
```

영향 파일:
- `BattleController.cs`
- `DeckPanelUI.cs`
- `FusionPanelUI.cs`
- `HandUI.cs`

원인은 이 파일들의 namespace/import가 아니라 `BoardInputController.cs.meta`의 잘못된 GUID였다.

수정:
- 기존 정상 GUID인 `2b3c4d5e6f708192a3b4c5d6e7f8091a` 복원.
- `BoardInputController.cs`의 23일차 `PieceDefinition` 기반 이동 연결은 그대로 유지.

## 최종 이동 구조

```text
BoardInputController
↓
PieceDefinition
↓
MovementResolver
↓
MovementRuleFactory
↓
IMovementRule
├─ Step
├─ Slide
├─ Leap
├─ Compound
└─ Conditional
↓
MovementResult
```

## 신규 기물 추가 목표 흐름

23일차 이후 단순 이동 계열 신규 기물은 다음 방식을 목표로 한다.

```text
PieceDefinition 생성
↓
MovementRules 데이터 입력
↓
PieceDatabase 등록
↓
완료
```

가능한 경우 다음 작업은 하지 않는다.

```text
새 PieceMovementType enum 추가
MovementResolver switch 추가
기물 전용 이동 함수 추가
```

## 완료 기준 체크

- [x] `MovementResolver`를 이동 규칙 파사드 구조로 단순화.
- [x] 기존 `PieceMovementType` API 유지.
- [x] `PieceDefinition` 데이터 기반 이동 API 추가.
- [x] Step 이동 규칙 구현.
- [x] Slide 이동 규칙 구현.
- [x] Leap 이동 규칙 구현.
- [x] Compound 이동 규칙 구현.
- [x] Conditional 이동 규칙 구현.
- [x] Wazir를 전용 enum 없이 데이터 기반으로 추가.
- [x] `PieceDatabase`에 Wazir 등록.
- [x] 실제 `BoardInputController` 선택 흐름을 PieceDefinition 기반 이동 계산으로 변경.
- [x] 죽은 카드 보유 풀 중복 복귀 문제 수정.
- [x] 사망 카드도 영구 보유 수에는 포함하도록 계산 보완.
- [x] 23일차 이동 규칙 회귀 테스트 추가.
- [x] 잘못된 `BoardInputController.cs.meta` GUID 복원.
- [ ] Unity Test Runner에서 전체 EditMode 테스트 최종 통과 재확인.
- [ ] 이후 24일차 신규 페어리 기물 대량 추가에서 데이터 기반 구조 실제 확장성 검증.

23일차에서는 기물 하나마다 `MovementResolver`에 분기를 추가하던 구조에서 벗어나 재사용 가능한 이동 규칙 조합 구조로 전환했다. Wazir를 첫 데이터 기반 기물로 추가해 신규 기물이 `PieceDefinition`의 이동 데이터만으로 동작할 수 있는 경로를 만들었고, 동시에 죽은 카드가 라운드 종료 후 복제되는 카드 생명주기 오류와 BoardInputController 메타 GUID 문제를 정리했다.
