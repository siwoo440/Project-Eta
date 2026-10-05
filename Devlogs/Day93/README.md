# 93일차 : 3성 기물 Ability 연결 및 AI 평가 확장

## 개발 목표

92일차까지 완성한 공통 Ability Effect 9종을 실제 3성 기물에 연결하고, 적 AI가 기존 Move·Attack뿐 아니라 Heal·ModifyMoveRange·Summon과 같은 Active Ability까지 후보로 생성·평가·실행할 수 있도록 전투 콘텐츠를 확장했다.

3성 18종의 기존 HP·ATK·이동 규칙은 유지하면서 기획서에 고유 능력이 명시된 8종에 실제 PieceAbilityDefinition 에셋을 연결했다.

## 작업 내용

- 3성 18종 HP·ATK·이동 데이터 회귀 검증 추가
- 성기사 수호 Ability 추가
- 전차 가속 타격 Ability 추가
- 수호기사 호위 Ability 추가
- 사냥꾼 추적 Ability 추가
- 돌격대장 선봉 Ability 추가
- 전술가 전술 지시 Ability 추가
- 의무병 응급치료 Ability 추가
- 소환사 임시 소환 Ability 추가
- 3성 Ability 에셋 자동 생성·연결 Editor Patcher 추가
- 전투당 1회 피해 감소 사용 기록 추가
- 최근 이동 거리 추적 구조 추가
- 전술 지시 대상 기물 이동 보정과 중복 적용 차단 추가
- 임시 소환물 소환자별 유지 수량 제한 추가
- 소환사 임시 폰 동시 유지 1개 제한 추가
- AIActionType에 Ability 행동 종류 추가
- AI Ability 후보 메타데이터 보존 구조 추가
- Heal·ModifyMoveRange·Summon AI 후보 생성기 추가
- Ability Preview 기반 AI 점수 평가 추가
- 원거리 기물 적정 거리 평가 추가
- Support·Summoner 역할의 아군 근접 위치 평가 추가
- 적 AI의 Ability 실제 실행 및 EnemyTurn 종료 처리 추가
- AI 공격력 평가에 Aura 보정 반영
- Day93 3성 통합 회귀 테스트와 Editor 검증 메뉴 추가
- AbilityEffectData의 임시 소환 상한 직렬화 필드 누락 수정

## 3성 기물 검증

현재 3성 기물은 총 18종이다.

| PieceId | 이름 | HP | ATK |
| --- | --- | ---: | ---: |
| paladin | 성기사 | 5 | 4 |
| war_chariot | 전차 | 5 | 4 |
| grenadier | 척탄병 | 4 | 4 |
| pikeman | 장창병 | 5 | 4 |
| crossbowman | 석궁병 | 4 | 5 |
| guardian | 수호기사 | 6 | 3 |
| hunter | 사냥꾼 | 4 | 5 |
| falcon | 매 | 4 | 5 |
| unicorn | 유니콘 | 4 | 4 |
| gryphon | 그리폰 | 5 | 4 |
| dragon_horse | 용마 | 5 | 4 |
| dragon_king | 용왕 | 6 | 4 |
| artillery | 포병대 | 4 | 5 |
| vanguard | 돌격대장 | 5 | 5 |
| tactician | 전술가 | 4 | 3 |
| medic | 의무병 | 5 | 2 |
| summoner | 소환사 | 4 | 3 |
| sniper | 저격수 | 3 | 5 |

기존 PieceDatabase 등록과 MovementRules는 유지하고 Ability 연결만 추가했다.

## 연결한 고유 Ability

기획서에서 별도 능력이 명시된 8종을 실제 Ability 데이터로 연결했다.

### 성기사 — 수호

인접 아군이 피해를 받을 때 전투당 1회 피해를 1 감소시킨다.

최소 피해 규칙을 유지하며 같은 전투에서 반복 발동하지 않도록 런타임 사용 기록을 저장한다.

### 전차 — 가속 타격

최근 이동 거리가 3칸 이상이면 다음 공격 피해를 1 증가시키고 이동 거리 기록을 소비한다.

현재 전투 구조가 이동과 공격을 각각 한 행동으로 처리하기 때문에 기획서의 동일 행동 이동 후 공격을 현재 구조에 맞춰 최근 장거리 이동 기록 방식으로 연결했다.

