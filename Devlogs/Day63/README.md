# 63일차 : 전투 핵심 UI 정식화·전투 피드백 및 UI 배치 정리

## 개발 목표

63일차에서는 62일차에 정식화한 `BattleHUD`를 기준으로 실제 전투 조작에 필요한 UI를 정리하고, 기존 전투 UI의 중복 표시를 줄이며 전투 피드백을 강화했다.

핵심 목표는 다음과 같다.

- 전투 상태별 조작 안내 UI 추가
- 최초 배치에서 King 우선 배치 안내
- Deployment / Player Turn / Enemy Turn 상태 안내
- 선택 기물의 이동·공격 가능 수 표시
- 기존 TurnStatus / Deployment 배너 중복 표시 정리
- 손패 최대 10장 표시 간격 보정
- 카드 Hover 강조 보강
- 피해량 Floating Text 추가
- 기존 카메라 조작 안내 Text 제거
- 좌측 상단 Battle HUD와 보조 안내 패널의 기능적 배치 정리
- 보스 HP 바 상단 정렬 보정 시도
- 시작 덱 중복 카드 정의를 고려한 CardFlow 테스트 수정

## BattleInteractionPresentation

전투 조작 안내 문구 계산을 런타임 UI 생성 코드와 분리하기 위해 `BattleInteractionPresentation`을 추가했다.

현재 상태별 안내는 다음과 같다.

### 최초 King 배치 전

`KING FIRST · 손패의 킹 카드를 아군 배치 영역으로 드래그`

### Deployment Turn

`DEPLOYMENT · 원하는 카드를 배치한 뒤 Space로 종료`

### Player Turn

`PLAYER TURN · 기물 선택 또는 손패 카드를 보드로 드래그`

기물이 선택된 경우:

`선택 기물 · MOVE 4 · ATTACK 2`

### Enemy Turn

`ENEMY TURN · 적 행동 처리 중`

### Battle Ended

전투 종료 상태에서는 추가 조작 안내를 표시하지 않는다.

## BattleInteractionStatusUI

`BattleInteractionStatusUI`를 추가해 Battle Scene에서 현재 전투 입력 상태를 자동으로 표시하도록 했다.

Battle Scene 로드 후 다음 객체를 찾아 연결한다.

- `BattleController`
- `BoardInputController`
- `TurnManager`

다음 이벤트와 상태를 사용해 안내를 갱신한다.

- 기물 선택 변화
- 손패 변화
- Turn 변화
- 현재 이동 가능 칸 수
- 현재 공격 가능 칸 수
- 최초 Deployment 여부
- King 배치 완료 여부

전투가 준비되기 전에 UI가 먼저 생성되는 상황을 고려해 제한된 프레임 동안 런타임 객체 연결을 기다린다.

## 구형 턴 UI 중복 정리

62일차 `BattleHUD`와 역할이 겹치는 기존 UI는 화면에서 숨기도록 정리했다.

대상은 다음과 같다.

- `TurnStatusCanvas`
- `DeploymentTurnBannerCanvas`
- `RoundSummaryCanvas`의 전투 중 중복 정보
- 전투 입력 패널의 `LegendText`

기존 기능 클래스 자체는 삭제하지 않고 런타임 표시만 정리해 기존 전투 로직과의 결합을 최소화했다.

## 좌측 상단 전투 UI 배치 정리

`BattleHudLegacyLayoutFix`를 추가해 전투 상단 UI를 기능적으로 재배치했다.

현재 기준은 다음과 같다.

- `BattleHUDRoot_Day62`: 좌측 상단
- `KingPlacementRoot_Day61`: Battle HUD 아래
- `BattleInteractionPanel`: King 안내 아래
- 중앙의 중복 Round / Turn UI 숨김
- 불필요한 조작·색상 범례 숨김

63일차에서는 정확한 최종 픽셀 레이아웃을 확정하지 않고, 서로 겹치지 않는 기능적 배치를 우선 적용했다.

전체 UI의 최종 Anchor / Pivot / 여백 / 해상도 대응은 이후 UI 상세 배치 확정 일차에서 다시 조정한다.

## 카메라 조작 안내 Text 제거

`Day41SeatedCameraRig.OnGUI()`에서 표시하던 카메라 조작 안내 Text를 제거했다.

기존 표시 예시는 다음과 같았다.

`카메라 뷰: 2 기본 | W: 맨 위/기본 전환 | S: 상대 시점 | ...`

화면 안내 Text만 제거했으며 기존 카메라 전환 및 회전 기능은 유지한다.

## 손패 UI 보정

기존 `HandUI`의 최대 10장 표시 구조를 유지하면서 카드 간격을 조정했다.

변경:

- 카드 간격: `-24` → `-30`

