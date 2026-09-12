# 76일차 : 카드·기물·Enemy·Fusion 콘텐츠 Pool 규칙 통합 및 무결성 검증

## 개발 목표

75일차에서 RouteMap 전체 편성과 Elite / Mid Boss 보상 경로를 실제 런 흐름에 연결한 뒤, 76일차에서는 여러 시스템에 흩어져 있던 카드·기물 사용 규칙을 하나의 공통 정책으로 정리했다.

이번 작업의 핵심은 다음 두 가지다.

- Reward / Shop / Enemy / Boss가 어떤 PieceDefinition을 사용할 수 있는지 공통 규칙으로 통합
- PieceId, Boss Resource, Fusion Recipe 참조를 한 번에 확인할 수 있는 상위 콘텐츠 무결성 검증기 추가

기존 Reward, Enemy Encounter, Fusion 시스템을 새로 교체하지 않고 기존 규칙을 최대한 재사용하는 방식으로 구성했다.

---

## RunContentPoolRules 추가

`RunContentPoolRules`를 새로 추가하여 콘텐츠별 Piece 사용 가능 여부를 한곳에서 판정하도록 했다.

현재 제공하는 규칙은 다음과 같다.

```text
IsValidPiece
CanUseAsReward
CanUseInShop
CanUseAsEnemy
CanUseAsBoss
```

이제 Reward와 Enemy가 각자 별도의 필터 조건을 중복해서 보유하지 않고 공통 정책을 참조한다.

---

## Reward Pool 규칙

일반 카드 보상에서는 다음 Piece를 제외한다.

- PieceId가 비어 있는 Piece
- King 이동 타입
- Fusion Category
- Monster Category
- Boss Category
- 4성
- 5성

따라서 일반 Reward에서 직접 획득 가능한 카드는 기본적으로 1~3성 플레이어용 카드에 한정된다.

기존 `CardRewardRules.CanOffer()`는 자체적으로 Category와 Grade를 검사하던 코드를 줄이고 `RunContentPoolRules.CanUseAsReward()`를 사용하도록 변경했다.

동일 PieceId 보유 상한 3장 규칙은 기존대로 유지한다.

---

## Shop Pool 규칙

현재 Shop은 일반 카드 획득 Pool과 같은 기준을 사용한다.

```text
CanUseInShop
→ CanUseAsReward
```

따라서 일반 Reward에서 직접 등장할 수 없는 King / Fusion / Monster / Boss / 4·5성 Piece는 Shop에서도 직접 판매되지 않는다.

향후 Shop 전용 카드나 고등급 판매 정책이 추가될 경우 `CanUseInShop()`만 독립적으로 확장할 수 있는 구조를 마련했다.

---

## Enemy Pool 규칙

일반 Enemy Encounter에서는 다음 Piece를 제외한다.

- PieceId가 비어 있는 Piece
- King 이동 타입
- Fusion Category
- Boss Category

반면 다음 Category는 일반 Enemy 후보로 사용할 수 있다.

```text
Basic
Special
Monster
```

기존 `EnemyEncounterRules.CanUsePiece()`는 위 조건을 직접 검사하지 않고 `RunContentPoolRules.CanUseAsEnemy()` 결과를 사용하도록 변경했다.

ThreatScore, Phase별 후보 범위, Elite 추가 적 수 등의 기존 Enemy Encounter 규칙은 그대로 유지한다.

---

## Boss Pool 규칙

Boss 전용 Resource는 다음 조건을 만족해야 한다.

```text
PieceDefinition 존재
PieceId 존재
PieceCategory.Boss
```

이를 `RunContentPoolRules.CanUseAsBoss()`에서 판정하도록 했다.

Day74에서 추가한 다음 Boss Resource도 Day76 테스트의 실제 프로젝트 검증 대상에 포함했다.

- `MidBoss74`
- `FinalBoss74`

---

## RunContentPoolValidator 추가

전체 콘텐츠 데이터를 한 번에 검사할 수 있도록 `RunContentPoolValidator`를 추가했다.

현재 검사하는 IssueType은 다음과 같다.

```text
NullPiece
EmptyPieceId
DuplicatePieceId
InvalidBossPiece
FusionRecipeIssue
FusionMaterialNotInPool
FusionResultNotInPool
```

검증기는 PieceDefinition 목록에서 기본 ID 무결성을 검사한 뒤 Boss와 Fusion 참조 검사를 추가로 수행한다.

---

## PieceId 무결성 검증

PieceDefinition 목록에 대해 다음 항목을 검사한다.

- null PieceDefinition
- 빈 PieceId
- 중복 PieceId

중복 ID 검사는 `HashSet<string>`을 사용하여 같은 PieceId가 둘 이상 등록되는 상황을 탐지한다.

이는 Save / Load, Fusion, Enemy Spawn 등 PieceId 기반 참조에서 생길 수 있는 데이터 충돌을 조기에 찾기 위한 검증이다.

---

## Fusion Recipe 기존 Validator 재사용

