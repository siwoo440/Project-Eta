# 60일차 : King 선택 캐러셀 UI 및 슬라이드 전환 구현

## 개발 목표

기존 2×2 버튼 방식의 King 선택 화면을 가로 캐러셀 기반 정식 선택 UI로 교체한다.

60일차의 핵심 목표는 다음과 같다.

- 중앙 King 카드 1장을 크게 표시
- 좌우에 이전 / 다음 King 미리보기 카드 표시
- 화살표 버튼과 키보드 방향키로 King 탐색
- 카드 이동 시 가로 슬라이드 애니메이션 적용
- King 페이지 순환 구조
- 페이지 인디케이터 표시
- 하단에 현재 King 상세 정보 표시
- 잠긴 King도 미리보기 가능
- 잠긴 King 선택 확정 차단
- `이 King으로 시작` 버튼을 통한 명시적 확정
- 확정 후 기존 최초 King 배치 흐름으로 연결

## KingSelectionCarouselState

`KingSelectionCarouselState`를 추가해 UI 표시와 King 순환 상태를 분리했다.

King 순서는 다음과 같다.

1. Default
2. Attack
3. Defense
4. Strategy

현재 중앙 King의 인덱스와 페이지 번호를 관리한다.

`Move()`는 이전 / 다음 King으로 이동하고, `Peek()`는 현재 King 기준으로 좌우 상대 위치의 King을 조회한다.

처음에서 이전으로 이동하면 마지막 King으로 이동하고 마지막에서 다음으로 이동하면 다시 첫 King으로 돌아오는 순환 구조다.

## King 표시 정보 카탈로그

`KingSelectionPresentationCatalog`를 추가했다.

각 King의 선택 UI용 정보를 한 곳에서 제공한다.

현재 표시 정보는 다음과 같다.

- King 표시 이름
- 패시브 이름
- 패시브 상세 설명
- 플레이 스타일
- 필요한 영구 해금 ID

공격형 King은 `처형의 연쇄`, 방어형 King은 `왕의 요새`, 전략형 King은 `전술적 준비` 정보를 표시한다.

## 캐러셀 카드 구조

기존 네 개의 선택 버튼 대신 캐러셀 카드 슬롯을 사용하도록 `KingSelectionUI`를 변경했다.

실제 런타임에는 다섯 개의 카드 슬롯을 준비한다.

- 화면 밖 이전 준비 카드
- 이전 King 미리보기
- 현재 King 중앙 카드
- 다음 King 미리보기
- 화면 밖 다음 준비 카드

중앙 카드는 가장 크게 표시한다.

좌우 미리보기 카드는 중앙 카드보다 작고 반투명하게 표시해 다음에 이동할 King이 보이도록 한다.

화면 밖 슬롯은 다음 슬라이드에서 자연스럽게 들어올 수 있도록 미리 준비한다.

## 슬라이드 애니메이션

이전 / 다음 버튼을 누르면 카드 내용을 즉시 교체하지 않고 카드 자체가 가로로 이동한다.

애니메이션에서는 다음 값을 동시에 보간한다.

- RectTransform 위치
- 카드 크기
- CanvasGroup 투명도

슬라이드 시간은 약 `0.25초`다.

`Time.unscaledDeltaTime`을 사용해 TimeScale에 영향을 받지 않고 UI 애니메이션이 진행되도록 했다.

이동 중에는 이전 / 다음 버튼을 일시적으로 비활성화해 중복 입력을 막는다.

## 입력 방식

캐러셀은 다음 입력을 지원한다.

- 화면의 `◀` 이전 버튼
- 화면의 `▶` 다음 버튼
- 키보드 왼쪽 방향키
- 키보드 오른쪽 방향키

마지막 King에서 다음을 누르거나 첫 King에서 이전을 눌러도 막히지 않고 순환한다.

## 페이지 인디케이터

현재 King 위치를 점과 숫자로 함께 표시한다.

예시:

`●   ○   ○   ○     1 / 4`

페이지 이동 시 중앙 King과 함께 인디케이터가 갱신된다.

## 하단 King 상세 정보

현재 중앙 카드의 King 정보를 하단 상세 영역에 표시한다.

현재 표시 항목은 다음과 같다.

- King 이름
- 현재 Run King HP
- 패시브 이름
- 패시브 상세 설명
- 플레이 스타일
- 영구 해금 상태