이를 통해 최대 10장 손패가 기존 1540px 영역 안에서 더 안정적으로 표시되도록 했다.

## CardView Hover 보강

사용 가능한 카드의 Hover 확대 비율을 다음과 같이 조정했다.

- 기존: `1.05`
- 변경: `1.08`

카드 선택 후보를 조금 더 명확히 구분하도록 했으며 Drag & Drop, 사용 가능 여부, 기존 카드 상태 로직은 유지한다.

## CombatFloatingTextUI

피해 발생 위치 위에 실제 적용 피해량을 표시하는 `CombatFloatingTextUI`를 추가했다.

`BattleHooks.AfterDamage`를 구독해 실제 피해가 발생했을 때 다음 형태로 표시한다.

- `-2`
- `-5`
- `-10`

주요 동작은 다음과 같다.

- 피해 대상 기물의 보드 좌표를 화면 좌표로 변환
- 실제 양수 피해만 표시
- 동일 위치 연속 피해 숫자의 완전한 겹침 방지
- 숫자가 위로 이동하며 Fade Out
- 일정 시간이 지나면 자동 제거
- 아군 피해와 적 피해의 표시색 구분
- UI가 보드 입력을 막지 않도록 Raycast 비활성화

현재 별도의 Heal 이벤트가 없으므로 63일차에서는 피해 표시만 자동 연결했다.

## Day63BattleCoreUITests

63일차 표시 계산의 회귀를 막기 위한 EditMode 테스트를 추가했다.

검증 항목:

- 최초 Deployment에서 `KING FIRST` 안내
- 선택 기물의 MOVE 수 표시
- 선택 기물의 ATTACK 수 표시
- Enemy Turn 안내
- Battle Ended에서 안내 숨김
- 양수 피해 `-7` 표시
- 0 피해 표시 생략

## CardFlowTests 오류 수정

시작 덱을 다음처럼 구성하면서 동일한 `PieceDefinition` 참조가 여러 장 존재하게 됐다.

- King 1
- Pawn 8
- Rook 2
- Bishop 2
- Queen 1
- Knight 2

기존 테스트 `DeploymentTurn_DiscardCard_RemovesFromHandAndAddsToDrawPileBottom`은 정리한 카드 정의가 손패에 더 이상 존재하지 않아야 한다고 검사했다.

기존 검증:

`Assert.IsFalse(context.RunState.Hand.Hand.Contains(cardToDiscard));`

Pawn처럼 같은 `PieceDefinition`을 여러 장 사용하는 경우 한 장을 정상적으로 정리해도 다른 Pawn이 손패에 남아 있으므로 해당 검증은 실패할 수 있었다.

실제 런타임 결과는 다음처럼 정상적으로 한 장 이동하고 있었다.

- Hand: 5 → 4
- DrawPile: 11 → 12

따라서 테스트를 카드 정의 전체 부재 여부가 아니라 정확히 한 장이 이동했는지 확인하도록 변경했다.

새 검증 기준:

- 같은 정의 카드 수가 정확히 1 감소
- Hand 전체 수가 1 감소
- DrawPile 전체 수가 1 증가
- 정리한 카드 정의가 `DrawPile[0]`에 존재

현재 `DeckState`는 리스트 마지막 인덱스를 드로우 더미 맨 위로 사용하므로 `DrawPile[0]`은 맨 아래 카드다.

## Day63CardFlowTestFixPatcher

기존 CardFlow 테스트가 아직 이전 `Contains()` 검증을 사용하는 프로젝트에서도 자동 보정할 수 있도록 Editor 패처를 추가했다.

패처는 다음을 수행한다.

- 기존 잘못된 `Contains()` 기반 검증 탐색
- 같은 카드 정의 수량 감소 검증 추가
- DrawPile 맨 아래 검증 추가
- 중복 적용 방지
- 수정 후 Test Assembly 재임포트

최신 커밋의 `CardFlowTests.cs`에는 이미 수정 내용이 직접 반영돼 있다.

## 보스 HP 바 상단 배치

`BattleHudLegacyLayoutFix`와 `BossHealthTopLineFix`를 통해 보스 HP 바를 상단 UI와 같은 기준선으로 이동시키는 보정을 추가했다.

`BossHealthTopLineFix`는 실제 런타임 오브젝트 이름인 `BossHealthPanel`을 직접 찾고 다음 값을 적용한다.

- Anchor: Top Center
- Pivot: Top Center
- 목표 위치: `(0, -18)`
- 크기: `620 × 54`
- 실행 순서: `11000`

다만 실제 플레이 화면에서는 보스 HP 바가 아직 다른 상단 UI와 완전히 같은 선까지 올라오지 않는 현상이 남아 있다.

