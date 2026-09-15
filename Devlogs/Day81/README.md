# 81일차 : 합성 완료 이벤트·등급별 카드 보유 규칙 통합

---

## 개발 목표

80일차의 Steam Achievement 통합에서 남은 카드 수 기반 합성 추측을 실제 합성 완료 이벤트로 교체했다.

카드 획득과 합성에서 서로 다르게 적용되던 동일 기물 보유 상한도 공통 규칙으로 통합했다.

---

## 명시적 합성 완료 이벤트

`BoardInputController`에 `FusionCompleted` 이벤트를 추가했다.

이 이벤트는 `TryFuseCards()`가 재료 제거와 결과 카드 추가를 완료한 뒤 실제 사용한 `FusionRecipe`를 한 번 전달한다.

`SteamGameIntegrationController`는 현재 Battle 씬의 `BoardInputController`에 연결하고 합성 성공 이벤트를 다음 Achievement로 변환한다.

- 모든 합성 성공: `FIRST_FUSION`
- 5성 합성 성공: `FIRST_FUSION`, `FIRST_FIVE_STAR`

씬 전환이나 Battle 재연결 시 이전 이벤트 구독을 해제해 중복 호출을 방지한다.

보상·상점·이벤트 카드 획득은 `DeckState.CardAcquired` 이벤트를 통해 별도로 전달한다.

저장 데이터에서 복원된 5성 카드도 Battle 연결 시 Achievement 상태를 한 번 동기화한다.

정상 보유 풀과 사망 카드 더미를 모두 확인하므로 사망 상태로 저장된 5성도 누락하지 않는다.

---

## 카드 수 기반 합성 추측 제거

80일차 구현은 `OwnedCardPool`의 전체 카드 수가 1장 줄고 새로운 카드가 생기면 합성으로 판단했다.

상점 카드 제거와 다른 카드 획득이 같은 프레임에 발생해도 같은 형태가 될 수 있어 잘못된 Achievement가 발생할 가능성이 있었다.

81일차에서는 다음 요소를 제거했다.

- 매 프레임 카드 보유 Snapshot 생성
- 이전·현재 카드 수 비교
- `IsFusionPoolDelta()` 추측 함수
- 추측 함수 전용 Day80 테스트

---

## 등급별 카드 보유 상한 통합

`CardOwnershipRules`를 추가해 동일 `PieceId`의 보유 상한을 한곳에서 관리한다.

| 등급 | 동일 기물 보유 상한 |
| --- | ---: |
| 1~3성 | 3장 |
| 4성 | 2장 |
| 5성 | 1장 |

`CardRewardRules`는 카드 추가와 보상 후보 판정에서 이 상한을 사용한다.

`FusionRuleValidator`도 같은 규칙을 사용하되, 보드 동시 배치 제한은 기존 규칙대로 4·5성에만 적용한다.

보유 수 계산은 `PieceDefinition` 참조가 아니라 `PieceId`를 비교하므로 저장 복원이나 런타임 복제 카드도 같은 기물로 판정한다.

보상·상점·이벤트 후보 생성과 실제 획득은 정상 보유 풀과 사망 카드 더미를 합쳐 상한을 판정한다.

따라서 보상·상점·이벤트·합성 경로가 같은 등급별 보유 상한을 공유한다.

---

## 테스트

`Day81RuleConsistencyTests`에 다음 회귀 테스트를 추가했다.

- 4성 동일 기물은 2장까지만 추가 가능
- 5성 동일 기물은 1장까지만 추가 가능
- 1~3성 동일 기물은 3장까지 추가 가능
- 서로 다른 정의 인스턴스도 `PieceId`가 같으면 정상·사망 보유 카드에 함께 집계
- 동일 5성이 사망 카드 더미에 있으면 후보 노출과 추가 획득 차단
- 품질 프로필 없는 균등 후보 생성도 사망 카드 보유 상한 반영
- 보상·상점·이벤트 카드 획득 시 `CardAcquired`가 정확히 한 번 발생
- 사망 카드 더미에 복원된 5성도 Steam Achievement 큐에 등록
- 실제 합성 성공 시 `FusionCompleted`가 정확히 한 번 발생
- 이벤트가 실제 사용한 `FusionRecipe`를 전달

TDD RED 실행에서는 4성 세 번째 추가가 허용되고 `FusionCompleted` 이벤트가 없어 2개 테스트가 모두 실패했다.

공통 보유 규칙과 명시적 이벤트 구현 후 81일차 대상 테스트 15개가 모두 통과했다.

전체 EditMode 회귀 테스트는 586개 모두 통과했다.

---

## 변경 파일

- `Assets/ProjectEta/Scripts/Cards/CardOwnershipRules.cs`
- `Assets/ProjectEta/Scripts/Cards/DeckState.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardRules.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardController.cs`
- `Assets/ProjectEta/Scripts/Run/RunState.cs`
- `Assets/ProjectEta/Scripts/Run/ShopOfferGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/ShopService.cs`
- `Assets/ProjectEta/Scripts/Run/StageActivityController.cs`
- `Assets/ProjectEta/Scripts/Fusion/FusionRuleValidator.cs`
- `Assets/ProjectEta/Scripts/Board/BoardInputController.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamGameEventBridge.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamGameIntegrationController.cs`
- `Assets/ProjectEta/Tests/EditMode/FusionTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day80SteamIntegrationTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day81RuleConsistencyTests.cs`

---

## 다음 개발 방향

82일차부터 실제 3성 합성 결과와 2→3성 레시피를 추가해 1~5성 합성 트리를 단계적으로 확장한다.

고등급 콘텐츠 추가 순서는 3성 데이터와 레시피, 4성 데이터와 레시피, 5성 최종 합성 데이터와 레시피 순으로 진행한다.
