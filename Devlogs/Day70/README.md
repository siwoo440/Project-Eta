# 70일차 : 카드 보상 품질 확장·5페이즈 RouteMap 진행 및 최종 보스 전환 안정화

## 개발 목표

70일차에서는 기존 카드 보상 시스템을 단순 무작위 3택에서 Stage 진행도와 보상 발생 위치를 반영하는 품질 기반 보상으로 확장하고, 하나의 10단계 RouteMap을 여러 페이즈로 이어가는 장기 런 진행 구조를 추가했다.

이번 일차의 중심은 다음과 같다.

- 카드 보상 품질 단계 추가
- Stage 진행도에 따른 1~3성 카드 등장 가중치 조정
- 일반 전투 보상과 Reward 노드 보상의 품질 차등
- 기존 카드 중복·해금·획득 제한 규칙과 품질 가중 선택 통합
- 5개 페이즈 RouteMap 생성
- Phase 1~4 마지막 노드를 MidBoss로 구성
- Phase 5 마지막 노드를 FinalBoss로 유지
- 페이즈 종료 시 다음 페이즈 시작점으로 직접 전환
- FinalBoss와 MidBoss 종료 조건 분리
- 페이즈 정보가 저장·표시 흐름에 유지되도록 보강
- 카드 보상 품질 및 5페이즈 진행 EditMode 테스트 추가

## 카드 보상 품질 단계 추가

기존 카드 보상은 획득 가능한 카드 목록을 만든 뒤 Seed 기반 셔플로 후보를 선택하는 구조였다.

70일차에서는 기존 생성 방식을 유지하면서 별도의 `CardRewardProfile`을 전달할 수 있는 가중 선택 경로를 추가했다.

보상 품질은 다음 세 단계로 구분한다.

- `Basic`
- `Improved`
- `Advanced`

각 프로필은 다음 정보를 가진다.

- 보상 발생 경로
- 현재 Stage
- 1성 카드 가중치
- 2성 카드 가중치
- 3성 카드 가중치

4성·5성 기물은 일반 카드 Reward의 품질 가중치에서 0으로 유지해 기존 고등급 획득 제한 규칙을 침범하지 않도록 했다.

## Stage 진행도 기반 보상 가중치

`CardRewardQualityRules`에서 현재 Stage와 보상 발생 위치를 기준으로 프로필을 생성한다.

### Stage 1~3

일반 전투 보상:

- 1성: 80
- 2성: 18
- 3성: 2

Reward 노드:

- 1성: 65
- 2성: 30
- 3성: 5

초반에는 1성 중심의 보상을 유지하되 Reward 노드에서 2~3성 확률을 조금 더 높였다.

### Stage 4~6

일반 전투 보상:

- 1성: 50
- 2성: 42
- 3성: 8

Reward 노드:

- 1성: 35
- 2성: 50
- 3성: 15

중반부터 2성 비중을 크게 올리고 Reward 노드는 일반 전투보다 3성 비중을 더 높였다.

### Stage 7 이상

일반 전투 보상:

- 1성: 25
- 2성: 55
- 3성: 20

Reward 노드:

- 1성: 15
- 2성: 50
- 3성: 35

후반으로 갈수록 1성 비중을 줄이고 2~3성 카드가 더 자주 등장하도록 구성했다.

## 카드 후보 가중 선택

`CardRewardGenerator`에 품질 프로필을 받는 생성 경로를 추가했다.

기존과 동일하게 먼저 다음 규칙을 통과한 카드만 후보에 포함한다.

- 현재 런에서 해금된 기물
- `CardRewardRules`상 획득 가능한 기물
- 동일 `PieceId` 중복 제외

그 뒤 각 카드의 `PieceGrade`에 맞는 가중치를 합산하고 Seed 기반 난수로 후보를 하나씩 선택한다.

선택된 카드는 즉시 남은 후보에서 제거해 같은 카드가 한 번의 3택 안에서 중복 등장하지 않도록 유지했다.

품질 프로필이 전달되지 않은 기존 호출은 이전 셔플 방식을 그대로 사용해 기존 코드와의 호환성을 유지한다.

## 보상 UI 품질 표시 연동

전투 승리 보상과 Reward 노드 진입 시 현재 Stage와 보상 경로를 사용해 `CardRewardProfile`을 계산하도록 연결했다.

따라서 보상 후보 생성과 화면 표시가 같은 품질 정보를 사용한다.

보상 품질은 다음 표시명을 사용할 수 있도록 구성했다.