### 수호기사 — 호위

인접한 플레이어 왕이 피해를 받을 때 전투당 1회 피해를 1 감소시킨다.

일반 아군이 아니라 왕을 대상으로 하는 보호 조건을 별도로 검사한다.

### 사냥꾼 — 추적

공격 대상의 현재 HP가 최대 HP의 절반 이하이면 공격 피해가 1 증가한다.

### 돌격대장 — 선봉

자신의 진영 기준 전방 방향으로 공격할 때 피해가 1 증가한다.

### 전술가 — 전술 지시

인접 아군 1개를 지정해 다음 행동에서 상하좌우 이동 후보를 추가한다.

ModifyMoveRange를 대상 기물에 적용하도록 확장했으며 같은 이동 보정이 이미 적용된 경우 중복 실행을 차단한다.

### 의무병 — 응급치료

자신의 행동 대신 인접 아군 기물의 HP를 1 회복한다.

Heal 공통 Executor를 그대로 사용하며 최대 HP를 넘지 않는다.

### 소환사 — 임시 소환

자신의 행동 대신 인접 빈 칸에 임시 폰을 소환한다.

TemporarySummonService에 소환자를 함께 기록하고 소환사 한 명당 살아 있는 임시 소환물을 최대 1개만 유지하도록 제한했다.

## Ability 데이터 생성

Editor Patcher를 추가해 다음 경로에 3성 Ability 에셋을 생성한다.

```text
Assets/ProjectEta/Data/Abilities/ThreeStar/
```

생성되는 주요 에셋:

- `Paladin_Guard.asset`
- `WarChariot_Charge.asset`
- `Guardian_Escort.asset`
- `Hunter_Tracking.asset`
- `Vanguard_Frontline.asset`
- `Tactician_Order.asset`
- `Medic_FirstAid.asset`
- `Summoner_TemporaryPawn.asset`

Patcher는 이미 Ability가 연결된 PieceDefinition을 강제로 덮어쓰지 않는다.

## AI Ability 후보

기존 적 AI 행동 종류를 다음과 같이 확장했다.

```text
Move
Attack
Ability
```

Active + PlayerAction Ability 중 현재 AI 후보 생성 대상은 다음과 같다.

- Heal
- ModifyMoveRange
- Summon

AI는 실제 상태를 먼저 변경하지 않고 PieceAbilityService.PreviewAbility를 사용해 실행 가능 여부와 예상 효과를 평가한다.

## AI Ability 평가

### Heal

- 실제 회복 예상량
- 대상의 손실 HP
- 최대 HP 절반 이하인 위기 아군 여부

를 점수에 반영한다.

### ModifyMoveRange

- 추가 이동 후보 수
- 공격 역할 기물 지원 여부

를 기본 점수에 반영한다.

### Summon

- 소환 기물 HP·ATK
- 소환 위치와 적 기물 거리

를 점수에 반영한다.

## 역할별 AI 평가

3성부터 중요해지는 RoleTags를 AI 점수에 추가로 반영했다.

### Ranged

적에게 지나치게 근접하는 이동은 감점하고 2~4칸 정도의 사격 거리를 확보하는 위치를 선호한다.

공격 후보도 인접 공격보다 원거리 공격에 추가 점수를 준다.

### Support / Summoner

고립되기보다 다른 아군과 가까운 위치를 선호하도록 이동 점수를 보정한다.

## AI Ability 실행

AI가 Ability 후보를 최종 선택하면 실행 직전에 Preview를 다시 수행한다.

```text
AIActionCandidate
↓
Ability 재검증
↓
PieceAbilityService.ExecuteAbility
↓
HP / 이동 보정 / 소환 상태 적용
↓
EnemyTurn 종료
```

기존 Move·Attack 실행 흐름은 유지했다.

## AI 후보 호환

Advanced Planner가 AIActionCandidate를 새 인스턴스로 재구성하는 기존 구조에서도 Ability 정보가 사라지지 않도록 AIAbilityCandidateRegistry를 추가했다.

후보 중복 제거와 평가 예산 처리에서도 AbilityId를 후보 식별 정보에 포함한다.

## 전투 피해 패시브

ThreeStarPassiveAbilityResolver를 추가해 다음 조건부 패시브를 DamageResolver의 실제 피해 적용 전에 처리한다.

