# 58일차 : 영구 성장 정식 UI 및 재사용 해금 패널 구현

## 개발 목표

48일차에서 구축한 Meta Progress 저장·해금 기능과 55일차 MainMenu의 개발용 영구 성장 화면을 실제 게임에서 사용할 수 있는 정식 UI로 통합한다.

58일차의 핵심 목표는 다음과 같다.

- 영구 성장 전용 재사용 패널 구현
- 전체 / 기물 / 킹 / 패시브 카테고리 필터
- 해금 목록과 상세 정보 분리
- 해금 가능 / 토큰 부족 / 해금 완료 상태 표시
- 해금 확인 팝업
- 해금 직후 Meta Token 및 저장 상태 갱신
- MainMenu의 기존 개발용 영구 성장 UI 교체
- 런 결과 화면에서도 동일한 영구 성장 패널 재사용
- 해금 정의에 상세 설명 추가
- 실제 런 콘텐츠 풀 반영은 59일차 범위로 유지

## 영구 성장 패널 상태

`MetaProgressPanelState`를 추가해 UI 표시 상태를 화면 코드와 분리했다.

현재 카테고리는 다음과 같다.

- All
- Piece
- King
- Passive

카테고리 변경 시 현재 목록을 `MetaUnlockCatalog` 기준으로 필터링한다.

현재 선택한 해금 항목의 ID도 상태 객체에서 관리한다.

## 해금 표시 상태

영구 해금 항목의 UI 상태를 `MetaUnlockDisplayState`로 분리했다.

- Locked
- Available
- Insufficient
- Unlocked

현재 프로토타입에는 별도의 선행 해금 조건이 없기 때문에 정상 정의는 주로 Available, Insufficient, Unlocked 상태를 사용한다.

`MetaProgressPanelState.Evaluate()`가 현재 Meta Progress와 해금 정의를 기준으로 표시 상태를 계산한다.

## 영구 해금 정의 확장

`MetaUnlockDefinition`에 `Description` 필드를 추가했다.

기존 필드는 그대로 유지한다.

- UnlockId
- DisplayName
- UnlockType
- Cost

여기에 상세 설명을 추가해 영구 성장 UI의 상세 정보 영역에서 실제 해금 내용을 설명할 수 있도록 했다.

현재 카탈로그에는 다음 해금 항목이 있다.

- 신규 기물 슬롯
- 신규 패시브 슬롯
- 공격형 킹
- 방어형 킹
- 전략형 킹

각 항목에 UI용 설명문을 추가했다.

## MetaProgressPanelController

정식 영구 성장 화면을 담당하는 `MetaProgressPanelController`를 추가했다.

화면은 크게 세 영역으로 구성한다.

### 카테고리 영역

좌측에서 다음 카테고리를 선택한다.

- 전체
- 기물
- 킹
- 패시브

선택된 카테고리는 강조 표시한다.

### 해금 목록 영역

현재 카테고리에 포함된 `MetaUnlockDefinition`을 기준으로 해금 카드를 동적으로 생성한다.

카드에는 다음 정보를 표시한다.

- 해금 이름
- 현재 상태
- Meta Token 비용

해금 상태에 따라 카드의 표시 색상을 구분한다.

### 상세 정보 영역

목록에서 항목을 선택하면 다음 정보를 표시한다.

- 해금 타입
- 이름
- 상세 설명
- 비용
- 현재 상태
- 해금 버튼

토큰이 부족하면 부족한 Meta Token 수를 버튼에 표시한다.

이미 해금된 항목은 `해금 완료` 상태로 표시하고 버튼을 비활성화한다.

## 해금 확인 흐름

해금 가능한 항목의 `해금하기` 버튼을 누르면 바로 토큰을 소비하지 않고 확인 팝업을 표시한다.

확인 팝업에는 다음 내용을 표시한다.

- 선택한 해금 이름
- 소모 Meta Token
- 즉시 영구 저장된다는 안내

사용자가 확정하면 기존 `MetaUnlockService.TryUnlock()`을 사용한다.

해금 성공 후에는 `MetaProgressService.Save()`를 호출해 토큰 차감과 해금 결과를 즉시 저장한다.

이후 토큰 잔액, 해금 수, 목록 카드, 상세 상태를 다시 갱신한다.

## MainMenu 영구 성장 연결

`MainMenuMetaProgressBridge`를 추가했다.

55일차 MainMenu가 생성하는 기존 `MetaRoot`를 그대로 사용한다.

브리지는 다음 흐름으로 동작한다.

