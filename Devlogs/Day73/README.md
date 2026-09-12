# 73일차 : Enemy Encounter 생성 규칙·Elite 전투 편성 정식화

## 개발 목표

72일차까지 Event 콘텐츠, Fusion Recipe 검증, RouteMap Seed UI를 정리한 뒤, 73일차에서는 **일반 전투와 Elite 전투의 적 편성을 고정 프로토타입에서 Seed 기반 Encounter 생성 구조로 확장**했다.

기존 `RoundDefinition`의 적 배치와 전투 스폰 경로는 유지하면서, 일반 Battle과 Elite에서 현재 Run의 Phase·Stage·Node에 따라 적 후보와 편성을 달리 생성하도록 구성했다.

또한 생성 결과가 잘못된 경우 기존 Round 편성으로 되돌아갈 수 있도록 검증과 fallback 경로를 함께 추가했다.

---

## Enemy Encounter 구조 추가

일반 적 편성을 별도 데이터 구조로 분리하기 위해 다음 타입을 추가했다.

- `EnemyEncounterSpawn`
- `EnemyEncounterResult`
- `EnemyEncounterRules`
- `EnemyEncounterGenerator`
- `EnemyEncounterContentValidator`

`EnemyEncounterSpawn`은 한 기물과 배치 좌표를 보관하고, `EnemyEncounterResult`는 한 전투에서 생성된 전체 적 편성을 보관한다.

Encounter 결과에는 다음 정보가 포함된다.

- Encounter Seed
- Stage Type
- 현재 Phase
- 현재 Stage
- RouteMap NodeId
- 전체 ThreatScore
- 실제 적 Spawn 목록

이를 통해 전투 시작 시 어떤 조건으로 어떤 적 편성이 생성됐는지 로그와 테스트에서 추적할 수 있도록 했다.

---

## 일반 Enemy Pool 규칙

`EnemyEncounterRules.CanUsePiece`를 기준으로 일반 적 후보 Pool을 구성한다.

현재 후보에서 제외되는 기물은 다음과 같다.

- `King` 이동 타입
- `Fusion` 카테고리
- `Boss` 카테고리

기본 기물, Monster, Special 기물은 일반 Enemy 후보로 사용할 수 있다.

실제 후보 Pool은 `PlayerStartingDeck26` 카탈로그와 전체 `PieceDefinition` Resources를 함께 조회하여 구성하며, 같은 에셋은 중복 등록하지 않는다.

---

## ThreatScore 기준

적 후보의 강도를 비교하기 위해 기물별 `ThreatScore`를 계산한다.

현재 규칙은 다음 값을 사용한다.

- HP × 2
- ATK × 3
- Monster 카테고리 +3
- Special 카테고리 +1

생성기는 후보를 ThreatScore 순으로 정렬한 뒤 현재 Phase에서 사용할 수 있는 후보 범위를 결정한다.

Encounter 전체의 `ThreatScore`도 선택된 모든 적 기물의 점수를 합산하여 기록한다.

---

## Phase별 Enemy 품질 변화

후반 Phase로 갈수록 강한 적이 후보 Pool에 포함되도록 최대 후보 범위를 단계적으로 확장했다.

현재 최대 후보 범위는 다음과 같다.

- Phase 1 : 약 55%
- Phase 2 : 약 70%
- Phase 3 : 약 85%
- Phase 4 : 약 95%
- Phase 5 : 100%

동시에 후반 Phase에서는 너무 약한 후보가 계속 선택되지 않도록 최소 후보 범위도 올린다.

- Phase 1~2 : 최소 제한 없음
- Phase 3 : 하위 약 10% 제외
- Phase 4 : 하위 약 20% 제외
- Phase 5 : 하위 약 30% 제외

이 구조를 통해 같은 일반 Battle이라도 Run이 진행될수록 더 높은 ThreatScore의 적 편성이 등장할 수 있도록 했다.

---

## Elite 전투 차등화

기존 Elite는 일반 RoundDefinition에 임시 적 1기를 추가하는 수준이었다.

73일차에서는 Elite도 동일한 Encounter 생성기를 사용하되 일반 Battle보다 강한 후보와 더 많은 적을 선택하도록 변경했다.

현재 Elite 규칙은 다음과 같다.

- 현재 Phase 후보 범위의 상위 절반을 중심으로 선택
- 첫 번째 Elite 적은 현재 후보 범위에서 가장 강한 기물 선택
- 초반에는 일반 전투보다 적 1기 추가
- Phase 3 이상 또는 Stage 6 이상에서는 적 2기 추가
- 추가 Elite 적은 적 진영 후방 영역을 우선 사용

기존 `StageDefinitionCatalog`의 Elite 보상 프로필 분리는 유지되며, 이번 작업은 전투 편성 자체의 차이를 만드는 데 집중했다.

---

## Encounter Seed 재현성

Enemy Encounter는 다음 값을 조합하여 결정적 Seed를 생성한다.

- RouteMap `MapSeed`
- 현재 Phase
- 현재 Stage
- `StageType`
- 현재 `NodeId`

같은 Run 조건에서 같은 노드와 같은 Stage Type으로 전투를 생성하면 같은 Encounter 결과를 다시 만들 수 있다.

반대로 Phase, Stage, Node 또는 일반/Elite 타입이 달라지면 다른 Seed를 사용한다.