- 기본 보상
- 향상 보상
- 고급 보상

## 5페이즈 RouteMap 구조 추가

기존 한 번의 1~10 Stage 경로를 장기 런으로 확장하기 위해 `RunPhaseRouteGenerator`와 `RunPhaseProgressService`를 추가했다.

전체 페이즈 수는 5개다.

각 페이즈는 기존 `StageRouteGenerator`의 1~10 Stage 생성 규칙을 재사용하되, 페이즈별 Seed를 분리해 동일 런에서도 서로 다른 경로를 생성할 수 있도록 했다.

Phase 2~5의 노드는 ID 앞에 페이즈 정보를 포함한다.

예시:

- `phase_2_...`
- `phase_3_...`
- `phase_4_...`
- `phase_5_...`

Phase 1은 기존 저장 데이터와의 호환성을 위해 기존 노드 ID 구조를 유지한다.

## 페이즈별 마지막 보스 규칙

각 페이즈의 Stage 10 역할을 다음과 같이 구분했다.

- Phase 1 Stage 10: MidBoss
- Phase 2 Stage 10: MidBoss
- Phase 3 Stage 10: MidBoss
- Phase 4 Stage 10: MidBoss
- Phase 5 Stage 10: FinalBoss

기존 단일 RouteMap에서 Stage 10이 FinalBoss였던 경로는 첫 전투 완료 후 Phase 1 규칙에 맞게 MidBoss 경로로 정규화한다.

이를 통해 첫 페이즈부터 최종 보스가 등장하지 않고, 최종 페이즈에서만 FinalBoss가 유지된다.

## 페이즈 종료 흐름 정리

`RunStageFlowService`가 전투 결과를 기록한 뒤 현재 Stage 10이 페이즈 종료 지점인지 판단하도록 정리했다.

Phase 1~4 MidBoss 승리 시:

1. 현재 전투를 승리 완료 상태로 기록
2. 다음 페이즈 전체 Route 생성
3. King을 새 페이즈 Stage 1 시작점으로 이동
4. 현재 Stage를 1로 복구
5. 선택 노드를 해제
6. `RunFlowPhase.Map`으로 전환

이 과정에서는 중간에 `RunFlowPhase.Completed`를 만들지 않는다.

따라서 페이즈 사이의 전환을 런 종료로 잘못 처리하지 않고 하나의 런 안에서 이어서 진행할 수 있다.

## FinalBoss 종료 조건 안정화

5페이즈 전환 도입 과정에서 기존 `Day53RogueliteLoopTests.CompleteBattleVictory_FinalBossEndsRun`과 충돌하는 케이스를 확인했다.

원인은 Stage 10이라는 숫자만으로 다음 페이즈 전환을 시도하면, 기존 FinalBoss 직접 진행 테스트도 Phase 1 종료로 오인할 수 있다는 점이었다.

`RunPhaseProgressService.TryAdvanceToNextPhase()`에 현재 RouteMap 노드의 StageType 검증을 추가했다.

다음 페이즈로 이동할 수 있는 조건은 현재 마지막 노드가 실제 `MidBoss`인 경우로 제한했다.

따라서 다음 흐름이 분리된다.

- Phase 1~4의 MidBoss 승리 → 다음 페이즈 Map
- Phase 5의 FinalBoss 승리 → Completed
- RouteMap 노드가 없는 기존 FinalBoss 직접 진행 → Completed
- FinalBoss 또는 기타 StageType → 추가 페이즈 전환 차단

`RunStageFlowService`는 다음 페이즈 전환이 성립하지 않는 Stage 10 승리에서 `CompleteRun()`을 호출하므로 기존 FinalBoss 종료 규칙도 유지된다.

## 저장 흐름의 페이즈 정보 보강

`RunPersistenceController`에서 현재 RouteMap 페이즈를 안전 지점 식별 정보에 포함하도록 보강했다.

저장 로그와 Toast에서도 현재 페이즈를 함께 표시할 수 있도록 구성했다.

페이즈 전환 직후 Stage 번호가 다시 1이 되더라도 서로 다른 페이즈의 저장 지점을 구분할 수 있다.

## RouteMap 표시 보강

RouteMap UI에서도 현재 페이즈 정보를 읽어 장기 런 진행 상태를 표현할 수 있도록 관련 표시 코드를 보강했다.

기존 Stage 깊이와 별도로 현재 페이즈를 확인할 수 있어 1~10 Stage가 반복되는 구조에서도 플레이어가 전체 진행 위치를 구분할 수 있다.

