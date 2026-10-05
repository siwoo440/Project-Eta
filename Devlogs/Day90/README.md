---
# 90일차 : 공통 Ability 프레임워크 및 피해·회복 핵심 효과 구축

---
## 개발 목표

89일차까지 완성한 81종 기물·70개 합성 트리 위에 3~5성 고유 능력을 공통 방식으로 연결할 수 있는 Ability 프레임워크를 구축했다.

기물마다 전용 코드를 추가하는 방식 대신 Heal·ModifyDamage·ApplyStatus·Aura·Summon·ModifyMoveRange·BlockTile·DestroyObstacle·RedirectDamage의 9종 공통 EffectType을 정의하고, 압축된 90일차 범위에 맞춰 Heal·ModifyDamage·RedirectDamage 3종을 실제 전투 흐름에 우선 연결했다.

---
## 작업 내용

- 공통 AbilityEffectType 9종 추가
- AbilityTrigger와 AbilityActionCost 실행 규칙 추가
- AbilityEffectData 공통 효과 데이터 구조 추가
- PieceAbilityDefinition ScriptableObject 추가
- AbilityExecutionContext 공통 실행 컨텍스트 추가
- AbilityExecutionResult 공통 결과 구조 추가
- IAbilityEffectExecutor 공통 Effect 실행 인터페이스 추가
- AbilityEffectRegistry 공통 Executor 등록 구조 추가
- PieceAbilityService Preview·Execute 실행 흐름 추가
- PieceDefinition에 복수 PieceAbilityDefinition 연결 슬롯 추가
- HealAbilityExecutor 실제 회복 처리 추가
- ModifyDamageAbilityExecutor 피해 증가·감소 처리 추가
- RedirectDamageAbilityExecutor 피해 대상 전환 처리 추가
- DamageContext에 OriginalTarget·실제 Target·OriginalAmount·RedirectCount 추가
- DamageResolver에 공통 Ability BeforeDamage 처리 연결
- 실제 Board 내부 Redirect 후보 탐색용 AbilityBoardRegistry 추가
- Redirect 후보 우선순위와 한 피해 이벤트 1회 제한 추가
- 기존 BattleHooks.BeforeDamage 호환 유지
- Day90 Ability 프레임워크 회귀 테스트 추가
- Day90 Editor 검증 메뉴 추가

---
## 공통 Ability EffectType

현재 공통 Ability 효과는 다음 9종으로 정의했다.

| EffectType | 역할 | 90일차 상태 |
| --- | --- | --- |
| Heal | 아군 HP 회복 | 구현 |
| ModifyDamage | 최종 피해량 증가·감소 | 구현 |
| ApplyStatus | 상태 이상 부여 | 후속 구현 |
| Aura | 범위 지속 효과 | 후속 구현 |
| Summon | 임시 기물 소환 | 후속 구현 |
| ModifyMoveRange | 이동 후보 변경 | 후속 구현 |
| BlockTile | 칸 일시 봉쇄 | 후속 구현 |
| DestroyObstacle | 파괴 가능 장애물 제거 | 후속 구현 |
| RedirectDamage | 아군 피해 대신 받기 | 구현 |

90일차에서는 전체 9종의 공통 호출 규약을 먼저 고정하고 전투 피해·회복 계열 3종만 실제 Executor를 등록했다.

---
## Ability 데이터 구조

PieceDefinition에 다음 연결점을 추가했다.

```text
PieceDefinition
└─ PieceAbilityDefinition[]
   └─ AbilityEffectData[]
```

한 기물은 여러 Ability를 가질 수 있고 하나의 Ability도 여러 Effect를 조합할 수 있다.

기존 81종 PieceDefinition은 Ability 배열이 비어 있어도 안전하게 동작하도록 구성했으며 실제 3·4·5성 고유 능력 데이터 연결은 후속 일차에서 진행한다.

---
## 실행 Trigger와 행동 비용

AbilityTrigger는 다음 실행 시점을 표현한다.

