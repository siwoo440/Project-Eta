# 48일차 : 메타 토큰·영구 진행·해금 저장 시스템 구축

## 개발 목표

47일차까지 구축한 런 내부 Gold와 별개로, 런이 종료된 뒤에도 계속 유지되는 영구 성장용 `Meta Token` 시스템을 추가한다.

승리와 실패 모두 현재 도달 단계와 보스 처치 여부에 따라 메타 토큰을 지급하고, 지급 결과와 영구 해금 상태를 JSON 파일에 저장해 게임을 다시 실행해도 유지되도록 구성한다.

49~50일차에서 실제 킹 콘텐츠를 연결할 수 있도록 기물·킹·패시브를 공통 영구 해금 구조로 관리한다.

## 주요 개발 내용

### MetaProgressState

런 밖에서 유지되는 영구 진행 상태를 관리하는 `MetaProgressState`를 추가했다.

현재 다음 데이터를 관리한다.

- 보유 Meta Token
- 영구 해금 기물 ID
- 영구 해금 킹 ID
- 영구 해금 패시브 ID

Meta Token 지급과 소비는 `AddTokens()`와 `TrySpendTokens()`를 통해 처리한다.

보유량보다 많은 토큰을 소비할 수 없으며 영구 잔액이 음수가 되지 않도록 구성했다.

### 런 내부 Gold와 영구 Meta Token 분리

47일차의 `RunEconomyState`는 현재 런에서만 사용하는 상점 재화로 유지한다.

48일차의 Meta Token은 별도의 `MetaProgressState`에서 관리하며 런 종료 이후에도 유지한다.

구조는 다음과 같다.

`Run Gold → 현재 런 상점에서만 사용 → 런 종료 시 영구 진행과 분리`

`Meta Token → 런 종료 보상 → 게임 재실행 후에도 유지 → 영구 해금에 사용`

기존 `RunState.MetaCurrency`는 이전 저장 구조와의 호환성을 위해 이번 단계에서는 제거하지 않는다.

### MetaRewardCalculator

런 종료 시 획득할 Meta Token을 계산하는 `MetaRewardCalculator`를 추가했다.

48일차 프로토타입 보상 수치는 다음과 같다.

- 런 종료 기본 참여 보상: 2
- 도달 Stage당: 2
- MidBoss 처치: 10
- FinalBoss 처치: 25
- 전체 런 클리어: 15

예시는 다음과 같다.

- Stage 1 패배: 4 Token
- Stage 3 패배: 8 Token
- Stage 6까지 진행 후 패배: 24 Token
- Stage 10 최종 클리어: 72 Token

밸런싱 수치를 이후 쉽게 변경할 수 있도록 런 종료 처리 코드와 보상 공식을 분리했다.

### 승리·패배 메타 보상 지급

`MetaProgressRunResultController`를 추가해 현재 `RunFlowState`의 종료 상태를 감지한다.

다음 두 상태에서 메타 보상을 지급한다.

- `RunFlowPhase.Completed`
- `RunFlowPhase.Failed`

현재 RunState에 대해 보상은 한 번만 지급하도록 중복 방지 플래그를 사용한다.

새로운 RunState가 연결되면 지급 상태를 다시 초기화해 다음 런의 보상을 받을 수 있도록 한다.

### 보스 처치 보상 판정

현재 10단계 런 구조를 기준으로 다음처럼 처리한다.

- Stage 5를 넘긴 경우 MidBoss 처치로 판정
- 최종 런 클리어 시 FinalBoss 처치로 판정
- 최종 클리어 시 Clear Bonus 추가

실제 보스 처치 기록을 별도 영구 데이터로 저장하는 구조는 이후 런 세이브·통계 확장 시 추가할 수 있도록 현재는 진행 단계 기반으로 단순화했다.

### MetaProgressSaveData

메타 진행 전용 저장 DTO인 `MetaProgressSaveData`를 추가했다.

현재 저장 항목은 다음과 같다.

- 저장 포맷 버전
- Meta Token
- 해금 기물 ID 목록
- 해금 킹 ID 목록
- 해금 패시브 ID 목록

저장 버전은 48일차 기준 `1`로 시작한다.

### MetaProgressSaveService

영구 진행 상태를 JSON 파일로 저장·복원하는 `MetaProgressSaveService`를 추가했다.

저장 위치는 다음 구조를 사용한다.

`Application.persistentDataPath/ProjectEta/meta_progress.json`

저장 과정은 임시 `.tmp` 파일에 먼저 기록한 뒤 실제 저장 파일로 교체하도록 구성했다.

저장 파일이 없으면 새로운 기본 영구 진행 상태를 생성한다.

손상되거나 읽을 수 없는 JSON을 발견하면 게임 진행을 막지 않고 빈 영구 진행 상태로 복구한다.

### MetaProgressService

현재 플레이 세션에서 영구 진행 데이터를 한 번 로드해 공유하는 `MetaProgressService`를 추가했다.

