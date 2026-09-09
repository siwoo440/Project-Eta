# 66일차 : RouteMap UI 정식화·노드 건물 시각화 및 Battle↔Map 전환 연출 개선

## 개발 목표

66일차에서는 기존 RouteMap 진행 로직을 유지하면서 전투 종료 후 지도 화면을 실제 게임 진행 화면에 가깝게 정식화했다.

이번 일차의 중심은 다음과 같다.

- RouteMap 전용 HUD 정식화
- 현재 Stage 진행도와 다음 스테이지 선택 안내 표시
- StageType 범례 추가
- 선택 가능한 노드 Hover 상세 정보 표시
- RouteMap 전체 노드의 StageType별 3D 건물 시각화
- 일반 전투·Elite·Reward·Shop·Event·Boss 건물 형태 구분
- Map·Reward·Shop·Event 상태에 따른 지도 건물 표시 규칙 정리
- Battle↔Map 전환 시 기물·지도 오브젝트 상승/낙하 연출 추가
- 전환 중 중복 입력 차단
- 손패 카드 Hover·Fusion 선택 상태에 따른 상하 모션 추가
- Map 전환 뒤 BossHealthUI가 남는 상황을 막기 위한 표시 Guard 추가
- 지도 표현 규칙과 전환 계산에 대한 EditMode 회귀 테스트 추가

66일차에서는 RouteMap의 노드 선택, King 이동, StageDefinition 진입 같은 기존 진행 로직을 다시 작성하지 않고 표현 계층을 중심으로 확장했다.

## 기존 RouteMap 진행 로직 유지

기존 시스템의 다음 기능은 그대로 사용한다.

- `RouteMapBoardController`의 Map 모드 진입·종료
- 현재 노드와 선택 가능한 다음 노드 표시
- RouteMap 노드 클릭 처리
- Map King 이동 연출
- `StageNodeSelected` 이벤트를 통한 실제 Stage 진입
- `FullRouteMapPreviewController`의 미래 경로 표시
- `RunState.RouteMap`의 전체 노드·방문·선택 상태
- `StageDefinitionCatalog`의 StageType 판정

따라서 이번 작업은 경로 선택 규칙을 변경하는 작업이 아니라 기존 경로 데이터를 더 명확하게 보여 주는 UI·연출 정식화 작업이다.

## Day66RouteMapUI

`Day66RouteMapUI`를 추가해 RouteMap 전용 Screen Space HUD를 구성했다.

Battle Scene 로드 후 별도 Inspector 연결 없이 자동 생성된다.

상단 HUD에서 다음 정보를 표시한다.

- `ROUTE MAP`
- 현재 Stage 깊이
- 최종 Stage 수
- `다음 스테이지를 선택하세요` 안내
- StageType별 건물 범례

범례는 다음 타입을 구분한다.

- BATTLE
- ELITE
- REWARD
- SHOP
- EVENT
- BOSS

HUD는 실제 `RunFlowPhase.Map`에서만 표시되며 Reward·Shop·Event 등 다른 흐름에서는 자동으로 숨겨진다.

## RouteMap Hover 정보 패널

선택 가능한 노드에 마우스를 올리면 우측 Hover 상세 패널을 표시한다.

표시 항목:

- StageType
- Stage 표시 이름
- Depth
- StageType별 간단 설명
- 현재 선택 가능 상태

Hover 판정은 기존 `RouteMapNodeView`가 붙어 있는 실제 선택 가능 노드 Collider를 Raycast해 처리한다.

지도 HUD와 Hover 패널의 배경·텍스트는 `raycastTarget`을 끄고 기존 RouteMap 노드 클릭을 방해하지 않도록 구성했다.

## RouteMap 전체 노드 건물 시각화

기존 원형 노드만으로 경로를 구분하던 구조에 StageType별 3D 건물 표현을 추가했다.

관련 파일:

- `Day66RouteBuildingPlan.cs`
- `Day66RouteNodeBuildingDecorator.cs`
- `Day66RouteNodeBuildingModel.cs`
- `RouteNodeBuildingStyle.cs`

`Day66RouteBuildingPlan`은 `RouteMapState.Nodes` 전체를 읽고 각 노드의 다음 정보를 건물 생성 사양으로 변환한다.

- NodeId
- Position
- Depth
- StageType

StageDefinitionId 파싱에 실패한 경우 일반 Battle 타입을 fallback으로 사용한다.

## StageType별 건물 구분

각 RouteMap 노드는 StageType에 따라 다른 형태의 건물 모델을 사용하도록 구성했다.

주요 구분 대상:

- 일반 Battle 성채
- Elite 요새
- Reward 보물고
- Shop 상점
- Event 마법탑
- Mid Boss 성채
- Final Boss 성채