- Active
- BeforeMove
- AfterMove
- BeforeAttack
- AfterAttack
- BeforeDamage
- AfterDamage
- TurnStart
- TurnEnd
- Passive

AbilityActionCost는 다음 두 종류다.

- None
- PlayerAction

플레이어가 직접 사용하는 액티브 Ability는 PlayerAction을 사용할 수 있고 피해 감소·Redirect 같은 자동 패시브는 행동을 소비하지 않는다.

---
## Preview와 Execute

공통 Ability는 실제 상태를 바꾸기 전 결과를 계산할 수 있도록 Preview와 Execute를 분리했다.

Preview는 예상 회복량·피해량·실제 대상 등을 반환하지만 HP·보드·턴 상태를 변경하지 않는다.

Execute는 Preview 검증을 먼저 수행한 뒤 모든 Effect가 실행 가능한 경우에만 실제 상태를 변경한다.

미구현 Effect가 포함된 복합 Ability는 실제 실행 전 실패해 앞쪽 Effect만 부분 적용되는 상황을 방지한다.

---
## Heal

Heal은 다음 규칙을 사용한다.

- 사망 기물은 회복 불가
- 다른 진영 기물 회복 차단
- 회복량은 1 이상
- 최대 HP는 PieceDefinition.BaseHp 기준
- 최대 HP를 초과하는 회복은 실제 부족 HP만 적용
- Preview에서는 실제 HP를 변경하지 않음

예를 들어 현재 HP 8 / 최대 HP 10 기물에 Heal 5를 사용하면 실제 회복량은 2이며 최종 HP는 10이 된다.

---
## ModifyDamage

ModifyDamage는 DamageContext.Amount를 기준으로 피해를 증가하거나 감소시킨다.

BeforeDamage 단계에서 공격자 효과를 먼저 적용하고 Redirect가 발생했다면 실제 피격자의 ModifyDamage를 다음에 적용한다.

일반 ModifyDamage 감소 효과는 양수 피해를 최소 1까지 유지한다.

기존 BattleHooks.BeforeDamage는 공통 Ability 처리 이후에도 최종 DamageContext를 다시 수정할 수 있으므로 기존 보호막 테스트처럼 피해를 명시적으로 0으로 만드는 흐름은 유지된다.

---
## RedirectDamage

RedirectDamage는 피해 이벤트가 처음 향한 OriginalTarget과 실제 피해를 받을 Target을 분리한다.

기본 규칙은 다음과 같다.

- 보호자와 피해 대상은 같은 진영이어야 함
- 보호자는 생존 상태여야 함
- 자신에게 들어온 피해를 자기 자신에게 Redirect할 수 없음
- 현재 피해 발생원은 Redirect 후보에서 제외
- Effect Radius 안의 보호자만 후보가 됨
- 한 피해 이벤트에서 Redirect는 최대 1회
- Redirect 후에는 실제 피격자의 ModifyDamage를 적용

여러 보호자가 동시에 후보인 경우 결과 재현성을 위해 다음 우선순위를 사용한다.

1. 피해 대상과 가까운 거리
2. 같은 거리면 Board Y 좌표
3. 같은 Y면 X 좌표
4. 그래도 같으면 PieceId 순

랜덤 선택을 사용하지 않아 AI·Save/Load·테스트에서도 같은 결과를 재현할 수 있다.

---
## 피해 처리 흐름

90일차 이후 기본 Damage 처리 순서는 다음과 같다.

```text
DamageContext 생성
        ↓
RedirectDamage 후보 탐색
        ↓
실제 피해 Target 확정
        ↓
공격자 ModifyDamage
        ↓
실제 피격자 ModifyDamage
        ↓
기존 BattleHooks.BeforeDamage
        ↓
최종 HP 감소
        ↓
BattleHooks.AfterDamage
```

기존 Ability가 없는 기물은 이전 DamageResolver와 동일한 피해 결과를 유지한다.

---
## Board 탐색 구조

Redirect 후보는 전역 PieceRuntimeState 목록에서 찾지 않고 피해 대상이 실제 점유한 BoardState를 기준으로 탐색한다.

