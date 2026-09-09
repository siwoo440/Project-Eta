# 65일차 : Reward·Shop·Event UI 통합 정식화 및 Stage Activity 상태 피드백 개선

## 개발 목표

65일차에서는 46~47일차에 구현되어 있던 카드 보상, 상점, 이벤트의 실제 게임 로직을 유지하면서 플레이어가 비전투 선택 결과를 명확하게 확인할 수 있도록 Reward·Shop·Event UI를 정식화했다.

핵심 목표는 다음과 같다.

- Reward 카드 3택 화면 정식화
- 후보 카드 Artwork·등급·ATK·HP·설명 표시
- 카드 클릭과 실제 획득 확정을 분리
- 선택 카드 강조 및 상세 비교 정보 표시
- Reward 화면에서 Gold·King HP·보유 카드 수 표시
- Shop·Event 공통 상태 HUD 추가
- Shop·Event 진행 중 Gold·King HP·보유 카드 수 실시간 표시
- 구매·제거·회복·강화 결과를 상태 변화 기반으로 감지
- 이벤트 Gold·HP·카드 획득 결과 표시
- 기존 45일차 Placeholder 중복 표시 억제
- 기존 카드 보상·상점·이벤트 실행 로직 재사용
- Stage Activity 결과 판정에 대한 EditMode 회귀 테스트 추가

## 기존 Reward·Shop·Event 로직 유지

65일차에서는 비전투 스테이지 규칙 자체를 새로 만들지 않았다.

기존 시스템이 담당하던 다음 기능을 그대로 사용한다.

- `CardRewardController`의 전투 승리·Reward Node 카드 보상 흐름
- `CardRewardGenerator`의 카드 후보 생성
- `CardRewardRules`의 카드 획득 가능 여부 판정
- `RunEconomyState`의 Gold 상태 관리
- `StageActivityController`의 Shop·Event 진행
- Shop 카드 구매
- Shop 카드 제거
- Shop King HP 회복
- Shop 카드 업그레이드
- Event 무료 카드 획득
- Event 무료 휴식
- Event 위험 계약
- `RunStageFlowService`의 비전투 스테이지 완료 처리

따라서 이번 작업의 중심은 이미 존재하는 런 로직을 다시 구현하는 것이 아니라 플레이어가 현재 상태와 선택 결과를 읽을 수 있도록 UI 계층을 정리하는 것이다.

## CardRewardUI 정식화

기존 `CardRewardUI`를 65일차 Reward 카드 선택 화면으로 확장했다.

기존에는 후보 카드의 기본 텍스트 정보와 버튼 선택이 중심이었다면, 65일차에서는 실제 카드 선택 화면에 가까운 구조로 변경했다.

후보 카드에서 표시하는 정보:

- 카드 이름
- 별 등급
- 카드 Artwork
- ATK
- HP
- Category
- MovementType
- 카드 설명

카드 Artwork는 기존 `PieceDefinition.CardArtwork`를 사용한다.

Artwork가 없는 경우에도 카드 UI가 깨지지 않도록 대체 배경색을 표시한다.

## Reward 카드 선택과 확정 분리

후보 카드를 클릭하는 동작과 실제 카드 획득을 분리했다.

기존에는 카드 버튼을 클릭하면 곧바로 선택 콜백이 실행되는 구조였지만, 65일차에서는 다음 단계로 처리한다.

`후보 카드 클릭 → 선택 강조 → 상세 정보 확인 → 선택 확정 → 기존 CardRewardController 획득 처리`

후보 카드를 클릭하면 해당 카드는 금색 외곽선과 약간의 확대 효과로 선택 상태를 표시한다.

실제 카드 획득은 하단의 `선택 확정` 버튼을 눌렀을 때만 기존 선택 콜백으로 전달된다.

확정 버튼은 카드를 선택하기 전에는 비활성 상태를 유지한다.

이를 통해 세 후보를 먼저 비교하고 선택을 변경한 뒤 최종 카드를 확정할 수 있다.

## Reward 상세 비교 패널

카드 하단에 선택 카드 상세 패널을 추가했다.

선택 전에는 세 후보의 등급·ATK·HP·설명을 비교하라는 기본 안내를 표시한다.

카드를 선택하면 다음 정보가 상세 패널에 표시된다.

- 카드 이름
- 별 등급
- ATK
- HP
- Category
- MovementType
- Description

각 후보 카드 본체에도 주요 정보가 함께 표시되므로 화면 안에서 세 후보를 직접 비교하고 선택한 카드의 상세 정보를 다시 확인할 수 있다.