최초 접근 시 디스크에서 `meta_progress.json`을 읽는다.

메타 보상 지급이나 영구 해금 후에는 동일 상태를 즉시 저장한다.

플레이 세션이 재시작되면 캐시를 초기화하고 다음 접근에서 다시 디스크 데이터를 읽도록 구성했다.

### MetaUnlockDefinition

기물·킹·패시브를 동일한 영구 해금 구조로 표현하기 위한 `MetaUnlockDefinition`을 추가했다.

현재 해금 타입은 다음과 같다.

- Piece
- King
- Passive

각 해금 정의는 다음 정보를 가진다.

- UnlockId
- DisplayName
- UnlockType
- Cost

### 프로토타입 영구 해금 목록

49~50일차 실제 콘텐츠 연결을 고려해 다음 영구 해금 ID를 미리 준비했다.

- `piece_unlock_01` : 신규 기물 슬롯
- `passive_unlock_01` : 신규 패시브 슬롯
- `king_attack` : 공격형 킹
- `king_defense` : 방어형 킹
- `king_strategy` : 전략형 킹

현재 비용은 프로토타입 값이며 이후 밸런싱 단계에서 변경한다.

### MetaUnlockService

영구 해금 조건과 토큰 소비를 관리하는 `MetaUnlockService`를 추가했다.

다음 조건을 모두 만족해야 해금할 수 있다.

- 유효한 해금 정의
- 아직 해금되지 않은 항목
- 현재 Meta Token이 비용 이상

해금 성공 시 Meta Token을 차감하고 타입별 영구 해금 ID 집합에 등록한다.

이미 해금된 항목은 다시 비용을 지불할 수 없다.

### MetaProgressUI

런 종료 후 이번 보상과 영구 해금을 확인할 수 있는 개발용 `MetaProgressUI`를 추가했다.

표시 내용은 다음과 같다.

- 런 클리어 / 런 종료
- 도달 Stage
- 이번 런에서 획득한 Meta Token
- 현재 보유 Meta Token
- 영구 해금 목록
- 각 해금 비용
- 해금 완료 상태

영구 해금 버튼을 누르면 토큰 차감과 해금 상태 변경 후 즉시 JSON 저장을 실행한다.

### Battle 씬 자동 생성

48일차 메타 진행 시스템은 기존 Battle 씬을 직접 수정하지 않아도 동작하도록 구성했다.

Battle 씬 로드 후 `MetaProgressRunResultController`가 자동으로 생성된다.

런 종료를 감지하면 필요한 `MetaProgressUI`도 런타임에 자동으로 생성한다.

별도의 Scene 오브젝트 배치나 Inspector 연결이 필요하지 않다.

### EditMode 테스트

`Day48MetaProgressTests`를 추가해 다음 규칙을 확인하도록 구성했다.

- Meta Token 지급과 소비
- 보유량 초과 소비 차단
- Stage 3 패배 보상
- MidBoss 이후 진행 보상
- Stage 10 전체 클리어 보상
- 정상 영구 해금
- 중복 해금 차단
- 토큰 부족 해금 차단
- JSON 직렬화·역직렬화
- Meta Token 복원
- Piece·King·Passive 해금 상태 복원

## 기존 테스트 경고 확인

EditMode 테스트 실행 중 다음 경고가 나타날 수 있다.

`SpawnTestEnemy: (4, 8)에 배치할 수 없습니다(범위 밖 또는 이미 점유됨).`

이 경고는 `AttackExecutionTests.SpawnTestEnemy_Fails_WhenTileAlreadyOccupied()` 테스트가 이미 점유된 `(4, 8)` 칸에 다시 적 배치를 시도해 소환이 정상적으로 거부되는지 확인하면서 의도적으로 발생한다.

테스트는 반환값이 `null`이고 기존 점유 기물이 유지되는지를 검증하므로 현재 기능 오류가 아니라 실패 경로를 확인하는 정상적인 개발용 경고다.

## 주요 파일

- `Assets/ProjectEta/Scripts/Meta/MetaProgressState.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressSaveData.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressSaveService.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressService.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaRewardCalculator.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaUnlockDefinition.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaUnlockService.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressRunResultController.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs`
- `Assets/ProjectEta/Tests/EditMode/Day48MetaProgressTests.cs`

## 결과

런 내부에서 사용하는 Gold와 게임 전체에 유지되는 Meta Token을 분리했다.

런이 실패하거나 최종 승리하면 현재 진행도에 따라 Meta Token을 지급하고 결과를 `meta_progress.json`에 저장한다.

게임을 다시 실행해도 Meta Token과 기물·킹·패시브 해금 상태가 복원되는 영구 성장 기반이 구축됐다.

현재는 프로토타입 해금 ID와 비용을 사용하며 49~50일차에서 공격형·방어형·전략형 킹의 실제 능력과 선택 구조를 이 영구 해금 시스템에 연결한다.
