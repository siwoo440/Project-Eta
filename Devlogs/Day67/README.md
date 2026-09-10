# 67일차 : 전투 결과·알림 UI 통합 및 RouteMap 노드 표시 구조 안정화

## 개발 목표

67일차에서는 전투 시작부터 런 종료까지 필요한 상태 알림과 결과 UI를 통합하고, 66일차 RouteMap 표현 계층에서 확인된 표시 충돌과 상태 소유 문제를 정리했다.

이번 일차의 중심은 다음과 같다.

- Battle Start·Turn·Passive·Boss Phase·Victory/Defeat 알림 통합
- Run Completed/Failed 및 Meta Token 결과 UI 정식화
- 강제 Victory/Defeat와 실제 전투 종료 상태 일치
- BattleOutcome 개발 버튼 중복 구조 정리
- BossHealthUI 표시 책임을 실제 UI 소유 객체로 통합
- RouteMap 노드 건물 Decorator·Guard 의존 제거
- RouteMap 노드 생성부에서 시각 모델 직접 생성
- 높은 건물형 노드를 낮은 바닥형 발판 모델로 재구성
- 노드 표시 위치를 1칸 중심에서 2x2 중앙 기준으로 조정
- 기존 EditMode 회귀 테스트와 새 구조의 호환성 정리

67일차에서는 새로운 보정용 Guard를 계속 추가하는 방식보다 전투 종료, Boss UI, RouteMap 표시의 책임을 실제 소유 시스템에 직접 배치하는 방향으로 구조를 정리했다.

## 전투 상태 알림 통합

전투 진행 상태를 공통 Queue 기반 알림으로 표시하도록 구성했다.

주요 알림 대상:

- `BATTLE START`
- `DEPLOYMENT`
- `PLAYER TURN`
- `ENEMY TURN`
- `PASSIVE`
- `WARNING / BOSS PHASE II`
- `VICTORY`
- `DEFEAT`
- `RUN COMPLETED`
- `RUN FAILED`

`Day67AnnouncementQueue`는 알림을 순차 처리하고 바로 이어지는 중복 알림을 차단한다.

`Day67BattleAnnouncementCoordinator`는 `TurnManager`, `RunState`, `KingRunState`, Boss Phase 표시 상태를 읽어 실제 전투 진행과 알림을 연결한다.

## King Passive 알림

King 전투 상태 변화도 공통 알림 흐름에 포함했다.

현재 감지 대상:

- 공격형 King 격노 증가
- 방어형 King 방벽 활성
- 전략형 King 전술적 준비 활성

기존 King 시스템의 상태를 다시 계산하지 않고 `KingRunStateService`가 제공하는 런타임 상태 변화를 표시 계층에서 감지하도록 구성했다.

## Boss Phase II 경고

기존 Boss Phase 시스템을 재작성하지 않고 `BossPhaseStatusUI`가 Phase II 상태를 표시하는 시점을 감지해 경고 알림을 출력한다.

Boss Phase II 알림은 전투당 한 번만 표시하도록 구성했다.

## Run Result·Meta Token UI

기존 `MetaProgressRunResultController`의 보상 계산과 지급 로직은 유지했다.

67일차에서는 `MetaProgressUI`를 런 결과 화면 형태로 정리해 다음 정보를 명확히 표시하도록 변경했다.

- `RUN COMPLETED` / `RUN FAILED`
- 도달 Stage
- 획득 Meta Token
- 누적 Meta Token
- 영구 성장 화면 진입
- 결과 화면 닫기

Meta Token 지급 공식이나 중복 지급 방지 로직은 새로 만들지 않고 기존 Meta Progress 시스템을 그대로 사용한다.

## BattleOutcome 개발 버튼 구조 정리

기존에는 개발용 승리·패배 UI가 두 계통으로 존재했다.

정리 후에는 `DebugBattleResultButtons`를 단일 개발용 결과 버튼으로 사용한다.

삭제한 `BattleOutcomeDebugUI`를 참조하던 다음 영역도 현재 구조에 맞춰 수정했다.

- `SceneRuntimeBootstrap`
- `BattleHUD`
- `Day61KingCombatHudTests`

개발 버튼의 Victory/Defeat는 모두 `BattleController.EndBattle()`을 통해 공통 전투 종료 흐름으로 들어간다.

## BattleController 전투 종료 책임 통합

강제 Victory에서 적 HP만 0으로 만들던 별도 정규화 방식은 제거하고, `BattleController`가 결과 상태 정리를 직접 소유하도록 변경했다.

Victory 종료 시:

