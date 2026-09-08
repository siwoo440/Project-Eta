# 64일차 : 카드·Fusion UI 통합 정식화 및 손패 슬롯 기반 합성 선택 개선

## 개발 목표

64일차에서는 기존 21~22일차에 구현되어 있던 Fusion 규칙과 실행 로직을 유지하면서, 63일차에 정리한 손패 카드 UI 위에 실제 합성 조작 흐름을 정식 UI로 통합했다.

핵심 목표는 다음과 같다.

- 배치 턴에서만 Fusion UI 사용
- 손패 카드에서 합성 가능한 재료를 직접 선택
- 첫 재료 선택 후 합성 가능한 두 번째 카드만 후보로 표시
- 동일한 `PieceDefinition` 카드가 여러 장 있어도 실제 손패 슬롯 단위로 구분
- 재료 A / 재료 B 선택 및 개별 선택 해제
- `A + B = C` 형태의 합성 결과 미리보기
- 결과 카드의 이름·등급·ATK·HP·설명 표시
- 미발견 숨김 Recipe 결과 비공개 처리 유지
- 기존 `EvaluateFusion()` / `TryFuseCards()` 규칙 재사용
- 합성 성공 후 Recipe 발견 알림 표시
- 기존 21~22일차 Fusion UI 중복 표시 억제
- Fusion 손패 선택 상태에 대한 EditMode 회귀 테스트 추가

## 기존 Fusion 구조 유지

64일차에서는 Fusion 규칙 자체를 새로 만들지 않았다.

기존 `BoardInputController`가 제공하는 다음 기능을 그대로 사용한다.

- `CanUseFusionInput`
- `SetFusionModeActive()`
- `EvaluateFusion()`
- `TryFuseCards()`
- `FusionDiscovery`
- 기존 손패·보유 풀 갱신
- 숨김 Recipe 발견 기록

따라서 64일차의 핵심은 기존 Fusion 시스템을 손패 UI와 정식으로 연결하는 것이다.

실제 합성 실행 시 기존 로직이 계속 담당하는 항목은 다음과 같다.

- 배치 턴 여부 검사
- Recipe 존재 여부 검사
- 합성 가능 재료 분류 검사
- 등급 상승 규칙 검사
- 손패 재료 보유 여부 검사
- 4·5성 보유 수량 제한 검사
- 재료 카드 2장 소모
- 결과 카드 손패 추가
- 보유 카드 풀 갱신
- 숨김 Recipe 발견 기록

## FusionHandSelectionState

동일한 카드 정의가 손패에 여러 장 있을 때 실제로 서로 다른 두 장을 선택할 수 있도록 `FusionHandSelectionState`를 추가했다.

기존 Fusion 상태는 `PieceDefinition` 중심으로 동작하기 때문에 같은 정의의 카드가 여러 장 존재하면 UI에서 실제 카드 한 장씩을 구분하기 어렵다.

64일차에서는 별도의 UI 선택 상태에서 실제 손패 슬롯 인덱스를 저장한다.

예시:

- 손패 3번 슬롯 Pawn
- 손패 6번 슬롯 Pawn

두 카드가 같은 `PieceDefinition`을 사용하더라도 선택 상태는 다음처럼 구분된다.

`[2, 5]`

주요 동작은 다음과 같다.

- 최대 2개 슬롯 선택
- 같은 슬롯 재클릭 시 해당 슬롯만 선택 해제
- 세 번째 슬롯 선택 차단
- A/B 선택 슬롯 개별 제거
- 손패 수 변경 뒤 범위를 벗어난 선택 정리
- 합성 종료 또는 손패 변경 시 전체 선택 초기화

## Day64FusionUI

`Day64FusionUI`를 추가해 Battle Scene에서 카드·Fusion 통합 UI를 자동 생성하도록 했다.

Battle Scene 로드 후 별도 Inspector 연결 없이 런타임에서 자동 생성된다.

주요 연결 대상:

- `BoardInputController`
- `HandState`
- 기존 Fusion 규칙
- 기존 Fusion 발견 상태