건물은 보드 타일 좌표를 그대로 사용해 RouteMap 노드와 같은 위치에 배치된다.

기단과 건물 본체를 분리해 노드 상태 신호와 건물 형태를 동시에 표현할 수 있도록 구성했다.

## Day66MapPresentationRules

지도와 보스 UI의 표시 조건을 한곳에서 판정하기 위해 `Day66MapPresentationRules`를 추가했다.

지도 건물 표시 규칙:

- BoardMode가 Map이 아니면 숨김
- Shop에서는 숨김
- Event에서는 숨김
- Map에서는 표시
- Reward에서는 지도 배경으로 유지

BossHealthUI 표시 규칙:

- `BoardMode.Battle`
- `RunFlowPhase.Battle`

두 조건이 모두 만족될 때만 보스 체력 UI를 허용한다.

## Day66BossHealthPresentationGuard

Map·Reward·Shop·Event로 전환된 뒤 이전 보스 체력 UI가 남는 상황을 억제하기 위해 별도 Guard를 추가했다.

Guard는 LateUpdate에서 현재 RunState를 확인하고 Battle 화면이 아닌 경우 `BossHealthUI.Hide()`를 호출한다.

이번 일차 종료 시점에는 이 Guard를 통해 표시 계층을 방어하고 있으며, 강제 승리 디버그 처리 자체가 Boss 사망 상태를 만들지 않는 구조적 문제는 별도의 후속 수정 대상으로 남아 있다.

## Battle↔Map 전환 연출

`Day66BoardTransitionFX`를 추가해 같은 Battle Scene 안에서 BoardMode가 변경될 때 화면 전환 연출을 적용했다.

지원 전환:

- Battle → Map
- Map → Battle

전환 시 기존 시각 오브젝트를 직접 이동시키는 대신 렌더링용 Proxy를 만들어 연출한다.

기본 흐름:

`기존 오브젝트 수집 → Proxy 생성 → 원본 Renderer 임시 숨김 → 위로 상승 → 새 상태 시각 탐색 → 새 Proxy 낙하 → 실제 시각 복원`

Battle 상태에서는 활성 `PieceView`를 대상으로 하고 Map 상태에서는 RouteMap 건물과 Map King을 대상으로 한다.

## 전환 중 입력 차단

전환 연출 도중 지도 노드나 전투 UI가 중복 입력되는 것을 막기 위해 투명 Screen Space 입력 차단 Canvas를 사용한다.

새 상태 시각이 준비되거나 전환 대기 시간이 종료되면 입력 차단을 해제한다.

전환 시간과 순차 재생 계산은 `Day66TransitionMotion`으로 분리했다.

## 손패 카드 상하 모션

`Day66HandCardMotionController`와 `Day66CardLiftState`를 추가했다.

손패 카드의 상태에 따라 Y 오프셋을 계산하고 `Mathf.MoveTowards`로 부드럽게 이동한다.

판정 대상:

- 기본 대기 상태
- Hover 상태
- 클릭 유지 상태
- Fusion 재료 선택 상태
- 상호작용 불가 상태

Fusion 모드에서는 `BoardInputController.FusionMaterials`에 포함된 카드를 별도 선택 상태로 인식한다.

기존 HandUI의 카드 생성·선택 로직은 변경하지 않고 표시 모션만 별도 컨트롤러에서 처리한다.

## 전체 RouteMap 표시 정리

`Day66RouteMapUI`는 실제 Map 선택 단계에서 `RouteMapFullGraph_Day52` 전체 미래 경로를 표시하고 다른 Stage Activity 흐름에서는 겹침을 막기 위해 숨긴다.

이를 통해 Reward·Shop·Event UI와 미래 경로 Preview가 동시에 표시되는 상황을 줄이도록 구성했다.

## EditMode 회귀 테스트

66일차 표현 계층을 확인하기 위한 EditMode 테스트 파일을 추가했다.

테스트 파일:

- `Day66CardLiftStateTests.cs`
- `Day66PresentationRegressionTests.cs`
- `Day66RouteBuildingPlanTests.cs`
- `Day66TransitionMotionTests.cs`
- `RouteNodeBuildingStyleTests.cs`

주요 검증 대상:

- 카드 상태별 목표 Y 오프셋
- Map·Reward·Shop·Event별 지도 건물 표시 규칙
- Battle 외 BossHealthUI 표시 차단 규칙
- RouteMap 전체 노드의 건물 계획 생성
- StageType별 건물 스타일 판정
- Battle↔Map 전환 순차 시간 계산

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Board/Day66MapPresentationRules.cs`
- `Assets/ProjectEta/Scripts/Board/Day66RouteBuildingPlan.cs`
- `Assets/ProjectEta/Scripts/Board/Day66RouteNodeBuildingDecorator.cs`
- `Assets/ProjectEta/Scripts/Board/Day66RouteNodeBuildingModel.cs`
- `Assets/ProjectEta/Scripts/Board/RouteNodeBuildingStyle.cs`
- `Assets/ProjectEta/Scripts/UI/Day66BoardTransitionFX.cs`
- `Assets/ProjectEta/Scripts/UI/Day66BossHealthPresentationGuard.cs`
- `Assets/ProjectEta/Scripts/UI/Day66CardLiftState.cs`
- `Assets/ProjectEta/Scripts/UI/Day66HandCardMotionController.cs`
- `Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day66TransitionMotion.cs`
- `Assets/ProjectEta/Tests/EditMode/Day66CardLiftStateTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day66PresentationRegressionTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day66RouteBuildingPlanTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day66TransitionMotionTests.cs`
- `Assets/ProjectEta/Tests/EditMode/RouteNodeBuildingStyleTests.cs`
- 각 신규 Unity 파일의 `.meta`
- `Devlogs/Day66/README.md`

### 수정

없음.

### 삭제

없음.

## 이번 일차에서 확인된 잔여 이슈

66일차 종료 전 강제 승리·패배 디버그 버튼 흐름을 추가 확인했다.

현재 강제 승리 버튼은 `BattleController.EndBattle(BattleOutcome.Victory)`를 호출해 정상 Run 승리 흐름에는 진입하지만 실제 Boss HP를 0으로 만들거나 Boss를 사망 상태로 정리하지 않는다.

따라서 강제 승리 직후에는 다음과 같은 상태 불일치가 발생할 수 있다.

- RunFlow는 Map 또는 Reward로 전환
- BattleOutcome은 Victory
- 기존 Boss 데이터는 살아 있음
- BossHealthUI가 기존 Boss를 다시 추적할 가능성 존재

이번 일차에서는 `Day66BossHealthPresentationGuard`로 Battle 외 화면의 보스바를 숨기도록 방어했지만, 디버그 승리 처리 자체를 실제 전투 종료 상태와 일치시키는 작업은 후속 수정 대상으로 남긴다.

또한 RouteMap 건물은 현재 별도 `Day66RouteNodeBuildingDecorator`가 RouteMap 상태를 주기적으로 동기화하는 방식이므로, 추후 안정화 단계에서 실제 RouteMap 노드 생성부와 직접 통합하는 방향을 검토할 수 있다.

## 결과

66일차 작업으로 기존 RouteMap 진행 로직 위에 지도 전용 HUD, 노드 Hover 정보, StageType별 3D 건물, Battle↔Map 전환 연출, 손패 카드 모션이 추가됐다.

기존 RouteMap의 이동 가능 노드·King 이동·Stage 선택 규칙을 유지하면서 플레이어가 현재 위치와 다음 목적지를 더 쉽게 구분할 수 있도록 표현 계층을 확장했다.

동시에 Map 전환 시 BossHealthUI가 남는 상태를 별도 표시 규칙으로 방어하고, 관련 표현 규칙·건물 계획·전환 계산을 EditMode 테스트 대상으로 분리했다.

다만 강제 승리 디버그 버튼이 실제 Boss 사망 상태를 만들지 않는 문제와 전환 연출의 원본 Renderer 복원 타이밍은 후속 안정화 작업에서 다시 점검할 필요가 있다.

## 검증 상태

2026-09-09 기준 GitHub `main` 최신 66일차 커밋을 확인했다.

- SHA: `5646561c3777dcc19e6bd660eadfad03bc8f8077`
- 현재 메시지: `66`
- 부모 커밋: `2aee2fac856e709391af32199b6a36abec8439a0`
- 65일차 대비 커밋 수: 1
- 65일차 대비 변경 파일: 32개
- 변경 형태: 신규 파일 32개, 수정 없음, 삭제 없음
- `Devlogs/Day66/README.md`: 현재 main에는 없음

GitHub 저장소에서 다음 내용을 확인했다.

- RouteMap 전용 HUD와 Hover 정보 패널 추가
- 전체 RouteMap 노드 기반 건물 계획 생성
- StageType별 상세 3D 건물 모델 추가
- 지도 건물 표시 규칙 분리
- Battle 상태에서만 BossHealthUI를 허용하는 표시 규칙 추가
- Battle↔Map 전환 Proxy 연출과 입력 차단 추가
- 손패 카드 Hover·Fusion 선택 상하 모션 추가
- 66일차 관련 EditMode 테스트 파일 5개 추가

이 개발 일지를 같은 66일차 커밋에 포함하기 위해 현재 커밋을 `--amend`하는 방식으로 마무리한다.