- 남아 있는 적 PieceRuntimeState 수집
- 1x1·2x2 적 보드 점유 해제
- 죽은 아군 카드 Owned Pool 복귀
- TurnManager BattleEnded 처리
- RunStageFlowService 결과 전환

Defeat 종료 시:

- King HP를 0으로 정규화
- 공통 BattleEnded·Run 실패 흐름 실행

이를 통해 개발 버튼과 실제 전투 종료의 런 상태 차이를 줄였다.

## BossHealthUI 표시 구조 정리

Map 전환 뒤 이전 Boss HP UI가 남는 문제를 외부 Guard가 반복해서 숨기는 방식으로 처리하지 않도록 변경했다.

`BossHealthUI` 자체가 다음 조건을 만족할 때만 표시되도록 정리했다.

- `BoardMode.Battle`
- `RunFlowPhase.Battle`

Map, Reward, Shop, Event, Completed, Failed 흐름에서는 BossHealthUI가 표시되지 않는다.

## RouteMap 표시 책임 통합

66일차의 `Day66RouteNodeBuildingDecorator`처럼 별도 시스템이 RouteMap 노드를 다시 탐색해 시각 모델을 붙이는 구조를 제거했다.

현재는 다음 생성 주체가 노드와 함께 시각 모델을 직접 만든다.

- `RouteMapBoardController`
- `FullRouteMapPreviewController`

이를 통해 노드 원판은 생성됐지만 건물 모델이 누락되는 생성 순서 문제를 줄이고, Map 화면 수명과 노드 시각 수명을 동일하게 맞췄다.

## RouteMap 노드 바닥형 발판 재구성

초기 StageType별 높은 건물형 모델은 10x10 보드에서 노드 간격이 좁고 서로 붙어 보이는 문제가 있었다.

따라서 `Day66RouteNodeBuildingModel`을 높은 건물 대신 낮은 바닥형 발판 모델로 재구성했다.

공통 발판은 다음 계층으로 구성된다.

- 외곽 원형 Base Plate
- 내부 Plate
- 중앙 Plate
- StageType별 바닥 문양

StageType은 높이보다 바닥 색상과 문양으로 구분한다.

주요 문양:

- Battle: 교차 전투선
- Elite: 다이아 문양
- Reward: 보물·보석 문양
- Shop: 차양·코인 문양
- Event: 마법진·룬 문양
- Mid Boss: 보스 문장·뿔 문양
- Final Boss: 왕관 문양

기존 Hover·선택 색상 신호는 발판 파트에도 계속 반영된다.

## RouteMap 2x2 중앙 배치

기존 RouteMap 노드는 보드 한 칸의 중심을 그대로 사용했다.

67일차 후반 수정에서는 노드가 한 칸에 하나씩 빽빽하게 붙어 보이는 문제를 완화하기 위해 시각 위치 계산을 `GetNodeLocalPosition()`으로 통일했다.

노드 표시 기준:

- 기본 셀 중심 좌표 계산
- 인접 2x2 영역의 중앙 방향으로 0.5 TileSize 오프셋
- 보드 가장자리에서는 바깥으로 넘어가지 않도록 안쪽 방향으로 보정

같은 위치 계산을 다음 요소에 적용했다.

- 현재 노드
- 선택 가능 노드
- 미래·방문 노드
- RouteMap 연결선
- Map King
- Map King 이동 목표

따라서 노드, 경로선, King 이동이 서로 다른 좌표 기준을 사용하지 않도록 맞췄다.

## EditMode 테스트 호환 정리

구조 변경 과정에서 과거 테스트가 삭제된 타입이나 공개 API를 계속 기대하는 문제가 확인됐다.

주요 정리 내용:

- `BattleOutcomeDebugUI` 기반 Day61 회귀 테스트를 `DebugBattleResultButtons` 기준으로 변경
- `Day66MapPresentationRules` 호환 API 유지
- `Day66RouteNodeBuildingModel.VisualRoot` 공개 조회 지원
- `Day66RouteNodeBuildingModel.PartCount` 공개 조회 지원
- 바닥형 Shop 노드가 복수 파트를 즉시 생성하는 기존 테스트 조건 유지

또한 EditMode에서 Primitive Collider를 정리할 때 `Destroy()`를 직접 호출하면 Unity가 오류 로그를 발생시키므로 실행 환경에 따라 제거 방식을 분리했다.

- Play Mode: `Object.Destroy`
- EditMode: `Object.DestroyImmediate`

동일 정리 함수를 VisualRoot와 런타임 Material 제거에도 사용하도록 맞췄다.

## 구조 정리 결과

