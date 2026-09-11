# 72일차 : Event 콘텐츠 정식화·Fusion Recipe 검증 및 RouteMap Seed UI 안정화

## 개발 목표

71일차까지 Shop 상품·가격 규칙과 5페이즈 경제 흐름을 정리한 뒤, 72일차에서는 기존 압축 일정에 맞춰 **Event 콘텐츠 확장과 Fusion Recipe 검증 기능을 한 번에 정식화**했다.

추가로 디버깅과 런 재현을 쉽게 하기 위해 RouteMap 화면 우측 하단에 현재 `MapSeed`를 표시하도록 UI를 추가했으며, Boot → MainMenu → Battle 구조에서 런타임 UI가 생성되지 않던 Scene 초기화 방식도 함께 수정했다.

---

## Event 콘텐츠 확장

기존 Event는 Stage 깊이에 따라 소수의 이벤트가 반복되는 프로토타입 구조였다.

이번 작업에서는 `StageEventDefinition`, `StageEventChoice`, `StageEventRules`, `StageEventService`를 분리하여 이벤트 데이터·선택지·밸런스 규칙·실제 결과 적용을 각각 담당하도록 구성했다.

정식 이벤트 콘텐츠는 다음 7종으로 확장했다.

- 버려진 카드 꾸러미
- 조용한 휴식처
- 위험한 계약
- 떠돌이 상인
- 버려진 금고
- 낡은 회복 제단
- 버려진 훈련장

이벤트마다 등장 가능한 Phase, 등장 가중치, 선택지와 결과를 분리했다.

초반부터 모든 이벤트가 등장하지 않도록 Phase에 따라 이벤트 Pool을 다르게 구성했으며, 훈련과 떠돌이 상인처럼 성장에 직접 영향을 주는 이벤트는 일정 Phase 이후부터 등장하도록 제한했다.

---

## Event Seed 재현성

이벤트 생성은 다음 값을 조합한 결정적 Seed를 사용하도록 변경했다.

- RouteMap `MapSeed`
- 현재 Phase
- 현재 Stage
- 현재 NodeId

같은 Run에서 같은 노드에 접근하면 동일한 이벤트를 다시 만들 수 있고, 같은 Stage라도 Phase나 Node가 다르면 다른 결과를 만들 수 있다.

카드 후보가 필요한 이벤트는 이벤트 ID와 선택지 ID까지 추가하여 후속 Seed를 따로 생성하도록 구성했다.

이를 통해 Event 결과 재현성과 Save/Load 이후 일관성을 높였다.

---

## Event 선택 규칙

선택지마다 실제 실행 가능 여부를 `StageEventService`에서 검사하도록 정리했다.

주요 규칙은 다음과 같다.

- King HP가 1이면 HP를 소비하는 위험 계약 차단
- Gold가 부족하면 유료 카드 구매 차단
- Gold가 부족하면 유료 회복 차단
- King HP가 최대치면 불필요한 유료 회복 차단
- 카드 보상 이벤트는 현재 Reward 규칙과 연결
- 후반 Phase로 갈수록 높은 등급 카드의 가중치 증가

Event UI 컨트롤러가 직접 모든 조건과 결과를 처리하던 구조를 줄이고, 결과 적용 규칙을 Service와 Rules로 이동했다.

---

## Fusion Recipe 기능 확장

Fusion은 기존 Recipe 구조를 유지하면서 데이터 조회와 검증 기능을 확장했다.

`FusionRecipeDatabase`에 다음 기능을 추가했다.

- 재료 A+B / B+A 순서 무관 Recipe 검색
- `RecipeId` 기준 Recipe 검색
- 현재 발견 상태를 반영한 공개 Recipe 목록 조회
- 결과 기물을 기준으로 관련 Recipe 목록 조회

기존 확정 Recipe 데이터 자체를 임의로 늘리지는 않고, 현재 연결된 Recipe가 정상적인지 검증할 수 있는 구조를 우선 강화했다.

---

## Fusion Recipe 무결성 검증

`FusionRecipeContentValidator`를 추가하여 전체 Recipe Database를 한 번에 검사할 수 있도록 했다.

검사 대상은 다음과 같다.

- null Recipe
- 중복 RecipeId
- 재료 누락
- 결과 기물 누락
- 동일 재료 조합 중복 등록
- 기존 Fusion 규칙 위반

Recipe가 늘어날수록 개별 수동 확인 대신 Database 전체를 순회하여 문제를 찾을 수 있도록 구성했다.

---

## Fusion 숨김 Recipe 처리

숨김 Recipe는 발견 전에는 공개 목록에서 제외하고, `FusionDiscoveryLog`에 발견 기록이 남은 뒤부터 공개 목록에 포함하도록 연결했다.

현재 핵심 Recipe 검색과 숨김 Recipe 발견 전·후 상태를 EditMode 테스트 대상으로 추가했다.

