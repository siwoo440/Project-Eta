---
# 91일차 : 상태이상·오라·임시 소환 Ability 구현

---
## 개발 목표

90일차에 구축한 공통 Ability 프레임워크에 ApplyStatus·Aura·Summon을 실제 Executor로 추가해 총 9종 Effect 중 6종을 구현했다.

기존 상태이상 시스템을 그대로 재사용하고, Aura는 PieceDefinition 원본 스탯을 변경하지 않는 위치 기반 동적 계산 방식으로 구성했으며, Summon은 일반 카드와 분리된 전투 한정 임시 기물 생명주기로 구현했다.

---
## 작업 내용

- ApplyStatusAbilityExecutor 추가
- Poison·Burn·Stun·Root를 공통 Ability 실행 구조에 연결
- 상태 이상 면역 기물의 ApplyStatus 실행 차단 추가
- 기존 StacksAdd·RefreshDuration 규칙 재사용
- ApplyStatus Preview에서 실제 상태를 변경하지 않도록 처리
- AuraAbilityExecutor와 AuraResolver 추가
- 같은 진영·범위 내 기물의 공격력 Aura 동적 계산 추가
- 동일 AuraGroup은 가장 높은 값 하나만 적용하도록 비중첩 처리
- 서로 다른 AuraGroup 효과는 합산하도록 처리
- Aura 범위 이탈·Source 사망 시 효과가 즉시 제거되도록 처리
- CombatResolver가 BaseAtk와 현재 Aura 보정을 합산한 실제 공격력을 사용하도록 수정
- SummonAbilityExecutor 추가
- 소환 위치 Preview와 점유·장애물 배치 차단 추가
- 임시 소환용 PieceDefinition 런타임 복제 구조 추가
- PieceRuntimeState에 IsTemporarySummon 상태 추가
- 임시 소환물의 OwnedCardPool·DrawPile·DeadCardPile 진입 차단 추가
- 전투 종료 시 임시 소환물을 Board에서 자동 제거하도록 추가
- AbilityEffectRegistry를 3종에서 6종으로 확장
- Day90 회귀 테스트를 후속 Registry 확장과 호환되도록 수정
- Day91 상태이상·Aura·Summon 회귀 테스트 추가
- Day91 Editor 검증 메뉴 추가

---
## Ability Registry 상태

91일차 종료 기준 공통 Effect 구현 상태는 다음과 같다.

| EffectType | 상태 |
| --- | --- |
| Heal | 구현 |
| ModifyDamage | 구현 |
| RedirectDamage | 구현 |
| ApplyStatus | 구현 |
| Aura | 구현 |
| Summon | 구현 |
| ModifyMoveRange | 미구현 |
| BlockTile | 미구현 |
| DestroyObstacle | 미구현 |

현재 총 6 / 9종이 구현됐다.

---
## ApplyStatus

ApplyStatus는 기존 StatusEffectDefinition·RuntimeStatusEffect·PieceRuntimeState.ApplyStatus 흐름을 재사용한다.

지원 상태는 다음과 같다.

- Poison
- Burn
- Stun
- Root

대상이 해당 StatusEffectType에 면역이면 Preview 단계부터 실행을 차단한다.

Poison처럼 StacksAdd를 사용하는 상태는 기존 최대 중첩 규칙을 유지하고, Burn처럼 RefreshDuration을 사용하는 상태는 중첩 수를 늘리지 않고 지속 시간만 갱신한다.

Preview에서는 예상 적용 상태를 계산하지만 실제 StatusEffects 목록·중첩·행동 가능 상태는 변경하지 않는다.

---
## Aura

Aura는 PieceDefinition.BaseAtk 자체를 변경하지 않고 현재 Board 위치를 기준으로 실시간 공격력 보정을 계산한다.

기본 규칙은 다음과 같다.

- 같은 진영 기물만 Aura 대상
- Radius 범위 내 대상만 적용
- Aura Source가 사망하면 즉시 효과 제거
- 대상이 범위를 벗어나면 즉시 효과 제거
- IncludeSelf가 false이면 Aura Source 자신은 제외
- 동일 AuraGroupId는 가장 높은 Amount 하나만 적용
- 서로 다른 AuraGroupId는 각각 합산

예를 들어 BaseAtk 4인 기물이 같은 attack 그룹의 +1·+3 Aura와 다른 command 그룹의 +2 Aura를 받으면 실제 공격력은 4 + 3 + 2 = 9가 된다.

CombatResolver는 공격 시 AuraResolver를 통해 현재 유효한 공격력을 계산한다.

---
## Summon

Summon은 일반 카드 배치와 분리된 임시 런타임 기물로 구현했다.

실행 흐름은 다음과 같다.

