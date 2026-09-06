# 52일차 : Seed 기반 1~10 전체 경로 생성 및 전체 지도 프로토타입

## 개발 목표

51일차까지 구축한 런 세이브와 `MapSeed` 저장 구조를 실제 경로 생성에 연결한다.

기존에는 현재 스테이지를 완료할 때마다 다음 한 층의 후보만 새로 생성했지만, 52일차에서는 런의 `MapSeed`를 기준으로 1~10단계 전체 Route Graph를 한 번에 생성하고 같은 지도 안에서 King이 단계별로 이동하도록 확장한다.

핵심 목표는 다음과 같다.

- 같은 Seed에서 항상 같은 전체 경로 생성
- 1~10단계 전체 노드를 하나의 `RouteMapState`에서 유지
- 일반 단계 2~3개 분기
- 5단계 MidBoss 고정
- 10단계 FinalBoss 고정
- 모든 연결을 King 1칸 이동 규칙으로 제한
- 전체 미래 경로를 10×10 보드에서 미리 표시
- 51일차 저장·복원 구조와 전체 경로 그래프 연결

## StageRouteGenerator 전체 경로 생성

`StageRouteGenerator.CreateFullRoute(int mapSeed)`를 추가했다.

전체 경로 생성은 `MapSeed`를 기반으로 결정론적으로 동작한다.

같은 Seed를 사용하면 다음 정보가 동일하게 생성된다.

- 노드 위치
- 스테이지 종류
- 노드 ID
- 단계별 분기
- 노드 간 연결 관계

플랫폼이나 실행 시점의 `System.Random` 상태에 의존하지 않도록 내부 결정론적 난수 구조를 사용한다.

## 1~10단계 보드 배치

10×10 보드의 Y축을 스테이지 깊이로 사용한다.

- Y 0 : Stage 1
- Y 1 : Stage 2
- Y 2 : Stage 3
- Y 3 : Stage 4
- Y 4 : Stage 5 MidBoss
- Y 5 : Stage 6
- Y 6 : Stage 7
- Y 7 : Stage 8
- Y 8 : Stage 9
- Y 9 : Stage 10 FinalBoss

Stage 1은 고정 시작 Battle 노드이며 이후 전체 경로가 미리 생성된다.

## 일반 단계 분기

기존 Day45 회귀 규칙과의 호환을 유지하기 위해 일반 단계의 분기 수는 다음처럼 구성한다.

- 짝수 깊이 : 3개 분기
- 홀수 깊이 : 2개 분기
- Stage 5 : MidBoss 1개
- Stage 10 : FinalBoss 1개

52일차 전체 경로에서 일반 노드 타입은 다음만 자동 생성 대상으로 사용한다.

- Battle
- Reward
- Shop
- Event

기존 `StageType.Elite`는 타입 자체와 이전 시스템 호환을 위해 유지하지만 52일차 전체 Route Graph 자동 배치에서는 제외한다.

각 일반 깊이에는 최소 하나의 Battle 노드를 포함한다.

## 중앙 경로 기준

전체 경로의 중심 열은 기존 Day45 경로 테스트와의 호환을 위해 보드 중앙 기준 X=4를 사용한다.

3분기에서는 기본적으로 다음 좌표를 사용한다.

- X=3
- X=4
- X=5

2분기에서는 X=4를 항상 포함하고 Seed에 따라 왼쪽 또는 오른쪽 보조 경로를 생성한다.

이를 통해 전체 경로가 Seed에 따라 변화하면서도 기존 지도 진행 좌표 규칙과 호환된다.

## King 1칸 경로 연결

모든 노드 간 연결은 `RouteMapState.IsKingStep()` 규칙을 따른다.

허용되는 이동은 다음과 같다.

- 상
- 좌상
- 우상

현재 지도 진행은 항상 다음 깊이로 이동하므로 결과적으로 Y축으로 1칸 전진하면서 X축은 최대 1칸만 변한다.

각 노드는 최종 보스를 제외하면 최소 하나 이상의 다음 연결을 가진다.

각 다음 단계 노드도 이전 단계에서 적어도 하나의 진입 경로를 가진다.

이를 통해 생성 단계에서 막다른 길이 생기지 않도록 구성했다.

## 보스 단계 고정

Stage 5는 항상 하나의 `MidBoss` 노드로 생성한다.

Stage 10은 항상 하나의 `FinalBoss` 노드로 생성한다.

보스 단계에서는 여러 분기를 만들지 않으며 직전 일반 단계의 경로가 보스 노드로 합쳐진다.

## RouteMapState 전체 그래프 유지

기존 `RouteMapState`는 현재 노드와 바로 다음 후보 중심으로 상태를 교체하는 구조였다.

52일차부터 `_nodes`는 1~10단계 전체 Route Graph를 유지한다.

주요 상태는 다음과 같다.

- `MapSeed`
- `CurrentDepth`
- `CurrentNodeId`
- `SelectedNodeId`
- `KingMapPosition`
- `SelectedPathNodeIds`
- `VisitedNodeIds`
- 전체 `Nodes`

`HasCompleteRoute`를 추가해 1~10단계가 모두 준비됐는지 확인할 수 있다.

## 단계 완료 후 다음 경로 개방

최초 전투 승리 후 전체 Route Graph를 생성한다.

