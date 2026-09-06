# 55일차 : MainMenu 정식 UI 및 이어하기 상태·확인 흐름 완성

## 개발 목표

54일차에서 구축한 `Boot → MainMenu → Battle` 실행 흐름을 유지하면서 개발용 MainMenu를 실제 게임에서 사용할 수 있는 정식 메뉴 형태로 정리한다.

55일차의 핵심은 새 게임·이어하기·영구 성장·설정·종료 기능을 다시 만드는 것이 아니라, 기존 기능을 하나의 일관된 메뉴 UI와 안전한 입력 흐름으로 통합하는 것이다.

## MainMenu 레이아웃 개편

기존 중앙 단일 패널 구조를 좌측 정보 영역과 우측 메뉴 영역으로 분리했다.

좌측 영역은 다음 정보를 담당한다.

- 프로젝트 제목과 장르 설명
- 이어하기 가능 여부
- 현재 Stage
- 현재 진행 Flow
- King HP
- 현재 Gold
- 방문 경로 수
- ESC 뒤로가기 안내

우측 영역은 다음 메뉴를 담당한다.

- 새 게임
- 이어하기
- 영구 성장
- 설정
- 게임 종료

메뉴 버튼은 일반·Hover·Pressed·Disabled 상태가 구분되도록 공통 스타일을 정리하고, 게임 종료처럼 위험한 동작은 별도의 강조 상태를 적용한다.

## 이어하기 상태 요약

`RunContinueInfo`를 추가해 MainMenu가 전체 `RunSaveData`를 직접 해석하지 않고 필요한 요약 정보만 받을 수 있도록 분리했다.

현재 MainMenu에 제공하는 정보는 다음과 같다.

- 현재 Stage
- 현재 RunFlowPhase
- Gold
- King HP
- 방문한 RouteMap 노드 수

`RunSaveSystem.CanContinue`는 `TryGetContinueInfo()`를 통해 안전한 저장 데이터인지 확인한 뒤 활성화된다.

`TryCreateContinueInfo()`는 기존 `IsContinueDataValid()` 검증을 그대로 사용하므로 기존 안전 체크포인트 규칙을 변경하지 않는다.

## 새 게임 확인 흐름

기존에는 새 게임 버튼을 누르면 Run Save를 바로 정리하고 Battle로 이동했다.

55일차부터는 이어하기 가능한 진행 데이터가 존재하면 즉시 새 게임을 시작하지 않고 확인 팝업을 먼저 표시한다.

진행 데이터가 없으면 기존과 동일하게 즉시 새 게임을 시작한다.

따라서 새 게임 흐름은 다음과 같다.

`새 게임 → 이어하기 데이터 확인 → 데이터 있음: 확인 팝업 / 데이터 없음: 즉시 Battle`

확정 시 기존 `SceneFlowController.StartNewRun()`을 사용하므로 Run Save만 제거하고 Meta Progress는 유지하는 54일차 규칙을 그대로 따른다.

## 게임 종료 확인

게임 종료 버튼에도 공통 확인 팝업을 연결했다.

사용자가 종료를 확정한 경우에만 기존 `SceneFlowController.QuitGame()`을 실행한다.

취소하거나 ESC를 누르면 MainMenu로 복귀한다.

## 메뉴 내비게이션 상태

UI 표시 상태를 `MainMenuNavigationState`로 분리했다.

현재 관리하는 패널 상태는 다음과 같다.

- Main
- Meta
- Settings
- NewGameConfirm
- QuitConfirm

`TryBack()`은 Main 화면에서는 입력을 소비하지 않고, 서브 패널 또는 확인 팝업에서는 Main으로 복귀한다.

이를 통해 `MainMenuController`가 패널 전환 조건을 직접 중복해서 판단하지 않도록 정리했다.

## ESC 뒤로가기

새 Input System의 `Keyboard.current`를 이용해 ESC 입력을 MainMenu 공통 뒤로가기로 연결했다.

ESC는 다음 화면에서 MainMenu로 복귀한다.

- 영구 성장
- 설정
- 새 게임 확인 팝업
- 게임 종료 확인 팝업

Main 화면에서는 ESC 입력을 별도 동작으로 소비하지 않는다.

## 영구 성장 패널

54일차에서 연결한 Meta Progress 기능은 유지하면서 MainMenu 전체 스타일에 맞게 레이아웃을 정리했다.

현재 기능은 그대로 유지한다.

- Meta Token 표시
- 현재 해금 상태 표시
- 해금 비용 표시
- 해금 가능 여부에 따른 버튼 활성화
- 해금 후 즉시 저장

영구 성장 자체의 본격적인 UI 확장은 계획된 58일차 작업으로 남긴다.

## 설정 패널

설정 패널은 정식 MainMenu 스타일에 맞는 진입·복귀 화면으로 정리했다.

55일차에서는 설정 기능 자체를 구현하지 않고 다음 작업을 위한 패널 구조만 유지한다.

- 해상도
- 전체화면 / 창모드
- UI Scale
- 설정 적용·취소·초기화
- 설정 저장·복원

실제 설정 기능은 56일차에서 구현한다.

## RunSaveSystem 확장

MainMenu가 안전 저장 데이터를 표시할 수 있도록 다음 API를 추가했다.

- `TryGetContinueInfo(out RunContinueInfo info)`
- `TryCreateContinueInfo(RunSaveData data, out RunContinueInfo info)`

`CanContinue`도 동일한 요약 생성 경로를 사용하도록 변경해 버튼 활성화 조건과 화면 표시 조건이 서로 다르게 판단되는 상황을 줄였다.

## Day55MainMenuTests

55일차 메뉴 흐름에 대한 EditMode 테스트 소스를 추가했다.

현재 테스트 항목은 다음과 같다.

- 이어하기 데이터가 있는 새 게임 요청은 확인 팝업 필요
- 이어하기 데이터가 없는 새 게임 요청은 즉시 시작 허용
- 서브 패널에서 뒤로가기를 사용하면 Main으로 복귀
- 유효한 Run Save에서 Stage·Flow·Gold·King HP·방문 노드 수 요약 생성

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Run/RunContinueInfo.cs`
- `Assets/ProjectEta/Scripts/UI/MainMenuNavigationState.cs`
- `Assets/ProjectEta/Tests/EditMode/Day55MainMenuTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Run/RunSaveSystem.cs`
- `Assets/ProjectEta/Scripts/UI/MainMenuController.cs`

### 삭제

없음.

## 결과

55일차 작업으로 54일차의 기능 중심 MainMenu가 정식 메뉴 구조에 가까운 화면으로 확장됐다.

새 게임은 기존 Run Save를 실수로 제거하지 않도록 확인 절차를 거치며, 이어하기는 단순 활성·비활성 표시를 넘어 현재 Stage·Flow·Gold·King HP·경로 진행 정보를 보여준다.

영구 성장과 설정 화면은 공통 메뉴 내비게이션에 포함됐고, 확인 팝업과 서브 패널은 ESC를 이용해 동일한 방식으로 MainMenu에 복귀한다.

56일차에서는 이 MainMenu 구조를 유지한 채 해상도·전체화면/창모드·UI Scale·설정 적용·취소·초기화·저장/복원 기능을 실제 설정 시스템으로 연결한다.

## 검증 상태

최신 55일차 커밋의 변경 파일과 소스 구조를 확인했다.

현재 GitHub commit status에는 연결된 CI 상태 검사가 등록되어 있지 않다.

따라서 GitHub에 올라온 소스와 테스트 코드의 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