```text
Summon Preview
↓
Board 점유 가능 여부 확인
↓
원본 PieceDefinition 런타임 복제
↓
PieceRuntimeState 생성
↓
Board.TryOccupyArea
↓
TemporarySummonService 등록
```

임시 소환용 PieceDefinition은 원본 에셋을 변경하지 않는 런타임 복제이며 PieceId를 비워 RunSave의 일반 PieceDefinition 복원 대상에서 제외한다.

---
## 임시 소환물 카드 풀 규칙

임시 소환물은 실제 카드가 아니므로 다음 영역에 들어가지 않는다.

- OwnedCardPool
- DrawPile
- DeadCardPile
- 카드 획득 처리
- 일반 카드 저장·복원

DeckState에서도 IsRuntimeTemporarySummonDefinition을 검사해 실수로 카드 풀 API가 호출되더라도 등록되지 않도록 방어한다.

---
## 임시 소환물 전투 종료 처리

TemporarySummonService는 소환 시 해당 TurnManager의 TurnChanged를 구독한다.

TurnState가 BattleEnded가 되면 해당 전투에 연결된 임시 소환물을 Board에서 제거하고 런타임 PieceDefinition 복제도 정리한다.

일반 카드의 DeadCardPile이나 OwnedCardPool에는 영향을 주지 않는다.

---
## 회귀 테스트

Day91StatusAuraSummonTests에 다음 검증을 추가했다.

- Ability Registry 6 / 9 구현 확인
- Poison·Burn·Stun·Root ApplyStatus 실행 확인
- 상태 이상 면역 대상 실행 차단 확인
- Poison 중첩 증가 확인
- Burn 지속 시간 갱신 확인
- ApplyStatus Preview의 상태 무변경 확인
- Aura의 같은 진영 범위 적용 확인
- 적군 Aura 미적용 확인
- 동일 AuraGroup 최고값 선택 확인
- 서로 다른 AuraGroup 합산 확인
- Aura 범위 이탈 시 효과 제거 확인
- Aura Source 사망 시 효과 제거 확인
- CombatResolver 실제 피해에 Aura 반영 확인
- Summon Preview에서 Board 상태 무변경 확인
- 빈 칸 Summon 성공 확인
- 점유 칸·장애물 칸 Summon 차단 확인
- 임시 소환물 카드 풀 비진입 확인
- BattleEnded 시 임시 소환물 자동 제거 확인

Day90 테스트도 Registry가 후속 일차에 확장돼도 Heal·ModifyDamage·RedirectDamage 핵심 계약만 검증하도록 갱신했다.

---
## Editor 검증

메뉴 경로:

```text
Project Eta
└─ Day 91
   └─ Validate Status Aura Summon
```

검증 메뉴에서는 다음을 확인한다.

- 구현 Executor 6 / 9
- ApplyStatus 등록 여부
- Aura 등록 여부
- Summon 등록 여부
- 현재 임시 소환물 추적 수
- 남은 EffectType 3종 표시

---
## 주요 변경 파일

- `Assets/ProjectEta/Scripts/Abilities/ApplyStatusAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AuraAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AuraResolver.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/SummonAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/TemporarySummonService.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectData.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectRegistry.cs` 수정
- `Assets/ProjectEta/Scripts/Battle/CombatResolver.cs` 수정
- `Assets/ProjectEta/Scripts/Cards/DeckState.cs` 수정
- `Assets/ProjectEta/Scripts/Pieces/PieceDefinition.cs` 수정
- `Assets/ProjectEta/Scripts/Pieces/PieceRuntimeState.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day90AbilityFrameworkTests.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day91StatusAuraSummonTests.cs` 추가
- `Assets/ProjectEta/Editor/Day91StatusAuraSummonValidationMenu.cs` 추가

---
## 검증 상태

- GitHub main 최신 커밋 `225d643` 기준 Day91 변경 파일 확인
- AbilityEffectRegistry 6종 연결 확인
- ApplyStatus 상태 면역 및 기존 상태 시스템 연동 확인
- AuraGroup 비중첩과 BaseAtk 비변경 구조 확인
- Summon 런타임 PieceDefinition과 Board 배치 구조 확인
- TemporarySummonService BattleEnded 정리 구조 확인
- Day91 EditMode 테스트 파일 확인
- GitHub 커밋 상태 검사에서 별도 CI 상태 체크는 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

---
## 다음 개발 방향

92일차에는 남은 ModifyMoveRange·BlockTile·DestroyObstacle 3종을 구현해 공통 Ability Effect 9종을 모두 완성한다.

임시 이동 후보 추가·봉쇄 칸 지속 시간과 UI 표시·파괴 가능 장애물 태그·파괴 후 이동 및 공격 경로 재계산·보드 상태와 저장 예외를 함께 검증한 뒤, 93일차부터 3성 18종의 실제 고유 능력 데이터를 연결한다.