상세 정보는 항상 중앙에 표시된 King을 기준으로 한다.

## 잠긴 King 표시

영구 해금되지 않은 King도 캐러셀에서는 숨기지 않는다.

잠긴 King은 카드에서 `잠김`으로 표시하고 카드 배경을 비활성 상태로 표현한다.

하단 상세 정보에서는 해당 King을 영구 성장에서 해금해야 한다는 안내를 표시한다.

잠긴 상태에서는 `이 King으로 시작` 버튼을 비활성화한다.

이를 통해 플레이어가 아직 해금하지 않은 King과 패시브를 미리 확인할 수 있다.

## King 선택 확정

60일차에서는 캐러셀에서 중앙 King을 보는 것과 실제 런 King 선택을 분리했다.

플레이어가 `이 King으로 시작` 버튼을 눌러야 현재 중앙 King을 실제 `KingRunState`에 확정한다.

선택 확정 전에는 전체 화면 King 선택 UI가 유지된다.

선택을 확정하면 캐러셀 화면을 닫고 다음 안내로 전환한다.

`보드에 King을 배치하세요`

이후 실제 King 배치는 기존 최초 배치 시스템이 담당한다.

## 기존 King 선택 조건 유지

King 선택 가능 구간 자체는 기존 규칙을 유지한다.

- 첫 Stage
- Battle 모드
- 최초 Deployment
- 최초 King 배치 전
- 전략형 배치 선택이 진행 중이지 않은 상태

따라서 60일차는 기존 게임 규칙을 바꾸지 않고 선택 Presentation과 확정 흐름을 정식화한다.

## 런 진행 중 King 상태 UI

King이 실제로 선택·배치된 뒤에는 기존 런 중 상태 표시 구조를 유지한다.

공격형 King은 현재 격노 스택을 표시한다.

방어형 King은 방벽 활성 상태를 표시한다.

전략형 King은 전술적 준비 상태를 표시한다.

이 전투 HUD는 61일차에서 더 정식화할 예정이다.

## 테스트 소스

`Day60KingSelectionCarouselTests`를 추가했다.

현재 포함한 검증 항목은 다음과 같다.

- 처음 / 마지막 페이지의 순환 이동
- 이전 / 현재 / 다음 King 미리보기 계산
- 전체 King 표시 정보 존재
- 좌우 미리보기 카드 구조
- 슬라이드 애니메이션 구현 연결
- `Time.unscaledDeltaTime` 사용
- 이전 / 다음 화살표 버튼
- King 선택 확정 상태
- 확정 후 King 배치 안내

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/King/KingSelectionCarouselState.cs`
- `Assets/ProjectEta/Scripts/King/KingSelectionPresentationCatalog.cs`
- `Assets/ProjectEta/Tests/EditMode/Day60KingSelectionCarouselTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/King/KingSelectionUI.cs`

### 삭제

없음.

## 결과

60일차 작업으로 King 선택 화면이 기존 2×2 개발용 버튼에서 중앙 강조형 가로 캐러셀 UI로 변경됐다.

현재 King은 중앙에 크게 표시되고 이전 / 다음 King은 양옆에서 작고 반투명한 카드로 미리 보인다.

화살표 버튼 또는 키보드 방향키를 누르면 카드가 좌우로 슬라이드하면서 다음 King이 중앙으로 이동한다.

King 선택 화면 하단에는 현재 중앙 King의 HP, 패시브, 상세 설명, 플레이 스타일과 해금 상태가 표시된다.

잠긴 King도 정보를 확인할 수 있지만 실제 선택 확정은 차단된다.

사용 가능한 King은 `이 King으로 시작` 버튼으로 확정한 뒤 기존 King 배치 흐름으로 진입한다.

## 검증 상태

최신 60일차 GitHub 커밋은 이전 59일차 커밋보다 1개 커밋 앞서 있으며 다음 변경 파일을 포함한다.

- `KingSelectionCarouselState.cs`
- `KingSelectionCarouselState.cs.meta`
- `KingSelectionPresentationCatalog.cs`
- `KingSelectionPresentationCatalog.cs.meta`
- `KingSelectionUI.cs`
- `Day60KingSelectionCarouselTests.cs`
- `Day60KingSelectionCarouselTests.cs.meta`

최신 커밋의 GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 GitHub에 올라온 변경 파일과 코드 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