## 테스트 추가

### Day70 카드 보상 품질 테스트

`Day70CardRewardQualityTests`에서 다음 규칙을 검증한다.

- 초반 전투 보상은 1성 가중치가 가장 높음
- 후반 전투 보상은 초반보다 3성 가중치가 높음
- Reward 노드는 동일 Stage 일반 전투보다 3성 가중치가 높음
- 일반 Reward에서 4성·5성 가중치는 0
- 잘못된 Stage 값은 Stage 1로 보정

### 5페이즈 RouteMap 테스트

`Day71FivePhaseRouteTests`에서 다음 규칙을 검증한다.

- Phase 2 이상 노드 ID에 페이즈 정보 포함
- Phase 1~4 Stage 10은 MidBoss
- Phase 5 Stage 10은 FinalBoss
- 첫 전투 완료 후 Phase 1 경로 MidBoss 정규화
- Phase 1 Stage 10 승리 후 Phase 2 시작점 직접 전환
- Phase 5 Stage 10 승리 후 최종 Completed 유지
- 비전투형 Stage 10 예외 경로의 다음 페이즈 전환
- 구버전 노드 ID의 Phase 1 호환

기존 Day53 로그라이트 루프 테스트의 FinalBoss 직접 종료 규칙과 새 5페이즈 전환 조건이 충돌하지 않도록 MidBoss StageType 검증도 추가했다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Run/CardRewardQuality.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardQualityRules.cs`
- `Assets/ProjectEta/Scripts/Run/RunPhaseProgressService.cs`
- `Assets/ProjectEta/Scripts/Run/RunPhaseRouteGenerator.cs`
- `Assets/ProjectEta/Tests/EditMode/Day70CardRewardQualityTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day71FivePhaseRouteTests.cs`
- 신규 파일의 `.meta`
- `Devlogs/Day70/README.md`

### 수정

- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardController.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/RunPersistenceController.cs`
- `Assets/ProjectEta/Scripts/Run/RunStageFlowService.cs`
- `Assets/ProjectEta/Scripts/UI/CardRewardUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs`

### 삭제

- 최종 커밋 기준 영구 삭제 파일 없음

## 결과

70일차에서는 카드 보상이 런 진행도와 보상 위치에 따라 품질이 달라지는 구조로 확장됐다.

초반에는 1성 중심, 후반에는 2~3성 중심으로 자연스럽게 보상 풀이 변화하고 Reward 노드는 일반 전투보다 높은 품질을 제공한다.

동시에 RouteMap을 5개 페이즈로 연결해 각 페이즈의 10번째 Stage를 중간 보스 또는 최종 보스로 구분할 수 있게 됐다.

Phase 1~4 종료는 런을 완료시키지 않고 다음 페이즈 Stage 1로 직접 이어지며, Phase 5 FinalBoss에서만 최종 `Completed` 상태가 유지된다.

기존 FinalBoss 직접 종료 테스트와 새 페이즈 전환 조건이 충돌하지 않도록 Stage 10 숫자만이 아니라 실제 `StageType.MidBoss` 여부를 확인하는 조건도 추가했다.

## 검증 상태

GitHub `main` 최신 커밋은 `a6393d6ae61db7e25e954c725c3d7c90dc5f101e`이며 커밋 메시지는 `70`이다.

이전 69일차 커밋 `a33df8d82a1a638fc84813da7e2a4c6e8cf52524`와 비교하면 현재 작업은 1개 커밋으로 구성되어 있고, 총 19개 경로가 변경됐다.

최신 `RunPhaseProgressService`에는 현재 RouteMap 노드가 실제 `MidBoss`일 때만 다음 페이즈로 이동하는 조건이 포함되어 있다.

최신 `RunStageFlowService`는 Stage 10에서 페이즈 전환이 성립하면 다음 Map으로 이동하고, 성립하지 않으면 `CompleteRun()`을 호출한다.

따라서 소스 흐름 기준으로 기존 Day53 FinalBoss 종료 규칙과 새 Day71 5페이즈 진행 규칙 사이에서 확인됐던 직접적인 충돌은 해소된 상태다.

GitHub에 등록된 Commit Status는 현재 없으므로 GitHub CI 기준 컴파일·테스트 결과는 확인할 수 없다.

따라서 현재 검증은 최신 커밋의 소스 구조와 테스트 기대값 비교 기준이며, Unity Editor에서 전체 컴파일과 EditMode Test Runner 최종 실행 결과는 별도로 확인해야 한다.
