# 92일차 : 보드 조작 Ability 및 공통 Effect 9종 완성

## 개발 목표

91일차까지 구현한 공통 Ability 6종에 ModifyMoveRange·BlockTile·DestroyObstacle을 추가해 공통 Effect 9종을 모두 완성하고, 이후 3~5성 기물의 실제 고유 능력을 데이터로 연결할 수 있는 보드 조작 기반을 마무리했다.

## 작업 내용

- ModifyMoveRange Ability Executor와 런타임 이동 범위 보정 구조 추가
- MovementResolver에 Ability 이동 후보 후처리와 봉쇄 칸 필터 추가
- 물리 장애물과 Ability 기반 임시 봉쇄 상태 분리
- TileBlockState·TileBlockService와 다음 PlayerTurn 해제 규칙 추가
- BlockTile Preview·Execute와 점유·장애물 칸 차단 규칙 추가
- 파괴 가능 장애물 속성과 DestroyObstacle Executor 추가
- 장애물 파괴 후 이동 경로 즉시 재계산 구조 추가
- AbilityEffectRegistry를 공통 Effect 9종 구현 상태로 확장
- Day91 회귀 검증을 후속 Registry 확장과 호환되도록 수정
- Day92 보드 Ability 회귀 테스트와 Editor 검증 메뉴 추가
- Unity NUnit Vector2Int 컬렉션 검증 호환 오류 수정

## 공통 Ability 구현 상태

| EffectType | 상태 |
| --- | --- |
| Heal | 구현 |
| ModifyDamage | 구현 |
| ApplyStatus | 구현 |
| Aura | 구현 |
| Summon | 구현 |
| ModifyMoveRange | 구현 |
| BlockTile | 구현 |
| DestroyObstacle | 구현 |
| RedirectDamage | 구현 |

92일차 종료 기준 공통 Ability Effect는 9 / 9 구현 상태다.

## ModifyMoveRange

ModifyMoveRange는 PieceDefinition의 원본 MovementRuleData를 변경하지 않고 MovementResolver가 기본 후보를 계산한 뒤 런타임 보정 후보를 합치는 방식으로 구성했다.

AbilityEffectData.Amount는 추가 이동 거리로 사용하고 Vector가 지정되면 해당 방향, 지정되지 않으면 8방향을 기준으로 추가 후보를 계산한다.

추가 후보도 보드 밖·장애물·Ability 봉쇄·점유 상태를 검사하며 Root 상태처럼 CanMove가 false인 경우 이동 후보를 새로 만들지 않는다.

## BlockTile

기존 TileState의 물리 장애물과 Ability 봉쇄를 분리했다.

- HasObstacle: 실제 물리 장애물
- IsObstacleDestructible: 파괴 가능 여부
- IsBlockedByAbility: 일시 Ability 봉쇄
- IsBlocked: 물리 장애물 또는 Ability 봉쇄 통합 진입 차단

BlockTile은 빈 칸만 대상으로 하며 점유 칸·물리 장애물 칸·이미 봉쇄된 칸에는 사용할 수 없다.

기본 지속 시간 1은 생성된 PlayerTurn의 다음 PlayerTurn 시작 시 해제되는 방식으로 처리하고 BattleEnded에서도 남은 봉쇄를 정리한다.

## DestroyObstacle

DestroyObstacle은 실제 물리 장애물 중 IsObstacleDestructible이 true인 대상만 제거한다.

파괴 불가 장애물·빈 칸·Ability 봉쇄는 DestroyObstacle 대상이 아니다.

실제 장애물을 제거한 뒤 MovementResolver를 다시 호출하면 최신 BoardState를 읽어 이동·공격 경로를 즉시 재계산한다.

## 회귀 테스트

Day92BoardAbilityTests에서 다음을 검증한다.

- AbilityEffectRegistry 9 / 9 구현
- ModifyMoveRange Preview 상태 비변경
- 추가 이동 후보 생성
- Root 상태 이동 제한 유지
- 장애물·봉쇄 뒤 후보 차단
- BlockTile Preview 상태 비변경
- 점유·장애물 칸 봉쇄 실패
- 다음 PlayerTurn 시작 시 봉쇄 해제
- 슬라이드 이동 경로의 봉쇄 적용
- DestroyObstacle Preview 상태 비변경
- 파괴 가능 장애물 제거
- 파괴 불가 장애물·빈 칸 실패
- 장애물 파괴 후 이동 경로 즉시 복구
- 기존 Day91 상태이상·Aura·Summon 회귀 유지

Unity 내장 NUnit 버전에서 Vector2Int를 Does.Contain/Does.Not.Contain으로 검사할 때 문자열용 오버로드로 해석되는 컴파일 오류가 발생해 List<Vector2Int>.Contains 결과를 Is.True/Is.False로 검증하도록 수정했다.

## Editor 검증

메뉴 경로:

```text
Project Eta
└─ Day 92
   └─ Validate Board Ability Framework
```

검증 항목:

- Ability EffectType 9종 존재
- AbilityEffectRegistry 9 / 9
- ModifyMoveRange Executor 등록
- BlockTile Executor 등록
- DestroyObstacle Executor 등록
- 현재 TileBlock 수
- 현재 MovementModifier 수

## 주요 변경 파일

- `Assets/ProjectEta/Scripts/Abilities/ModifyMoveRangeAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/MovementModifierResolver.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/MovementRangeModifierService.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/MovementRangeModifierState.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/BlockTileAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/TileBlockService.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/TileBlockState.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/DestroyObstacleAbilityExecutor.cs` 추가
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectRegistry.cs` 수정
- `Assets/ProjectEta/Scripts/Abilities/AbilityEffectData.cs` 수정
- `Assets/ProjectEta/Scripts/Board/TileState.cs` 수정
- `Assets/ProjectEta/Scripts/Board/BoardState.cs` 수정
- `Assets/ProjectEta/Scripts/Board/MovementResolver.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day91StatusAuraSummonTests.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day92BoardAbilityTests.cs` 추가 및 NUnit 호환 수정
- `Assets/ProjectEta/Editor/Day92BoardAbilityValidationMenu.cs` 추가

## 검증 상태

- GitHub main 최신 커밋 `ec0fef2` 기준 Day92 변경 내용 확인
- 보고된 Vector2Int NUnit 컴파일 오류 위치 수정 확인
- AbilityEffectRegistry 9 / 9 구조 확인
- BlockTile·DestroyObstacle·ModifyMoveRange 연결 확인
- GitHub CI 상태 체크는 별도 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

## 다음 개발 방향

93일차에는 완성된 공통 Ability 9종을 사용해 3성 18종의 HP·ATK·이동·실제 고유 능력을 전투 데이터에 연결하고, 원거리·지원·능력 효과를 적 AI 후보 평가가 함께 고려하도록 확장한다.