67일차에서 제거하거나 역할을 축소한 핵심 보정 계층은 다음과 같다.

- `BattleOutcomeDebugUI`
- `Day67BoardModeVisualGuard`
- `Day67BattleResultStateNormalizer`
- `Day66BossHealthPresentationGuard`
- `Day66RouteNodeBuildingDecorator`

기능을 삭제하는 것이 목적이 아니라 해당 기능을 실제 소유 객체에 다시 배치하는 것이 목적이다.

현재 책임 구조는 다음과 같다.

`BattleController → 전투 종료 상태 정리`

`BossHealthUI → Boss HP 표시 조건`

`RouteMapBoardController → 현재·선택 노드 생성 및 표시`

`FullRouteMapPreviewController → 미래·방문 노드 표시`

`Day66RouteNodeBuildingModel → StageType별 바닥형 노드 모델`

`Day66BoardTransitionFX → 화면 전환 연출`

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncement.cs`
- `Assets/ProjectEta/Scripts/UI/Day67AnnouncementQueue.cs`
- `Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncementUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncementCoordinator.cs`
- `Assets/ProjectEta/Tests/EditMode/BossHealthPresentationTests.cs`
- 신규 Unity 파일의 `.meta`
- `Devlogs/Day67/README.md`

### 수정

- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`
- `Assets/ProjectEta/Scripts/Boss/BossHealthUI.cs`
- `Assets/ProjectEta/Scripts/Board/RouteMapBoardController.cs`
- `Assets/ProjectEta/Scripts/Board/FullRouteMapPreviewController.cs`
- `Assets/ProjectEta/Scripts/Board/Day66RouteNodeBuildingModel.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`
- `Assets/ProjectEta/Scripts/UI/BattleHUD.cs`
- `Assets/ProjectEta/Tests/EditMode/Day61KingCombatHudTests.cs`
- 관련 66·67일차 EditMode 회귀 테스트

### 삭제

- `Assets/ProjectEta/Scripts/UI/BattleOutcomeDebugUI.cs`
- `Assets/ProjectEta/Scripts/UI/BattleOutcomeDebugUI.cs.meta`
- `Assets/ProjectEta/Scripts/UI/Day66BossHealthPresentationGuard.cs`
- `Assets/ProjectEta/Scripts/UI/Day66BossHealthPresentationGuard.cs.meta`
- `Assets/ProjectEta/Scripts/UI/Day67BoardModeVisualGuard.cs`
- `Assets/ProjectEta/Scripts/UI/Day67BoardModeVisualGuard.cs.meta`
- `Assets/ProjectEta/Scripts/Board/Day66RouteNodeBuildingDecorator.cs`
- `Assets/ProjectEta/Scripts/Board/Day66RouteNodeBuildingDecorator.cs.meta`
- `Assets/ProjectEta/Scripts/Battle/Day67BattleResultStateNormalizer.cs`
- `Assets/ProjectEta/Scripts/Battle/Day67BattleResultStateNormalizer.cs.meta`

## 결과

67일차 작업으로 전투 시작부터 턴 진행, King Passive, Boss Phase, 전투 결과, 런 종료까지 이어지는 주요 상태 피드백을 하나의 알림 흐름으로 통합했다.

동시에 66일차 RouteMap 표현 과정에서 누적된 Guard·Decorator·중복 개발 UI 구조를 정리하고, 전투 종료·Boss HP·RouteMap 노드 표시 책임을 실제 소유 시스템으로 이동했다.

RouteMap 노드는 높은 건물 모델에서 낮은 바닥형 발판과 StageType 문양 중심으로 변경했으며, 표시 좌표도 2x2 중앙 기준으로 조정해 노드가 서로 붙어 보이는 문제를 완화했다.

## 검증 상태

2026-09-10 기준 GitHub `main` 최신 커밋을 확인했다.

- SHA: `5dd27c6a4d8d6e6251955537f19a7728c5c80589`
- 커밋 메시지: `67`
- `Devlogs/Day67/README.md`는 아직 원격 저장소에 없음

최신 원격 커밋의 `Day66RouteNodeBuildingModel`에는 EditMode에서도 `Destroy()`를 호출하는 코드가 남아 있다.

최종 로컬 수정에서는 Play Mode와 EditMode의 제거 API를 분리했지만, 이 개발 일지를 커밋하기 전 Unity EditMode Test Runner를 다시 실행해 최종 결과를 확인해야 한다.

따라서 이번 amend 커밋에는 마지막 `Day66RouteNodeBuildingModel` EditMode 정리 수정과 이 `README.md`를 함께 포함하는 것을 기준으로 한다.