이후 특정 노드를 선택하면 다음과 같이 진행한다.

`Map → 노드 선택 → 해당 Stage 진입 → Stage 완료 → 같은 전체 지도 복귀`

스테이지를 완료해도 전체 그래프를 새로 만들지 않는다.

현재 King 위치와 전체 그래프를 그대로 유지하고 `SelectedNodeId`만 초기화해 현재 노드에 연결된 다음 깊이의 후보를 다시 선택할 수 있게 한다.

## 51일차 저장 구조 연동

51일차의 `RouteMapSaveData`는 이미 다음 정보를 저장한다.

- Map Seed
- 현재 노드
- 선택 노드
- King 좌표
- 선택 경로
- 방문 노드
- 노드 목록
- 연결 관계

52일차에서는 `RouteMapState.Nodes` 자체가 전체 1~10 그래프가 되므로 기존 저장 구조가 전체 런 지도를 그대로 보존하게 된다.

복원 시에도 저장된 전체 노드와 연결 관계를 다시 생성해 동일한 Route Graph와 현재 진행 위치를 유지한다.

## FullRouteMapPreviewController

전체 미래 경로를 보드에서 볼 수 있도록 `FullRouteMapPreviewController`를 추가했다.

기존 `RouteMapBoardController`는 계속 다음 역할을 담당한다.

- 현재 King 노드 표시
- 바로 이동 가능한 다음 노드 표시
- 클릭 입력
- 현재→다음 경로 표시

새 Preview Controller는 다음을 담당한다.

- 미래 단계 노드 표시
- 과거 방문 노드 표시
- 전체 1~10 연결선 표시
- Boss 노드 강조
- 현재 선택 가능 노드와의 시각 중복 방지

미래 노드와 전체 경로선의 Collider는 제거해 기존 지도 클릭 입력을 방해하지 않도록 했다.

## Stage 타입 시각 구분

미래 전체 경로는 StageType에 따라 색상을 구분한다.

- Battle : 청록 계열
- Reward : 녹색 계열
- Shop : 파란색 계열
- Event : 보라색 계열
- MidBoss : 붉은 주황 계열
- FinalBoss : 진한 붉은 계열

방문한 노드는 미래 노드와 다른 톤으로 표시해 이미 지나온 경로를 구분한다.

## Day45 회귀 테스트 수정

52일차 첫 전체 경로 구현 이후 기존 Day45 테스트에서 다음 오류가 발생했다.

`PreparePrototypeAfterBattle_RoundThree_KeepsThreeCandidates`

기존 테스트는 3단계 완료 후 다음 조건을 요구한다.

- King 위치 `(4, 2)` 유지
- Stage 4 후보 정확히 3개

초기 52일차 구현은 전체 경로 중심 X와 일반 분기 수를 Seed 난수로 결정해 Stage 4가 2개가 될 수 있었다.

이를 수정해 다음 회귀 규칙을 유지했다.

- 전체 경로 중심 X=4
- 짝수 깊이 3분기
- 홀수 깊이 2분기

Seed는 여전히 2분기 방향과 StageType 배치 순서에 사용된다.

따라서 기존 Day45 경로 규칙과 52일차 Seed 기반 전체 경로 기능을 함께 유지한다.

## EditMode 테스트

`Day52FullRouteTests`를 추가했다.

주요 테스트 항목은 다음과 같다.

- 같은 Seed에서 같은 전체 그래프 생성
- 다른 Seed에서 경로 프로토타입 변화
- Stage 1~10 깊이 생성
- 일반 깊이 2~3개 분기
- Stage 5 MidBoss 단일 노드
- Stage 10 FinalBoss 단일 노드
- 일반 경로에서 Elite 자동 생성 제외
- 모든 연결이 King 1칸 이동
- 최종 보스 이전 막다른 길 없음
- 모든 노드에 이전 단계 진입 경로 존재
- 최초 승리 후 전체 그래프 생성
- 다음 깊이만 선택 가능
- 스테이지 완료 후 전체 그래프 유지
- 저장·복원 후 전체 그래프와 Seed 유지

기존 `Day45StageFlowTests`의 3단계→4단계 3분기 회귀 조건도 유지하도록 수정했다.

## 주요 파일

### 생성

- `Assets/ProjectEta/Scripts/Board/FullRouteMapPreviewController.cs`
- `Assets/ProjectEta/Tests/EditMode/Day52FullRouteTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Run/StageRouteGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/RouteMapState.cs`

### 삭제

없음.

## 결과

52일차 작업으로 Project η의 10×10 보드가 단순한 다음 스테이지 선택판에서 1~10단계 전체 로그라이트 경로를 표현하는 Route Map으로 확장됐다.

런마다 `MapSeed`를 기반으로 하나의 전체 그래프를 생성하고, 플레이어는 동일한 지도 안에서 King을 한 단계씩 이동하며 Battle·Reward·Shop·Event와 보스 스테이지를 선택한다.

5단계 MidBoss와 10단계 FinalBoss는 강제 경유 지점으로 유지하며, 전체 경로 그래프는 51일차 저장 시스템과 함께 저장·복원된다.

다음 53일차에서는 이 전체 경로 위에서 전투, 카드 보상, 상점, 이벤트, 보스, 메타 보상까지 실제 1~10 런 흐름이 끊김 없이 이어지는지 통합하는 단계로 진행한다.