---

## RouteMap Seed UI

Run 재현과 디버깅을 쉽게 하기 위해 RouteMap 상태에서 현재 `MapSeed`를 화면 오른쪽 아래에 표시하도록 했다.

표시 형식은 다음과 같다.

```text
SEED 123456789
```

Seed UI는 기존 RouteMap Header/Hover UI와 분리한 `Day72RouteMapSeedUI`에서 관리한다.

- RouteMap 상태에서만 표시
- Battle / Shop / Reward / Event 등 다른 상태에서는 숨김
- `RunState.RouteMap.MapSeed` 값을 직접 표시
- Screen Space Overlay Canvas 사용
- 우하단 Anchor 적용
- 기존 RouteMap UI보다 높은 Canvas Sorting Order 사용

---

## RouteMap UI Scene Load 수정

Seed UI를 처음 적용한 뒤 Boot → MainMenu → Battle 순서로 진입하면 RouteMap HUD와 Seed UI가 생성되지 않는 문제가 확인됐다.

원인은 Battle 전용 런타임 UI가 `RuntimeInitializeLoadMethod(AfterSceneLoad)`에서 현재 Scene 이름만 검사하는 구조였다.

첫 Scene인 Boot에서 콜백이 실행된 뒤 Battle Scene 진입 시 해당 생성 코드가 다시 실행되지 않을 수 있으므로, 다음 방식으로 수정했다.

- `RuntimeInitializeLoadType.BeforeSceneLoad`에서 `SceneManager.sceneLoaded` 콜백 등록
- 실제 Battle Scene이 로드된 시점에 UI 생성
- 중복 콜백 등록 방지
- 기존 UI가 이미 존재하면 중복 생성 차단

해당 변경은 `Day66RouteMapUI`와 `Day72RouteMapSeedUI`에 적용했다.

---

## 테스트 추가

### Day72 Event 테스트

다음 항목을 검증하도록 EditMode 테스트를 추가했다.

- 동일 Node의 Event Seed 재현성
- Phase가 달라질 때 Event Seed 분리
- Event 콘텐츠 7종 이상 존재
- Phase별 Event Pool 분리
- HP 1 상태 위험 선택 차단
- 유료 선택지의 Gold·HP 조건 검사
- 후반 Event 카드 보상 품질 증가

### Day72 Fusion 테스트

다음 항목을 검증하도록 EditMode 테스트를 추가했다.

- Fusion Database 존재 및 Recipe 무결성
- 재료 순서를 바꿔도 동일 Recipe 검색
- 핵심 Fusion Recipe 존재
- 숨김 Recipe 존재
- 숨김 Recipe 발견 전 비공개
- 발견 후 공개
- 결과 기물 기준 복수 Recipe 조회

---

## 주요 변경 파일

### Event

- `Assets/ProjectEta/Scripts/Run/StageEventChoice.cs`
- `Assets/ProjectEta/Scripts/Run/StageEventDefinition.cs`
- `Assets/ProjectEta/Scripts/Run/StageEventGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/StageEventRules.cs`
- `Assets/ProjectEta/Scripts/Run/StageEventService.cs`
- `Assets/ProjectEta/Scripts/Run/StageActivityController.cs`

### Fusion

- `Assets/ProjectEta/Scripts/Fusion/FusionRecipeContentValidator.cs`
- `Assets/ProjectEta/Scripts/Fusion/FusionRecipeDatabase.cs`

### RouteMap UI

- `Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day72RouteMapSeedUI.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day72EventContentTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day72FusionContentTests.cs`

---

## 현재 확인 상태

최신 `main`의 Day72 코드에는 Event 확장, Fusion 검증, RouteMap Seed UI 및 Battle Scene 로드 콜백 수정이 반영되어 있다.

GitHub 저장소에는 현재 이 커밋에 연결된 CI Status 또는 GitHub Actions Workflow Run이 없기 때문에, 원격 저장소 기준으로 Unity 실제 컴파일 성공이나 EditMode Test 전체 통과 여부는 확인할 수 없다.

따라서 이번 개발일지는 다음 범위까지를 기록한다.

- GitHub 최신 코드 반영 상태 확인
- Event/Fusion 테스트 코드 존재 확인
- RouteMap UI Scene Load 수정 코드 반영 확인
- 실제 Unity Editor 컴파일 및 Play Mode 최종 검증은 로컬 실행 결과 기준으로 확인

---

## 다음 개발 방향

압축 일정 기준 다음 일차는 **73일차 : 일반 적 + Elite 콘텐츠 완성**이다.

다음 작업에서는 일반 적 Pool과 역할을 확장하고, Elite가 단순 스탯 증가가 아닌 별도 전투 패턴과 보상 차이를 갖도록 정리하는 것을 목표로 한다.