이를 통해 RouteMap Seed 기반의 Run 재현성을 전투 적 편성까지 확장했다.

---

## 적 배치 규칙

기존 `RoundDefinition.InitialEnemies`의 배치 좌표는 가능한 경우 그대로 활용한다.

기존 좌표를 사용할 수 없거나 Elite 추가 적처럼 별도 좌표가 필요한 경우 적 진영에서 결정적 순서로 빈 Cell을 선택한다.

일반 대체 배치는 보드 상단 절반의 적 진영을 사용하고, Elite 추가 적은 가능한 경우 더 뒤쪽 행부터 배치한다.

같은 Encounter 안에서 하나의 Cell이 중복 사용되지 않도록 별도 Cell 집합을 관리한다.

---

## StageBattleRuntimeController 연동

`StageBattleRuntimeController`의 일반 Battle·Elite 시작 적 생성 경로를 새 Encounter 시스템으로 변경했다.

현재 전투 타입별 동작은 다음과 같다.

- `Battle` : Seed 기반 Enemy Encounter 생성
- `Elite` : 강화된 Seed 기반 Enemy Encounter 생성
- `MidBoss` / `FinalBoss` : 기존 `RoundDefinition`과 Boss 생성 구조 유지

Enemy 후보는 현재 Piece 카탈로그와 Resources에서 수집하고, Encounter 생성 후 무결성 검사를 거친 뒤 기존 `SpawnTestEnemy` 경로를 사용하여 실제 보드에 생성한다.

기존 Reinforcement 시스템은 변경하지 않고 `RoundDefinition.Reinforcements`를 그대로 사용한다.

---

## Encounter fallback 처리

새 Encounter가 잘못 생성되어 전투 자체가 막히는 상황을 줄이기 위해 fallback 처리를 추가했다.

다음 상황에서는 기존 `RoundDefinition.InitialEnemies` 편성으로 되돌아간다.

- Encounter Validator가 문제를 발견한 경우
- 생성된 Encounter 적이 실제 보드에 한 기도 배치되지 못한 경우

따라서 새 적 편성 규칙을 확장하면서도 기존 전투 데이터 경로를 안전망으로 유지한다.

---

## Encounter 무결성 검증

`EnemyEncounterContentValidator`를 추가하여 생성 결과를 실제 배치 전에 검사한다.

검사 대상은 다음과 같다.

- 빈 Encounter
- null Spawn
- null Piece
- 금지 기물 포함
- 보드 밖 또는 적 진영 밖 좌표
- 같은 Cell 중복 배치

검증 결과에 문제가 있으면 새 Encounter 적용을 중단하고 기존 Round 편성을 사용한다.

---

## 테스트 추가

`Day73EnemyEncounterTests`를 추가하여 다음 항목을 검증하도록 했다.

- 같은 Seed와 조건에서 같은 Enemy Encounter 생성
- Phase가 달라지면 Encounter Seed 변경
- Elite가 일반 Battle보다 적 수와 ThreatScore가 낮지 않음
- King / Fusion / Boss 기물이 일반 Encounter에서 제외됨
- Validator가 중복 Cell을 감지함

테스트에서 사용하는 PieceDefinition과 RoundDefinition은 런타임 ScriptableObject로 생성하고 테스트 종료 시 정리하도록 구성했다.

---

## 주요 변경 파일

### Enemy Encounter

- `Assets/ProjectEta/Scripts/Run/EnemyEncounterSpawn.cs`
- `Assets/ProjectEta/Scripts/Run/EnemyEncounterResult.cs`
- `Assets/ProjectEta/Scripts/Run/EnemyEncounterRules.cs`
- `Assets/ProjectEta/Scripts/Run/EnemyEncounterGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/EnemyEncounterContentValidator.cs`

### Battle 연동

- `Assets/ProjectEta/Scripts/Run/StageBattleRuntimeController.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day73EnemyEncounterTests.cs`

---

## 현재 확인 상태

최신 `main`의 Day73 커밋에는 Enemy Encounter 생성 구조, Phase별 후보 규칙, Elite 편성 차등화, Encounter 검증 및 EditMode 테스트 코드가 반영되어 있다.

GitHub 저장소에는 현재 이 커밋에 연결된 CI Status 또는 GitHub Actions Workflow Run이 확인되지 않는다.

따라서 원격 저장소 기준으로 확인 가능한 범위는 다음과 같다.

- Day72 대비 Day73 변경 파일 반영 상태
- Enemy Encounter 관련 신규 코드 반영 상태
- `StageBattleRuntimeController` 연동 상태
- `Day73EnemyEncounterTests` 존재 여부
- 최신 커밋의 파일 구성과 코드 구조 검토

Unity Editor의 실제 컴파일 성공과 EditMode Test 전체 통과 여부는 로컬 Unity 실행 결과로 최종 확인해야 한다.

---

## 다음 개발 방향

이번 단계에서는 **적의 종류별 개별 행동보다 Encounter 편성 규칙과 Elite 난이도 차등화 기반을 먼저 구축**했다.

다음 단계에서는 현재 Enemy Pool과 ThreatScore를 기반으로 적 역할별 AI 우선순위, Elite 전용 전투 패턴, 개별 적 특성, 실제 전투 밸런스를 확장하는 방향으로 진행한다.