1. MainMenuController와 MetaRoot 생성 대기
2. 기존 55일차 개발용 영구 성장 UI 숨김
3. MetaRoot에 `MetaProgressPanelController` 추가
4. 기존 MainMenu 뒤로가기 흐름과 연결

따라서 별도 Meta Scene을 만들지 않고 기존 MainMenu Scene 구조를 그대로 유지한다.

## SceneRuntimeBootstrap 확장

MainMenu 진입 시 다음 런타임 컴포넌트를 추가하도록 기존 부트스트랩을 확장했다.

- MainMenuController
- MainMenuSettingsBridge
- MainMenuMetaProgressBridge

이를 통해 Boot에서 MainMenu로 진입한 경우에도 영구 성장 패널이 자동 연결된다.

## 런 결과 UI 역할 분리

기존 `MetaProgressUI`는 런 결과와 영구 해금 구매를 같은 화면에서 직접 처리했다.

58일차에서는 역할을 분리했다.

런 결과 화면에서는 다음 내용만 우선 표시한다.

- 런 클리어 / 런 종료
- 도달 Stage
- 이번 런 획득 Meta Token
- 현재 총 Meta Token

그리고 `영구 성장 보기` 버튼을 통해 재사용 `MetaProgressPanelController`로 이동한다.

영구 성장 패널에서 뒤로가기를 누르면 다시 런 결과 화면으로 돌아온다.

이를 통해 MainMenu와 런 종료 화면이 동일한 영구 성장 UI를 사용한다.

## 기존 Meta Progress 기능 유지

58일차는 기존 영구 성장 핵심 로직을 새로 만들지 않는다.

다음 기존 구조를 그대로 사용한다.

- MetaProgressState
- MetaProgressService
- MetaProgressSaveService
- MetaUnlockService
- MetaUnlockCatalog

기존 Meta Token 소비, 중복 해금 차단, 저장 기능을 유지하면서 화면 계층만 정식화했다.

## 59일차와의 범위 구분

58일차에서 구매한 해금은 Meta Progress 저장 데이터에는 즉시 반영된다.

하지만 이번 작업에서는 해당 해금이 실제 다음 런의 콘텐츠 풀을 변경하도록 연결하지 않는다.

다음 항목은 59일차 범위다.

- 해금 기물을 시작/보상 카드 Pool에 반영
- 해금 King을 실제 새 런 선택 목록에 반영
- 해금 Passive를 실제 런 Passive Pool에 반영

58일차는 영구 성장 UI와 구매·저장 흐름까지만 담당한다.

## 테스트 소스

`Day58MetaProgressPanelTests`를 추가했다.

현재 포함한 검증 항목은 다음과 같다.

- 모든 영구 해금 정의에 상세 설명 존재
- King 카테고리 필터 결과
- 토큰 부족 / 해금 가능 / 해금 완료 표시 상태
- 전체 카테고리에서 전체 카탈로그 반환
- 런 결과 UI의 재사용 MetaProgressPanelController 연결
- SceneRuntimeBootstrap의 MainMenuMetaProgressBridge 주입

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Meta/MetaProgressPanelState.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs`
- `Assets/ProjectEta/Scripts/Meta/MainMenuMetaProgressBridge.cs`
- `Assets/ProjectEta/Tests/EditMode/Day58MetaProgressPanelTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Meta/MetaUnlockDefinition.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`

### 삭제

없음.

## 결과

58일차 작업으로 영구 성장 기능이 MainMenu의 개발용 버튼 목록에서 카테고리·목록·상세 정보가 분리된 정식 화면 구조로 변경됐다.

플레이어는 전체 / 기물 / 킹 / 패시브 영역을 필터링해 확인할 수 있으며, 현재 토큰과 해금 상태에 따라 해금 가능 여부를 확인하고 확인 팝업을 거쳐 영구 해금할 수 있다.

해금 결과는 즉시 Meta Progress에 저장된다.

또한 런 종료 화면과 MainMenu가 동일한 영구 성장 패널을 재사용하도록 구성해 영구 성장 UI의 중복 구현을 줄였다.

59일차에서는 58일차에서 저장된 해금 상태를 실제 새 런의 기물·King·Passive 콘텐츠 풀에 반영한다.

## 검증 상태

최신 58일차 GitHub 커밋에 `MainMenuMetaProgressBridge`, `MetaProgressPanelController`, `MetaProgressPanelState`, 확장된 `MetaUnlockDefinition`, 변경된 `MetaProgressUI`, `SceneRuntimeBootstrap`, `Day58MetaProgressPanelTests`가 포함된 것을 확인했다.

GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 최신 커밋의 변경 파일과 코드 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