BoardState가 생성될 때 AbilityBoardRegistry에 등록되고 RedirectDamage 처리 시 피해 대상이 포함된 Board를 찾아 그 보드의 고유 PieceRuntimeState만 후보로 수집한다.

이를 통해 다른 테스트나 다른 전투 Board의 기물이 Redirect 후보에 섞이는 것을 방지한다.

---
## 회귀 테스트

Day90AbilityFrameworkTests에 다음 검증을 추가했다.

- AbilityEffectType 9종 정의 확인
- 90일차 기본 Executor 3종 등록 확인
- 동일 Effect Executor 중복 등록 차단
- Ability 없는 기존 PieceDefinition의 빈 배열 호환
- 한 기물에 복수 Ability 연결 확인
- 한 Ability에 복수 Effect 연결 확인
- Heal Preview 상태 무변경 확인
- Heal 최대 HP 제한 확인
- Active Heal의 PlayerAction 소비 확인
- 미구현 Effect 포함 Ability의 부분 실행 차단
- 공격자·피격자 ModifyDamage 순차 적용 확인
- ModifyDamage 최소 피해 1 확인
- RedirectDamage의 실제 피해 대상 전환 확인
- 동일 거리 Redirect 후보의 결정적 우선순위 확인
- Redirect 후 보호자 ModifyDamage 적용 확인
- 한 DamageContext에서 Redirect 1회 제한 확인
- 기존 BattleHooks.BeforeDamage의 0 피해 차단 호환 확인
- Ability 없는 기물의 기존 DamageResolver 결과 유지 확인

---
## Editor 검증

메뉴 경로:

```text
Project Eta
└─ Day 90
   └─ Validate Ability Framework
```

현재 검증 기준은 다음과 같다.

- Ability EffectType 9종 존재
- Heal Executor 등록
- ModifyDamage Executor 등록
- RedirectDamage Executor 등록
- 현재 실제 PieceDefinition Ability 연결 수 표시

3~5성 실제 Ability 데이터 연결은 후속 93~95일차 범위이므로 현재 기물 Ability 수가 0이어도 정상이다.

---
## 주요 변경 파일

- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectType.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityTrigger.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityActionCost.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectData.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/PieceAbilityDefinition.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityExecutionContext.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityExecutionResult.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/IAbilityEffectExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectRegistry.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/PieceAbilityService.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/HealAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/ModifyDamageAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/RedirectDamageAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityBoardRegistry.cs` 추가
- `Assets/ProjectEta/Scripts/Pieces/PieceDefinition.cs` 수정
- `Assets/ProjectEta/Scripts/Battle/DamageContext.cs` 수정
- `Assets/ProjectEta/Scripts/Battle/DamageResolver.cs` 수정
- `Assets/ProjectEta/Scripts/Board/BoardState.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day90AbilityFrameworkTests.cs` 추가
- `Assets/ProjectEta/Editor/Day90AbilityFrameworkValidationMenu.cs` 추가

---
## 검증 상태

- GitHub main 최신 커밋 `c044a39` 기준 Day90 변경 파일 확인
- PieceDefinition Ability 슬롯 반영 확인
- DamageContext Redirect 상태 구조 반영 확인
- DamageResolver 공통 Ability 처리 연결 확인
- Preview·Execute 구조와 Redirect 우선순위 반영 확인
- Day90 EditMode 회귀 테스트 파일 확인
- GitHub 커밋 상태 검사에서 별도 CI 상태 체크는 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

---
## 다음 개발 방향

91일차에는 ApplyStatus·Aura·Summon을 공통 Ability Executor로 구현한다.

기존 독·화상·기절·속박 StatusEffect 시스템을 ApplyStatus와 연결하고, 동일 계열 Aura의 비중첩·범위 진입/이탈 갱신과 임시 소환물의 생성·행동·사망·전투 종료 제거 및 카드 풀 비진입 규칙을 함께 검증한다.