이 항목은 63일차 완료를 막는 전투 로직 오류로 보지 않고 **알려진 UI 배치 잔여 문제**로 기록한다.

보스 HP 바의 최종 위치는 이후 UI 배치 정리 과정에서 다시 수정한다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Editor/Day63BattleUIPolishPatcher.cs`
- `Assets/ProjectEta/Editor/Day63BattleUIPolishPatcher.cs.meta`
- `Assets/ProjectEta/Editor/Day63CardFlowTestFixPatcher.cs`
- `Assets/ProjectEta/Editor/Day63CardFlowTestFixPatcher.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BattleHudLegacyLayoutFix.cs`
- `Assets/ProjectEta/Scripts/UI/BattleHudLegacyLayoutFix.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BattleInteractionPresentation.cs`
- `Assets/ProjectEta/Scripts/UI/BattleInteractionPresentation.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BattleInteractionStatusUI.cs`
- `Assets/ProjectEta/Scripts/UI/BattleInteractionStatusUI.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BossHealthTopLineFix.cs`
- `Assets/ProjectEta/Scripts/UI/BossHealthTopLineFix.cs.meta`
- `Assets/ProjectEta/Scripts/UI/CombatFloatingTextUI.cs`
- `Assets/ProjectEta/Scripts/UI/CombatFloatingTextUI.cs.meta`
- `Assets/ProjectEta/Tests/EditMode/Day63BattleCoreUITests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day63BattleCoreUITests.cs.meta`

### 수정

- `Assets/ProjectEta/Scripts/Environment/Day41SeatedCameraRig.cs`
- `Assets/ProjectEta/Scripts/UI/CardView.cs`
- `Assets/ProjectEta/Scripts/UI/HandUI.cs`
- `Assets/ProjectEta/Tests/EditMode/CardFlowTests.cs`
- `Devlogs/Day63/README.md`

### 삭제

없음.

## 결과

63일차 작업으로 62일차의 공통 Battle HUD 위에 실제 전투 조작을 위한 상태 안내와 피해 피드백이 추가됐다.

전투 중 플레이어는 현재 Deployment / Player / Enemy 상태와 선택 기물의 MOVE / ATTACK 후보 수를 확인할 수 있으며, 피해 발생 시 대상 기물 위치 위에 실제 피해량이 표시된다.

기존 TurnStatus, Deployment Banner, 중앙 중복 정보와 카메라 조작 안내 Text를 정리해 화면 중복을 줄였다.

손패 10장 표시와 카드 Hover도 소폭 보정했으며, 시작 덱에 동일 PieceDefinition 카드가 여러 장 존재하는 구조에 맞춰 CardFlow 테스트의 잘못된 검증 조건도 수정했다.

## 검증 상태

2026-09-07 기준 GitHub `main` 최신 커밋:

- SHA: `d2502d2d4a65355e2cf58ef4227e9337b4e6c46b`
- 메시지: `63`
- 부모 커밋: `5557871e7e51578e3a7bca2df7f890c87903a9b4`
- 62일차 대비 변경 파일: 20개

GitHub 저장소에서 다음 내용을 확인했다.

- `BattleInteractionPresentation` 상태별 안내 계산 존재
- `BattleInteractionStatusUI` Battle Scene 자동 생성 및 상태 연결 존재
- 구형 TurnStatus / Deployment UI 숨김 처리 존재
- 카메라 조작 안내 `OnGUI()` 제거 반영
- HandUI 카드 간격 `-30` 반영
- CardView Hover `1.08` 반영
- `CombatFloatingTextUI` 피해 이벤트 연결 존재
- `Day63BattleCoreUITests` 6개 표시 규칙 테스트 존재
- `CardFlowTests` 중복 카드 정의 수량 검증으로 수정 반영
- `BossHealthTopLineFix` 상단 정렬 보정 코드 존재

GitHub commit status에는 연결된 CI 상태 검사가 등록되어 있지 않다.

따라서 저장소 코드와 테스트 소스의 변경 반영은 확인했지만 Unity Editor 전체 EditMode / PlayMode TestRunner 통과 여부는 GitHub 상태만으로 확정할 수 없다.

현재 확인된 잔여 사항:

- 보스 HP 바가 실제 플레이 화면에서 상단 다른 UI와 완전히 같은 선까지 올라오지 않음
- 해당 배치 문제는 이후 UI 레이아웃 수정 시 다시 조정 예정

## 다음 일차

64일차에서는 카드·Fusion UI를 통합 정식화한다.

주요 예정 범위:

- 합성 가능 카드 강조
- Fusion 재료 선택 상태
- `A + B = C` 구성
- 결과 카드 미리보기
- Recipe 발견 상태
- Fusion 가능 / 불가 사유 표현
