# 47일차 : 판 위 상점·이벤트 및 런 전용 경제·월드 UI 구축

## 개발 목표

46일차까지 실제 기능이 연결된 Battle·Elite·Reward 노드에 이어 `Shop`과 `Event` 노드를 실제 플레이 가능한 비전투 스테이지로 확장한다.

별도 화면이나 씬으로 전환하지 않고 기존 10×10 체스판 위에 돗자리와 World Space UI를 펼치는 방식으로 상점과 이벤트를 구성하고, 선택 결과를 현재 `RunState`에 반영한 뒤 다시 같은 경로 지도로 복귀하도록 한다.

상점 재화는 48일차의 영구 성장용 메타 재화와 분리해 현재 런에서만 사용하는 임시 경제 상태로 관리한다.

## 주요 개발 내용

### RunEconomyState

현재 런에서만 사용하는 상점 전용 경제 상태를 추가했다.

프로토타입 시작 재화는 100 Gold로 설정했다.

현재 상점 가격은 다음과 같다.

- 카드 구매: 30 Gold
- 카드 제거: 40 Gold
- King HP +1: 20 Gold
- 카드 업그레이드: 50 Gold

위 재화는 메타 성장용 통화와 분리되어 있으며 현재 런이 유지되는 동안만 사용한다.

### StageActivityController

`Shop`과 `Event` 흐름을 실제 시스템에 연결하는 `StageActivityController`를 추가했다.

Battle 씬에서 런타임에 자동 생성되며 별도의 Scene 또는 Inspector 설정 없이 현재 `RunState`, `BoardView`, `RouteMapBoardController`를 찾아 연결한다.

상점 또는 이벤트 노드에 진입하면 기존 Placeholder 화면을 숨기고 실제 판 위 UI를 표시한다.

비전투 스테이지 종료 시 현재 스테이지를 완료 상태로 기록하고 `RouteMapState`에 다음 깊이의 경로를 생성한 뒤 Map 흐름으로 복귀한다.

### 상점 기능

상점에서는 다음 기능을 제공한다.

#### 카드 구매

46일차의 `CardRewardGenerator`와 `CardRewardRules`를 재사용해 획득 가능한 카드 후보를 생성한다.

구매 성공 시 Gold를 차감하고 `DeckState.AddToOwnedPool()`을 통해 선택 카드를 현재 런 카드 풀에 추가한다.

#### 카드 제거

현재 `OwnedCardPool`의 플레이어 카드를 선택해 Gold를 지불하고 제거할 수 있다.

King과 Monster·Boss 계열은 제거 대상으로 사용하지 않는다.

#### King HP 회복

Gold를 지불해 현재 King HP를 1 회복한다.

47일차 프로토타입 기준 King 최대 HP는 3으로 설정했다.

#### 카드 업그레이드

카드를 선택하고 Gold를 지불하면 해당 카드의 HP와 ATK를 각각 1 증가시킨다.

원본 `PieceDefinition` 에셋은 직접 수정하지 않고 런타임 복제본을 생성한 뒤 `OwnedCardPool`의 해당 카드와 교체한다.

따라서 원본 프로젝트 에셋을 변경하지 않으면서 현재 런에서만 강화 결과를 유지한다.

### StageChoiceResult

상점과 이벤트에서 발생하는 선택 결과를 공통 구조로 표현하도록 `StageChoiceResult`를 추가했다.

현재 결과 타입은 다음과 같다.

- 변화 없음
- Gold 변화
- King HP 변화
- 카드 획득
- 카드 제거
- 카드 업그레이드
- 복합 결과

이를 통해 Shop과 Event가 서로 다른 화면을 사용하더라도 결과 적용과 로그 구조를 공유할 수 있도록 했다.

### 이벤트 시스템

깊이에 따라 순환하는 프로토타입 이벤트 3종을 추가했다.

#### 버려진 카드 꾸러미

카드 후보 중 한 장을 무료로 획득한다.

46일차 카드 보상 후보 생성 규칙을 재사용한다.

#### 조용한 휴식처

King HP를 1 회복한다.

최대 HP에 도달한 경우 회복 선택지를 사용할 수 없다.

#### 위험한 계약

King HP 1을 지불하고 다음 보상을 받는다.

- Gold +60
- 카드 후보 중 한 장 획득

King HP가 1일 경우 즉사를 막기 위해 선택할 수 없다.

### 판 위 돗자리형 Shop/Event UI

`StageBoardOverlayUI`를 추가해 상점과 이벤트가 기존 체스판 위에 직접 펼쳐지도록 구성했다.

기존 판을 제거하거나 별도 씬으로 이동하지 않고 `BoardView` 위에 얇은 Plane과 장식을 런타임으로 생성한다.

상점은 어두운 갈색 계열, 이벤트는 보라 계열로 구분한다.

Shop/Event 진입 중에는 기존 RouteMap의 노드·킹 표시를 임시로 숨기고 종료 시 다시 복원한다.

### 1번 카메라 강제 고정

Shop과 Event 중 UI의 방향과 가독성이 카메라 전환에 따라 달라지는 문제를 줄이기 위해 `StageActivityCameraLock`을 추가했다.

비전투 스테이지에 진입하면 현재 카메라 위치와 회전을 저장한 뒤 Battle 씬의 1번 카메라 기준 위치와 각도로 강제 고정한다.

프로토타입 기준은 다음과 같다.

- Position: `(0, 9, -9)` 기준
- Rotation: 약 45도 하향 시점

기존 W/S 카메라 조작 컴포넌트도 Shop/Event 동안 잠그고, 비전투 스테이지를 종료하면 진입 전 카메라 위치·회전과 입력 상태를 복원한다.

### World Space UI 방향 보정

