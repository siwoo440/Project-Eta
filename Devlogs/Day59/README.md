# 59일차 : 영구 해금 런 콘텐츠 적용 및 Run Snapshot 구조 구현

## 개발 목표

58일차에서 구매·저장할 수 있게 만든 영구 해금 상태를 실제 런의 콘텐츠 가용성 규칙에 연결한다.

59일차의 핵심 목표는 다음과 같다.

- 새 런 시작 시 영구 해금 상태를 Run 단위 Snapshot으로 고정
- 진행 중인 런에 이후 Meta Progress 변경이 소급 적용되지 않도록 분리
- 이어하기에서도 동일한 Run Snapshot 유지
- 기물의 영구 해금 요구 ID 지원
- Reward / Shop / Event 카드 후보에서 잠긴 기물 제외
- 기존 King 영구 해금 규칙을 Run Snapshot 기준으로 정합화
- 향후 Passive 콘텐츠 해금 적용을 위한 공통 가용성 기반 마련
- 아직 실제 콘텐츠가 정해지지 않은 Placeholder 해금은 임의 매핑하지 않음

## RunContentUnlockSnapshot

`RunContentUnlockSnapshot`을 추가해 런 시작 시점의 영구 해금 상태를 고정한다.

Snapshot은 다음 세 종류의 영구 해금 목록을 가진다.

- 기물 해금 ID
- King 해금 ID
- Passive 해금 ID

새 런에서 Snapshot을 생성한 뒤에는 Meta Progress가 변경되어도 기존 Snapshot은 바뀌지 않는다.

따라서 영구 해금은 현재 진행 중인 런이 아니라 다음 새 런부터 적용되는 구조가 된다.

## Snapshot 저장 데이터

`RunContentUnlockSnapshotSaveData`를 추가했다.

저장 데이터에는 다음 값이 포함된다.

- RunId
- unlockedPieceIds
- unlockedKingIds
- unlockedPassiveIds

Snapshot은 저장 DTO로 변환하거나 다시 복원할 수 있다.

이를 통해 메모리 상태뿐 아니라 이어하기에서도 런 시작 당시의 해금 상태를 유지할 수 있다.

## RunContentUnlockSnapshotService

`RunContentUnlockSnapshotService`를 추가했다.

현재 런의 `RunId`를 기준으로 Snapshot을 관리한다.

동작 순서는 다음과 같다.

1. 현재 활성 BattleController에서 RunState 확인
2. 같은 RunId의 메모리 Snapshot이 있으면 재사용
3. 저장된 Snapshot이 있고 RunId가 같으면 복원
4. 기존 Snapshot이 없으면 현재 Meta Progress에서 새 Snapshot 생성
5. 새 Snapshot을 별도 파일로 저장

Snapshot 저장 파일은 다음 경로를 사용한다.

`Application.persistentDataPath/run_content_unlock_snapshot.json`

다른 RunId의 Snapshot은 현재 런에 재사용하지 않는다.

## MetaContentAvailabilityService

기물·King·Passive의 영구 해금 판정을 한 곳에서 처리하는 `MetaContentAvailabilityService`를 추가했다.

현재 제공하는 주요 판정은 다음과 같다.

- `IsRequiredUnlockAvailable`
- `IsPieceAvailable`
- `IsKingAvailable`
- `IsPassiveAvailable`

필요한 영구 해금 ID가 비어 있는 콘텐츠는 기본 콘텐츠로 간주해 항상 사용 가능하다.

영구 해금 ID가 지정된 콘텐츠는 현재 Run Snapshot에 해당 ID가 존재해야 사용할 수 있다.

## PieceDefinition 영구 해금 필드

`PieceDefinition`에 다음 필드를 추가했다.

`RequiredMetaUnlockId`

값이 비어 있으면 기존과 같은 기본 기물이다.

값이 지정되면 해당 Meta Unlock이 현재 Run Snapshot에 포함되어 있을 때만 획득 후보에 들어갈 수 있다.

기존 PieceDefinition 에셋은 새 필드가 기본적으로 비어 있기 때문에 기존 콘텐츠 가용성은 유지된다.

## 카드 Reward 적용

`CardRewardGenerator`가 현재 활성 런의 `RunContentUnlockSnapshot`을 조회하도록 변경했다.

카드 후보를 만들 때 기존 `CardRewardRules.CanOffer()` 검사 전에 `MetaContentAvailabilityService.IsPieceAvailable()`을 적용한다.

따라서 영구 해금 조건을 만족하지 않은 기물은 후보 생성 단계에서 제외된다.

기존 Reward, Shop, Event 카드 획득 흐름이 `CardRewardGenerator`를 재사용하고 있으므로 공통 후보 생성 규칙에서 영구 해금 필터를 적용할 수 있게 됐다.

## King 영구 해금 정합화

기존 `KingUnlockRules`는 Meta Progress를 직접 확인해 특수 King의 선택 가능 여부를 판정했다.