`DefaultExecutionOrder(1200)`을 사용해 기존 전투 입력·UI가 준비된 뒤 동작하도록 구성했다.

## Fusion 모드 진입

화면의 Fusion 버튼은 기존 `BoardInputController.SetFusionModeActive()`를 사용한다.

합성 모드는 기존 규칙에 따라 배치 턴에서만 활성화할 수 있다.

합성 모드에 진입하면 일반 카드 소환과 구분되는 별도 카드 선택 오버레이가 활성화된다.

합성 모드 종료 시 선택된 Fusion 재료 상태는 초기화된다.

## 첫 번째 재료 선택

첫 재료를 선택하기 전에 현재 손패 안에서 실제로 합성 상대가 존재하는지 검사한다.

현재 손패의 다른 슬롯과 `EvaluateFusion()`을 실행해 정상 Recipe가 존재하는 카드만 첫 재료 후보로 허용한다.

따라서 현재 손패 구성상 어떤 카드와도 합성할 수 없는 카드는 Fusion 재료 선택 대상으로 사용되지 않는다.

## 두 번째 재료 후보 제한

첫 번째 재료가 선택되면 현재 손패를 다시 검사해 해당 카드와 실제로 합성 가능한 슬롯만 두 번째 재료 후보로 사용한다.

판정은 기존 `BoardInputController.EvaluateFusion()` 결과를 그대로 사용한다.

두 번째 재료 후보 조건:

- 첫 재료와 다른 실제 손패 슬롯
- Fusion 규칙 통과
- Recipe 존재
- 결과 카드 존재

이 구조를 통해 카드 한 장을 자기 자신과 두 번 선택하는 입력을 차단하면서, 같은 정의의 카드 두 장을 사용하는 특수 Recipe는 서로 다른 슬롯이라면 처리할 수 있다.

## A + B = C 미리보기

재료 두 장이 선택되면 Fusion 패널에서 현재 조합을 `A + B = C` 형태로 표시한다.

결과 영역에서는 다음 정보를 표시한다.

- 결과 카드 Artwork
- 결과 카드 이름
- 등급
- ATK
- HP
- 설명

확정 직전에도 다시 `EvaluateFusion()`을 실행해 손패나 전투 상태가 중간에 바뀐 경우 잘못된 합성이 실행되지 않도록 한다.

## 숨김 Recipe 처리

기존 `FusionDiscovery` 상태를 유지한다.

아직 발견되지 않은 숨김 Recipe는 합성 전 결과 정보를 공개하지 않는 기존 규칙을 유지하며, 실제 합성 성공 후 발견 상태가 기록된다.

이번 합성으로 새 Recipe가 발견된 경우 64일차 UI에서 Recipe 발견 알림을 표시한다.

발견 알림 유지 시간은 3초다.

## 합성 실행

합성 확정 시 별도의 카드 처리 로직을 새로 만들지 않고 기존 `BoardInputController.TryFuseCards()`를 호출한다.

따라서 기존 Fusion 시스템과 동일하게 다음 흐름으로 처리된다.

`재료 A + 재료 B → 기존 Fusion 규칙 재검증 → 재료 소모 → 결과 카드 손패 추가 → 보유 풀 갱신 → Recipe 발견 처리`

합성 성공 후 64일차 손패 슬롯 선택 상태만 초기화한다.

Fusion 모드 자체는 기존 시스템 상태를 유지하므로 같은 배치 턴 안에서 이어서 Fusion 조작을 수행할 수 있다.

결과 카드는 기존 손패에 즉시 추가되므로 같은 배치 턴의 기존 카드 배치 흐름을 그대로 사용할 수 있다.

## 기존 Fusion UI 중복 표시 정리

21~22일차의 기존 Fusion 시스템과 로직은 삭제하지 않는다.

64일차 정식 UI가 실행될 때 기존 `FusionPanelCanvas`의 화면 표시만 억제해 동일한 Fusion UI가 두 번 표시되지 않도록 구성했다.

이를 통해 기존 Fusion 클래스와 데이터 구조를 보존하면서 UI 계층만 64일차 버전으로 전환한다.