## Reward 런 상태 표시

Reward 화면 상단에는 현재 런 상태를 표시한다.

표시 항목:

- Gold
- King HP
- 보유 카드 수

Gold는 기존 `RunEconomyService.GetOrCreate()`를 통해 현재 `RunState`와 연결된 값을 읽는다.

King HP와 보유 카드 수도 현재 `RunState`의 값을 그대로 사용한다.

## Day65ActivityDeltaState

Shop·Event에서 실제 선택 결과를 UI에 표시하기 위한 상태 변화 추적 클래스를 추가했다.

`Day65ActivityDeltaState`는 이전 프레임의 다음 값을 스냅샷으로 저장한다.

- Gold
- King HP
- 보유 카드 수

새 값을 입력하면 이전 스냅샷과 비교해 다음 변화량을 계산한다.

- `CurrencyDelta`
- `KingHpDelta`
- `OwnedCardDelta`

최초 스냅샷은 변화 결과로 취급하지 않고 이후 실제 값 변화부터 결과를 생성한다.

런 인스턴스가 변경되면 기존 비교 기준을 제거할 수 있도록 `Clear()`를 제공한다.

## Stage Activity 결과 문구

`Day65ActivityDelta.BuildSummary()`에서 현재 `RunFlowPhase`와 값 변화 패턴을 조합해 플레이어가 이해하기 쉬운 결과 문구를 생성한다.

Shop 주요 판정:

- Gold -30 + 카드 증가 → 카드 구매 완료
- Gold -40 + 카드 감소 → 카드 제거 완료
- Gold -20 + King HP 증가 → King HP 회복
- Gold -50 → 카드 강화 완료

Event 주요 판정:

- Gold +60 + King HP 감소 → 위험한 계약
- Gold 변화 없이 카드 증가 → 무료 카드 획득
- Gold 변화 없이 King HP 증가 → 휴식 완료

Reward에서는 보유 카드 증가를 카드 보상 획득으로 표시할 수 있는 공통 판정을 제공한다.

정해진 패턴과 일치하지 않는 변화도 Gold·HP·보유 카드 증감값을 조합한 일반 결과 문구로 표시할 수 있다.

## Day65StageActivityHUD

Reward·Shop·Event의 상태와 선택 결과를 보조하는 공통 HUD를 추가했다.

`Day65StageActivityHUD`는 Battle Scene 로드 후 별도 Inspector 연결 없이 자동 생성된다.

주요 연결 대상:

- `BattleController`
- `RunState`
- `RunEconomyState`
- `RunFlowPhase`
- `StagePlaceholderUI`

`DefaultExecutionOrder(1180)`을 사용해 기존 Stage Activity 상태 변경 뒤 UI를 갱신하도록 구성했다.

## Shop·Event 공통 상태 바

현재 흐름이 Shop 또는 Event일 때 화면 상단에 공통 상태 바를 표시한다.

표시 항목:

- 현재 활동 이름 `SHOP` / `EVENT`
- Gold
- King HP
- 보유 카드 수

Reward는 자체 정식 화면에서 동일한 상태 정보를 표시하므로 공통 상태 바는 Shop·Event에 집중한다.

현재 흐름이 다른 상태로 전환되면 상태 바를 자동으로 숨긴다.

## 선택 결과 토스트

Gold·HP·보유 카드 수 변화가 감지되면 화면 상단에 선택 결과 토스트를 표시한다.

예시:

- `카드 구매 완료 · Gold -30`
- `카드 제거 완료 · Gold -40`
- `King HP 회복 · HP +1 · Gold -20`
- `카드 강화 완료 · Gold -50`
- `위험한 계약 · HP -1 · Gold +60`
- `카드 획득 · 보유 카드 +1`

결과 토스트 유지 시간은 2.8초다.

Shop·Event 선택 직후 `RunFlowPhase`가 Map으로 전환되는 경우에도 직전 Activity Phase를 기준으로 결과 문구를 판정하도록 구성했다.

## 기존 Placeholder 중복 표시 억제

45일차의 `StagePlaceholderUI` 소스는 삭제하지 않는다.

65일차 HUD가 Shop·Event 진행 중 기존 Placeholder가 실제로 표시된 상태를 발견하면 `Hide()`를 호출해 정식 Stage Activity UI와 구형 임시 화면이 동시에 표시되지 않도록 한다.

이를 통해 기존 진행 구조를 보존하면서 화면 표시만 65일차 정식 UI 흐름으로 정리한다.