초기 World Space Canvas는 앞면 방향을 잘못 계산해 글자가 좌우 반전되어 보이는 문제가 있었다.

Unity World Space UI의 실제 보이는 앞면인 로컬 `-Z`가 플레이어 카메라를 향하도록 회전 계산을 수정했다.

이후 1번 카메라 고정 구조와 결합해 카메라에 따라 계속 회전을 계산하지 않고 정해진 시점에 맞는 고정 배치를 사용하도록 단순화했다.

### 카드·팻말형 UI 재구성

초기 상점 UI는 하나의 큰 직사각형 패널 안에 모든 버튼과 설명이 들어가 있어 월드 오브젝트보다 일반 메뉴 UI처럼 보였다.

이를 판 위 실제 물건에 가까운 구조로 재구성했다.

현재 구성은 다음과 같다.

- 상단 작은 제목 팻말
- Gold·King HP 상태 팻말
- 개별 카드 형태의 선택지
- 플레이어 쪽 하단의 나가기·뒤로 카드
- 중앙 돗자리와 외곽 장식

카드 선택지는 각각 독립된 물건처럼 보이도록 약간씩 회전을 다르게 배치했다.

### UI 가독성 개선

1번 카메라에서 선택지가 작게 보이는 문제를 수정했다.

World Space Canvas 전체 크기, 제목 팻말, 상태 팻말, 선택 카드 크기와 글자를 확대했다.

UI 전체 위치도 플레이어 쪽으로 내려 카메라에 더 가까운 위치에 배치했다.

### 마우스 오버 설명

카드 내부에 제목과 상세 설명을 모두 표시하면 글자가 작아지고 복잡해지는 문제를 해결하기 위해 `StageOverlayHoverRelay`를 추가했다.

현재 선택 카드 안에는 `카드 구매`, `카드 제거`, `킹 HP 회복`, `카드 업그레이드`, `상점 나가기` 같은 핵심 제목만 크게 표시한다.

상세 설명과 비용은 카드를 가리킬 때 화면 하단의 큰 흰색 글자로 표시한다.

마우스가 카드에서 벗어나면 설명은 자동으로 사라진다.

### 카드 내부 설명 제거

최종 UI 수정에서 5개의 상점 메인 선택지 모두 카드 내부의 작은 설명을 제거했다.

카드 전체 공간을 선택지 제목이 사용하도록 변경하고 중앙에 큰 글자로 표시한다.

상세 정보는 하단 Hover 설명으로만 제공해 카드 자체의 가독성을 높였다.

### Object 이름 충돌 수정

`StageBoardOverlayUI`에서 `StringComparison` 사용을 위해 `using System`을 추가하면서 축약형 `Object`가 `System.Object`와 `UnityEngine.Object` 사이에서 모호해지는 `CS0104` 오류가 발생했다.

`using System`을 제거하고 실제 필요한 위치만 `System.StringComparison.Ordinal`로 완전 수식해 해결했다.

기존 `Object.FindFirstObjectByType()`와 `Object.Destroy()` 호출은 다시 `UnityEngine.Object`로 정상 해석된다.

### 회귀 테스트

47일차 기능 검증을 위해 EditMode 테스트를 추가했다.

주요 확인 항목은 다음과 같다.

- 새 런의 시작 Gold
- 보유 Gold보다 큰 비용 지불 차단
- 런타임 카드 업그레이드 시 HP·ATK +1
- 원본 `PieceDefinition` 에셋 미변경
- King 업그레이드 차단
- 깊이에 따른 이벤트 타입 생성
- 공통 `StageChoiceResult` 값 보존
- 1번 카메라 위치·회전 계산
- World Space UI 앞면 방향
- Hover 진입·이탈 콜백 동작

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/RunEconomyState.cs`
- `Assets/ProjectEta/Scripts/Run/RuntimeCardUpgradeService.cs`
- `Assets/ProjectEta/Scripts/Run/StageActivityController.cs`
- `Assets/ProjectEta/Scripts/Run/StageChoiceResult.cs`
- `Assets/ProjectEta/Scripts/Run/StageEventGenerator.cs`
- `Assets/ProjectEta/Scripts/UI/StageActivityCameraLock.cs`
- `Assets/ProjectEta/Scripts/UI/StageBoardOverlayUI.cs`
- `Assets/ProjectEta/Scripts/UI/StageOverlayOption.cs`
- `Assets/ProjectEta/Scripts/UI/StageOverlayHoverRelay.cs`
- `Assets/ProjectEta/Tests/EditMode/Day47ShopEventTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day47StageActivityCameraTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day47StageOverlayHoverTests.cs`

## 결과

45일차부터 Placeholder로 남아 있던 Shop과 Event가 실제 비전투 스테이지로 동작하게 됐다.

상점에서는 현재 런 전용 Gold를 이용해 카드 구매·제거·King HP 회복·카드 업그레이드를 수행할 수 있다.

이벤트에서는 카드 획득, 회복, 위험과 보상의 선택 구조를 사용할 수 있다.

상점과 이벤트 모두 별도 화면으로 전환하지 않고 기존 10×10 체스판 위에 월드 오브젝트처럼 배치되며, 1번 카메라를 고정해 일관된 시점에서 선택할 수 있다.

UI는 대형 패널 방식에서 카드·팻말 중심 구조로 변경됐고, 카드 안에는 큰 제목만 표시하며 자세한 내용은 마우스 오버 시 하단 흰색 설명으로 분리했다.

비전투 선택이 끝나면 현재 결과가 `RunState`에 반영되고 다시 같은 경로 지도에 복귀하는 흐름이 완성됐다.

다음 48일차에서는 현재 런 전용 Gold와 분리된 메타 토큰 및 런 밖 영구 성장 구조를 구현한다.