## FusionHandSelectionStateTests

손패 슬롯 기반 선택 상태의 회귀를 막기 위한 EditMode 테스트를 추가했다.

검증 항목:

- 서로 다른 손패 인덱스 두 개를 동시에 선택 가능
- 같은 슬롯을 다시 선택하면 해당 슬롯만 선택 해제
- 다른 선택 슬롯은 유지
- 재료 두 장 선택 이후 세 번째 슬롯 선택 차단

특히 첫 테스트는 같은 카드 정의를 사용하는 카드가 여러 장 존재하는 상황에서도 UI 선택 기반이 서로 다른 손패 슬롯을 유지할 수 있는 구조를 검증한다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Fusion/FusionHandSelectionState.cs`
- `Assets/ProjectEta/Scripts/Fusion/FusionHandSelectionState.cs.meta`
- `Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs.meta`
- `Assets/ProjectEta/Tests/EditMode/FusionHandSelectionStateTests.cs`
- `Assets/ProjectEta/Tests/EditMode/FusionHandSelectionStateTests.cs.meta`
- `Devlogs/Day64/README.md`

### 수정

없음.

### 삭제

없음.

## 결과

64일차 작업으로 기존 Fusion 시스템이 63일차 손패 UI 흐름과 연결되는 정식 조작 UI가 추가됐다.

플레이어는 배치 턴에서 Fusion 모드에 진입한 뒤 손패 카드 한 장을 선택하고, 실제로 조합 가능한 두 번째 카드만 골라 `A + B = C` 결과를 확인한 뒤 합성을 실행할 수 있다.

동일한 `PieceDefinition`이 여러 장 존재하는 손패 구조에서도 실제 손패 슬롯을 기준으로 선택 상태를 분리해 같은 종류의 카드 두 장을 독립적으로 선택할 수 있는 기반을 마련했다.

실제 카드 소모, 결과 생성, 보유 풀 갱신, 등급·수량 제한, 숨김 Recipe 발견은 기존 Fusion 로직을 그대로 재사용해 중복 구현을 피했다.

## 검증 상태

2026-09-08 기준 GitHub `main` 최신 커밋:

- SHA: `7bb320069cc6a07b615fd15b517f8d1401c40480`
- 메시지: `64`
- 부모 커밋: `32ed13d018b6237b6e714fc1980ccbf8f5f89be6`
- 63일차 대비 커밋 수: 1
- 63일차 대비 변경 파일: 6개
- 변경 형태: 신규 파일 6개, 기존 파일 수정·삭제 없음

GitHub 저장소에서 다음 내용을 확인했다.

- `FusionHandSelectionState`가 실제 손패 슬롯 인덱스를 최대 2개까지 저장
- 같은 슬롯 재클릭 선택 해제 처리 존재
- 세 번째 재료 선택 차단 처리 존재
- `Day64FusionUI`가 Battle Scene에서 자동 생성
- 기존 `BoardInputController.EvaluateFusion()`으로 후보와 확정 전 규칙 검증
- 기존 `BoardInputController.TryFuseCards()`로 실제 Fusion 실행
- 합성 성공 후 슬롯 선택 상태 초기화
- 숨김 Recipe 발견 상태와 발견 알림 연결
- 기존 Fusion Canvas 중복 표시 억제
- `FusionHandSelectionStateTests`에 슬롯 선택 관련 테스트 3개 존재

GitHub commit status에는 연결된 자동 CI/status check 결과가 현재 없다.

따라서 저장소 diff와 코드 구조상 명확한 충돌이나 누락은 확인되지 않았지만, Unity Editor에서의 실제 컴파일과 EditMode Test Runner 실행 성공 여부는 이 GitHub 상태만으로 확정할 수 없다.

## 다음 개발 연결

64일차에서 카드·Fusion UI 통합의 기본 조작 흐름을 정리했으므로 이후 일차에서는 현재 전투 UI 구조를 유지한 채 Reward / Shop / Event 등 다음 Run UI 흐름으로 확장할 수 있다.