Fusion 검증 규칙은 새로 중복 구현하지 않고 기존 `FusionRecipeContentValidator`를 그대로 호출한다.

기존 Validator가 검사하는 항목은 다음과 같다.

- Null Recipe
- Duplicate RecipeId
- Missing Material
- Missing Result
- Rule Violation
- Duplicate Material Pair

Day76의 `RunContentPoolValidator`는 위 결과를 `FusionRecipeIssue`로 통합한 뒤 추가로 전체 Piece Pool 참조 여부를 검사한다.

---

## Fusion 참조 무결성 검사

각 Fusion Recipe의 다음 참조를 전체 Piece Pool과 비교한다.

```text
MaterialA
MaterialB
Result
```

재료의 PieceId가 전체 Piece Pool에 없으면 `FusionMaterialNotInPool`을 기록한다.

결과 Piece의 PieceId가 전체 Piece Pool에 없으면 `FusionResultNotInPool`을 기록한다.

기획서에서 확정되지 않은 신규 Fusion Recipe는 이번 작업에서 임의로 추가하지 않았다.

---

## Day76 테스트 추가

`Day76ContentPoolTests`를 추가했다.

현재 테스트 항목은 다음과 같다.

- Reward Pool에서 King 제외
- Reward Pool에서 Fusion 제외
- Reward Pool에서 Monster 제외
- Reward Pool에서 Boss 제외
- Reward Pool에서 4·5성 제외
- Shop과 Reward의 기본 획득 규칙 일치
- Enemy Pool에서 King / Fusion / Boss 제외
- Enemy Pool에서 Monster / Special 허용
- Boss Pool에서 Boss Category만 허용
- 빈 PieceId 탐지
- 중복 PieceId 탐지
- Fusion 재료의 전체 Piece Pool 누락 탐지
- 실제 Resources의 PieceId 기본 무결성 검사
- `MidBoss74` / `FinalBoss74` Boss Category 검증

---

## PieceCategory 테스트 오류 수정

Day76 테스트 최초 적용 후 다음 컴파일 오류가 확인됐다.

```text
PieceCategory does not contain a definition for 'Normal'
```

현재 프로젝트의 `PieceCategory`는 다음 값으로 구성된다.

```text
Basic
Fusion
Special
Monster
Boss
```

따라서 Day76 테스트에 잘못 사용된 `PieceCategory.Normal` 10곳을 모두 `PieceCategory.Basic`으로 수정했다.

최신 커밋의 `Day76ContentPoolTests.cs`에는 `Normal` 참조가 남아 있지 않고 실제 프로젝트 enum과 동일한 `Basic`을 사용한다.

---

## 주요 변경 파일

### Content Pool

- `Assets/ProjectEta/Scripts/Run/RunContentPoolRules.cs`
- `Assets/ProjectEta/Scripts/Run/RunContentPoolRules.cs.meta`
- `Assets/ProjectEta/Scripts/Run/RunContentPoolValidator.cs`
- `Assets/ProjectEta/Scripts/Run/RunContentPoolValidator.cs.meta`

### 기존 규칙 연결

- `Assets/ProjectEta/Scripts/Run/CardRewardRules.cs`
- `Assets/ProjectEta/Scripts/Run/EnemyEncounterRules.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day76ContentPoolTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day76ContentPoolTests.cs.meta`

삭제된 파일은 없다.

---

## 현재 확인 상태

최신 `main`의 Day76 커밋에는 75일차 대비 총 8개 파일 변경이 반영되어 있다.

변경 구성은 다음과 같다.

- 기존 파일 수정 2개
- 신규 C# 파일 3개
- 신규 `.meta` 파일 3개
- 삭제 없음

현재 최신 Day76 테스트는 실제 프로젝트의 `PieceCategory.Basic` 정의를 사용하도록 수정되어 있으며, 기존 FusionRecipe API와 `FusionRecipeContentValidator` API도 최신 소스와 일치하는 것을 확인했다.

GitHub 저장소에는 이 커밋에 연결된 CI Status 또는 GitHub Actions Workflow Run이 없다.

따라서 원격 저장소에서 확인 가능한 범위는 코드 반영 상태와 정적 API 구조까지이며, Unity Editor의 전체 컴파일 성공 및 전체 EditMode Test 통과 여부는 로컬 Unity Test Runner 실행 결과를 기준으로 최종 확인해야 한다.

---

## 다음 개발 방향

76일차까지 주요 콘텐츠의 등장 규칙과 데이터 참조 검증 계층이 정리됐다.

다음 단계에서는 신규 콘텐츠 추가보다 실제 출시 환경을 준비하는 방향으로 넘어가는 것이 우선이다.

주요 다음 작업은 다음과 같다.

- 5 Phase 전체 장기 진행 검증
- Save / Load 회귀 테스트
- Steam Overlay 연동
- Steam Cloud 저장 연결
- Achievement 연동
- Steam 기능과 기존 UI 통합
- 전체 콘텐츠 QA 및 밸런스 점검