## Day65ActivityDeltaStateTests

Stage Activity 결과 판정의 회귀를 막기 위한 EditMode 테스트를 추가했다.

검증 항목:

- 최초 스냅샷은 변화 없음
- Shop 카드 구매 결과 판정
- Shop 카드 제거 결과 판정
- Shop King HP 회복 결과 판정
- Shop 카드 업그레이드 결과 판정
- Event 위험 계약 결과 판정
- Event 무료 카드 획득 결과 판정

현재 테스트 파일에는 총 7개의 NUnit 테스트가 포함되어 있다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/UI/Day65ActivityDeltaState.cs`
- `Assets/ProjectEta/Scripts/UI/Day65ActivityDeltaState.cs.meta`
- `Assets/ProjectEta/Scripts/UI/Day65StageActivityHUD.cs`
- `Assets/ProjectEta/Scripts/UI/Day65StageActivityHUD.cs.meta`
- `Assets/ProjectEta/Tests/EditMode/Day65ActivityDeltaStateTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day65ActivityDeltaStateTests.cs.meta`
- `Devlogs/Day65/README.md`

### 수정

- `Assets/ProjectEta/Scripts/UI/CardRewardUI.cs`

### 삭제

없음.

## 결과

65일차 작업으로 Reward·Shop·Event에서 플레이어가 현재 런 상태와 선택 결과를 이전보다 명확하게 확인할 수 있는 UI 계층이 추가됐다.

Reward에서는 카드 3장의 Artwork·등급·능력치·설명을 직접 비교하고 후보를 선택한 뒤 별도의 확정 버튼으로 실제 카드 획득을 진행한다.

Shop·Event에서는 기존 Stage Activity 실행 로직을 변경하지 않고 현재 Gold·King HP·보유 카드 수를 공통 HUD에서 확인할 수 있으며, 실제 상태 변화량을 이용해 구매·제거·회복·강화·이벤트 결과를 화면 토스트로 표시한다.

게임 규칙은 기존 `CardRewardController`, `StageActivityController`, `RunEconomyState` 등을 그대로 재사용해 기존 Run 진행 로직과 UI 정식화 책임을 분리했다.

## 검증 상태

2026-09-09 기준 `README.md` 추가 전 GitHub `main` 65일차 커밋:

- SHA: `02e692a799dcb37be8f0cb080cd97b019eaa2ceb`
- 메시지: `65`
- 부모 커밋: `662cd9b57e4ec1fe251d2967659e16b556347ff4`
- 64일차 대비 커밋 수: 1
- 64일차 대비 변경 파일: 7개
- 변경 형태: 신규 파일 6개, 기존 파일 수정 1개, 삭제 없음

GitHub 저장소에서 다음 내용을 확인했다.

- `CardRewardUI`가 후보 선택과 실제 선택 확정을 분리
- Reward 후보 카드에 Artwork·등급·ATK·HP·설명 표시
- Reward 선택 카드 상세 패널과 선택 강조 처리 존재
- Reward 화면에서 Gold·King HP·보유 카드 수 표시
- `Day65ActivityDeltaState`가 Gold·King HP·보유 카드 수 스냅샷 비교
- Shop 구매·제거·회복·강화 결과 문구 판정 존재
- Event 위험 계약·무료 카드 획득 결과 문구 판정 존재
- `Day65StageActivityHUD`가 Battle Scene에서 자동 생성
- Shop·Event 공통 상태 바 표시
- 선택 결과 토스트 표시
- 구형 `StagePlaceholderUI` 중복 표시 억제
- `Day65ActivityDeltaStateTests`에 결과 판정 관련 테스트 7개 존재

GitHub commit status에는 연결된 자동 CI/status check 결과가 현재 없다.

별도로 65일차 배포 ZIP에 대해 압축 무결성과 C# 파일의 단순 중괄호 균형을 확인했으며 해당 정적 검사에서는 오류가 발견되지 않았다.

다만 현재 작업 환경에서는 Unity Editor 컴파일과 EditMode Test Runner를 실행할 수 없으므로 실제 Unity 컴파일 및 런타임 동작 성공 여부는 이 검증만으로 확정하지 않는다.

## 다음 개발 연결

65일차에서 Reward·Shop·Event의 비전투 선택 UI와 상태 피드백을 정리했으므로 다음 일차에서는 RouteMap과 화면 흐름 UI를 정식화하고 Battle·Reward·Shop·Event·Map 사이의 전환 표현을 하나의 Run UI 흐름으로 연결하는 작업으로 이어갈 수 있다.
