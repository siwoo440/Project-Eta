# 83일차: Battle 런타임 초기화 통합 및 손패 위치 수정

---

## 개발 목표

Battle 씬에 분산된 자동 생성 진입점을 중앙 부트스트랩으로 통합하고, F1 패널에서 런타임 관리자 누락과 중복을 확인할 수 있게 수정했다.

중앙 초기화 적용 뒤 확인된 손패 카드의 상단 이동 문제를 레이아웃 좌표 기준에 맞게 수정했다.

---

## 작업 내용

- `SceneRuntimeBootstrap`의 Battle 초기화를 핵심 전투·전투 기능·런 진행·UI 연출 단계로 분리 수정
- Battle 전용 `AutoCreateForBattleScene` 진입점 29개 삭제
- 킹 선택 UI와 전투 알림 UI의 복합 호스트 생성 경로 추가
- 필수 런타임 관리자 32개의 존재 수를 수집하는 진단 구조 추가
- 관리자 누락·중복·초기화 소요 시간을 표시하는 F1 상태 영역 추가
- Boot·MainMenu 진입 시 이전 Battle 진단 정보를 제거하도록 수정
- Unity `HorizontalLayoutGroup`의 상단 앵커 좌표를 반영하도록 손패 기본 Y 계산 수정
- 손패 하단 정렬 좌표를 검증하는 회귀 테스트 추가
- Battle 중앙 초기화와 관리자 진단 집계를 검증하는 EditMode 테스트 추가

---

## 초기화 구조

Battle 씬 진입 시 다음 순서로 런타임 객체를 준비한다.

| 단계 | 주요 대상 |
| --- | --- |
| 핵심 전투 | `BattleController`, `RoundRuntimeController`, `RoundStateBattleBridge`, 시작 덱 확장 |
| 전투 기능 | 전투방, 보스, 적 AI, 킹 능력, 턴 지연, 치명 공격 연출 |
| 런 진행 | 경로 지도, 스테이지 전환, 보상, 상점·이벤트, 저장, 메타 결과 |
| UI·연출 | Battle HUD, 전투 안내, 합성 UI, 카드 모션, 전투 알림, 튜토리얼, Toast |

전역 설정, Steam, 정적 상태 초기화처럼 Battle 씬 수명 주기와 분리된 진입점은 기존 구조를 유지했다.

---

## F1 런타임 진단

F1 상태 페이지에서 다음 정보를 확인할 수 있다.

- 초기화 정상 여부 표시 추가
- 준비된 관리자 수와 필수 관리자 수 표시 추가
- 누락된 관리자 이름 표시 추가
- 중복된 관리자 이름과 개수 표시 추가
- Battle 초기화 소요 시간 표시 추가

일회성 작업 뒤 제거되는 `PrototypePlayerDeck26Bootstrap`은 지속 관리자 집계에서 제외했다.

---

## 손패 위치 수정

`HorizontalLayoutGroup`은 자식 카드를 부모 상단 앵커 기준의 음수 Y 좌표로 배치한다.

기존 `ResolveLayoutBaseY()`는 부모 높이 270px을 빼지 않고 하단 기준 양수 값을 반환해, 카드가 약 270px 위로 이동했다.

부모 레이아웃 높이를 기준 좌표에서 차감하도록 수정해 대기 손패를 화면 하단으로 복원했다. Hover와 합성 재료 선택 카드의 상승 연출은 유지했다.

---

## 검증 결과

- Battle 중앙 관리 대상 29개 등록 확인
- 개별 `AutoCreateForBattleScene` 잔여 0개 확인
- `ProjectEta.Runtime.csproj` 컴파일 경고 0개·오류 0개 확인
- `ProjectEta.Tests.EditMode.csproj` 컴파일 경고 0개·오류 0개 확인
- Unity Asset Pipeline Refresh 이후 C# 컴파일 오류 없음 확인
- `git diff --check` 공백 오류 없음 확인
- 원본 프로젝트가 Unity 에디터에서 열려 있어 CLI 전체 EditMode 테스트 미실행

---

## 주요 변경 파일

- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs` 수정
- `Assets/ProjectEta/Scripts/SceneFlow/BattleRuntimeDiagnostics.cs` 추가
- `Assets/ProjectEta/Scripts/Debug/ProjectEtaDebugWindow.cs` 수정
- Battle 전용 자동 생성 스크립트 29개 수정
- `Assets/ProjectEta/Scripts/UI/Day66HandCardMotionController.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day83RuntimeBootstrapTests.cs` 추가
- `Assets/ProjectEta/Tests/EditMode/Day66CardLiftStateTests.cs` 수정

---

## 다음 개발 방향

84일차에는 1~5성 합성 트리와 기물 등급별 성장 규칙을 데이터 중심 구조로 정리한다.

합성 결과, 카드 보유 상태, 전투 기물 생성이 같은 등급 규칙을 사용하도록 통합하고 3성 이상 기물 콘텐츠를 확장할 기반을 만든다.