- 성기사 수호
- 전차 가속 타격
- 수호기사 호위
- 사냥꾼 추적
- 돌격대장 선봉

이후 기존 PieceAbilityService의 RedirectDamage·ModifyDamage와 BattleHooks 흐름을 그대로 이어간다.

## 회귀 테스트

Day93ThreeStarIntegrationTests에서 다음을 검증한다.

- 3성 기물 정확히 18종
- 18종 PieceId 중복 없음
- 현재 HP·ATK 값 유지
- 18종 MovementRules 존재
- 명시된 8종 Ability 연결
- 성기사 수호 전투당 1회 발동
- 수호기사 왕 보호 조건
- 사냥꾼 HP 절반 이하 추가 피해
- 돌격대장 전방 추가 피해
- 전차 장거리 이동 후 첫 공격 추가 피해
- Medic AI Heal 후보 생성
- Tactician AI 이동 보정 후보 생성
- 전술 지시 동일 대상 중복 적용 차단
- Summoner 임시 폰 1개 상한
- 적 AI Ability 실행 후 EnemyTurn 종료
- 저격수 원거리 공격 후보 유지

## Editor 검증

메뉴 경로:

```text
Project Eta
└─ Day 93
   ├─ Patch Three Star Abilities
   └─ Validate Three Star Integration
```

검증 기준:

- 3성 기물 18 / 18
- 고유 Ability 연결 대상 8종 이상
- AIActionType.Ability 등록
- 원거리·지원·소환 AI 후보 구조 활성

## 주요 변경 파일

- `Assets/ProjectEta/Data/Abilities/ThreeStar/*.asset` 8종 추가
- 3성 대상 PieceDefinition 8종 Ability 참조 수정
- `Assets/ProjectEta/Editor/Day93ThreeStarAbilityPatcher.cs` 추가
- `Assets/ProjectEta/Editor/Day93ThreeStarValidationMenu.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/ThreeStarPassiveAbilityResolver.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectData.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/ModifyMoveRangeAbilityExecutor.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/MovementRangeModifierService.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/PieceAbilityService.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/SummonAbilityExecutor.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/TemporarySummonService.cs` 수정
- `Assets/ProjectEta/Scripts/Pieces/PieceRuntimeState.cs` 수정
- `Assets/ProjectEta/Scripts/Battle/DamageResolver.cs` 수정
- `Assets/ProjectEta/Scripts/AI/AIActionType.cs` 수정
- `Assets/ProjectEta/Scripts/AI/AIActionCandidate.cs` 수정
- `Assets/ProjectEta/Scripts/AI/AIAbilityCandidateRegistry.cs` 추가
- `Assets/ProjectEta/Scripts/AI/EnemyAIAbilityCandidateBuilder.cs` 추가
- `Assets/ProjectEta/Scripts/AI/EnemyAIAbilityScoreEvaluator.cs` 추가
- `Assets/ProjectEta/Scripts/AI/EnemyAIThreeStarRoleScoreEvaluator.cs` 추가
- `Assets/ProjectEta/Scripts/AI/EnemyAIPlanner.cs` 수정
- `Assets/ProjectEta/Scripts/AI/EnemyAICandidatePruner.cs` 수정
- `Assets/ProjectEta/Scripts/AI/EnemyAIActionExecutor.cs` 수정
- `Assets/ProjectEta/Scripts/AI/EnemyAIThreatScoreEvaluator.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day93ThreeStarIntegrationTests.cs` 추가

## 검증 상태

- GitHub main 최신 커밋 `cae46ed` 기준 Day93 변경 파일 확인
- 3성 Ability 에셋 8종과 PieceDefinition 연결 확인
- `_maxActiveSummons` 직렬화 필드·프로퍼티·생성자 대입 확인
- AI Ability 후보·실행 구조 확인
- Day93 통합 회귀 테스트 파일 확인
- GitHub 커밋 상태 검사에서 별도 CI 상태 체크는 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

## 다음 개발 방향

94일차에는 4성 18종의 이동·HP·ATK·복합 Ability를 실제 데이터에 연결한다.

4성부터는 오라·회복·봉쇄·장애물 파괴·복합 이동 등 3성보다 복잡한 Ability 조합이 늘어나므로 AI와 UI 상세 표시를 함께 확장한다.