59일차에서는 기존 공개 진입점을 유지하면서 현재 Run Snapshot을 조회하도록 변경했다.

또한 `RunContentUnlockSnapshot`을 직접 받는 `CanSelect` 오버로드를 추가했다.

현재 규칙은 다음과 같다.

- Default King: 항상 선택 가능
- Attack King: `king_attack` 해금 필요
- Defense King: `king_defense` 해금 필요
- Strategy King: `king_strategy` 해금 필요

특수 King의 해금 상태 역시 런 시작 시점 Snapshot을 기준으로 고정된다.

## Passive 확장 기반

`MetaContentAvailabilityService.IsPassiveAvailable()`을 추가했다.

현재 59일차에는 실제 Passive 콘텐츠 Pool이 아직 연결되어 있지 않기 때문에 구체적인 Passive를 새로 만들거나 임의 해금 ID에 연결하지 않는다.

향후 실제 Passive 콘텐츠가 추가되면 같은 Run Snapshot 규칙으로 가용성을 판정할 수 있다.

## Placeholder 해금 처리

58일차 영구 성장 카탈로그에는 실제 콘텐츠가 아직 확정되지 않은 다음 Placeholder 해금이 존재한다.

- `piece_unlock_01`
- `passive_unlock_01`

59일차에서는 이 ID를 특정 PieceId나 PassiveId에 임의로 연결하지 않았다.

실제 해금 대상 콘텐츠가 확정되면 PieceDefinition 또는 향후 Passive 정의에서 필요한 Meta Unlock ID를 지정하는 방식으로 연결한다.

## Run Save 범위

59일차에서는 기존 `RunSaveData` 포맷을 변경하지 않았다.

영구 해금 Snapshot은 별도 `run_content_unlock_snapshot.json` 파일로 관리한다.

기존 런 저장 구조와 버전을 직접 변경하지 않으면서 RunId 기준으로 동일한 Snapshot을 복원하는 방식이다.

## 테스트 소스

`Day59MetaContentAvailabilityTests`를 추가했다.

현재 포함한 검증 항목은 다음과 같다.

- Piece / King / Passive 해금 Snapshot 생성 및 저장 DTO 복원
- 영구 해금 ID가 없는 기본 콘텐츠 상시 사용
- Snapshot 생성 이후 Meta Progress 변경의 현재 런 소급 적용 차단
- PieceDefinition의 RequiredMetaUnlockId 기반 가용성 판정
- CardRewardGenerator의 Snapshot 및 MetaContentAvailabilityService 연결
- KingUnlockRules의 Run Snapshot 고정 동작

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Meta/MetaContentAvailabilityService.cs`
- `Assets/ProjectEta/Scripts/Run/RunContentUnlockSnapshot.cs`
- `Assets/ProjectEta/Scripts/Run/RunContentUnlockSnapshotService.cs`
- `Assets/ProjectEta/Tests/EditMode/Day59MetaContentAvailabilityTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/King/KingUnlockRules.cs`
- `Assets/ProjectEta/Scripts/Pieces/PieceDefinition.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardGenerator.cs`

### 삭제

없음.

## 결과

59일차 작업으로 Meta Progress의 영구 해금 데이터와 실제 런 콘텐츠 가용성 사이에 공통 연결 계층이 추가됐다.

새 런에서는 현재 영구 해금 상태를 RunId 단위 Snapshot으로 생성하고, 해당 런이 끝날 때까지 같은 Snapshot을 사용한다.

기물은 `RequiredMetaUnlockId`를 통해 영구 해금 조건을 선언할 수 있고, 카드 Reward 생성 단계에서 잠긴 기물을 제외한다.

Shop과 Event도 기존 카드 후보 생성기를 재사용하므로 동일한 영구 해금 필터를 적용할 기반이 연결됐다.

King 선택 역시 현재 Meta Progress를 실시간으로 읽는 대신 현재 런의 Snapshot을 사용하도록 정합화됐다.

Passive는 향후 실제 콘텐츠 Pool이 추가될 때 같은 구조를 사용할 수 있도록 가용성 판정 기반만 준비했다.

## 검증 상태

최신 59일차 GitHub 커밋에서 다음 변경 파일이 실제 포함된 것을 확인했다.

- `KingUnlockRules.cs`
- `MetaContentAvailabilityService.cs`
- `PieceDefinition.cs`
- `CardRewardGenerator.cs`
- `RunContentUnlockSnapshot.cs`
- `RunContentUnlockSnapshotService.cs`
- `Day59MetaContentAvailabilityTests.cs`
- 신규 C# 파일에 대응하는 `.meta` 파일

58일차 커밋 대비 59일차 커밋은 1개 커밋만 앞서 있으며, 위 파일들이 추가·수정된 상태다.

GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 최신 커밋의 변경 파일과 코드 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
