---
# 87일차 : 1~3성 합성 트리 확장 및 레시피 무결성 보정

---
## 개발 목표

86일차까지 구축한 1성 18종·2성 14종 기반을 확장해, 1→2성 21개와 2→3성 20개의 FusionRecipe를 실제 데이터로 등록하고 3성까지의 합성 성장 경로를 연결했다.

누락된 2성 5종과 3성 18종 PieceDefinition을 추가하고, 공개·숨김·동일 카드 조합·방향성 조합·결과 등급 검증을 포함한 합성 데이터 무결성 체계를 보완했다.

---
## 작업 내용

- 강습병·파수병·돌파병·전령·매복병 2성 PieceDefinition 5종 추가
- 성기사·전차·척탄병·장창병·석궁병·수호기사·사냥꾼·매·유니콘·그리폰·용마·용왕·포병대·돌격대장·전술가·의무병·소환사·저격수 3성 PieceDefinition 18종 추가
- PieceDatabase를 32종에서 55종으로 확장
- 신규 2성 5종 이동 규칙 추가
- 신규 3성 18종의 기획서 기준 임시 HP·ATK와 기본 이동 규칙 연결
- 척탄병·장창병·석궁병·그리폰·포병대·돌격대장·저격수 조건부 이동·공격 규칙 추가
- 1→2성 FusionRecipe를 21개로 확장
- 2→3성 FusionRecipe 20개 추가
- FusionRecipeDatabase를 4개에서 41개로 확장
- 공개 Recipe 37개·숨김 Recipe 4개 구성
- Special 플레이어 기물이 합성 재료로 사용될 수 있도록 FusionRuleValidator 수정
- 실제 Special King은 합성 재료에서 계속 제외하도록 판정 보정
- 재료 A/B 순서로 결과가 달라지는 방향성 Recipe 지원 추가
- 일반 Recipe는 기존처럼 재료 순서 무관 조회 유지
- FusionRecipeContentValidator와 FusionProgressionAnalyzer에 방향성 Recipe 구분 규칙 반영
- Day26·Day72·Day84·Day85·Day86 회귀 테스트 기준 갱신
- Day87 합성 확장 전용 EditMode 테스트와 Editor 검증 메뉴 추가
- 87일차 적용 후 발생한 13개 TestRunner 회귀 오류 원인 보정

---
## 현재 기물 등록 상태

| 등급 | 목표 | 현재 등록 | 미등록 |
| --- | ---: | ---: | ---: |
| 1성 | 18 | 18 | 0 |
| 2성 | 19 | 19 | 0 |
| 3성 | 18 | 18 | 0 |
| 4성 | 18 | 0 | 18 |
| 5성 | 8 | 0 | 8 |
| 합계 | 81 | 55 | 26 |

1~3성 목표 로스터는 현재 PieceDatabase에 모두 등록된 상태다.

4성 18종과 5성 8종은 후속 개발 일차에서 등록한다.

---
## 합성 레시피 등록 상태

| 구간 | 공개 | 숨김 | 합계 |
| --- | ---: | ---: | ---: |
| 1성 → 2성 | 19 | 2 | 21 |
| 2성 → 3성 | 18 | 2 | 20 |
| 3성 → 4성 | 0 | 0 | 0 |
| 4성 → 5성 | 0 | 0 | 0 |
| 현재 합계 | 37 | 4 | 41 |

전체 목표 70개 중 현재 41개가 등록됐다.

1성에서 시작해 2성·3성까지 실제 합성 성장 경로가 연결되며 다음 미등록 성장 단계는 4성이다.

---
## 동일 카드 숨김 조합

현재 동일 카드 2장을 사용하는 숨김 조합을 지원한다.

- 퀸 + 퀸 → 아마존
- 나이트 + 나이트 → 나이트라이더

동일 카드 조합도 기본 등급 상승 규칙을 따르므로 현재 두 Recipe는 `IgnoresGradeStepRule`을 사용하지 않는다.

---
## 방향성 합성 조합

기획 데이터에는 같은 두 재료를 반대 순서로 선택했을 때 서로 다른 결과가 나오는 조합이 존재한다.

- 돌파병 + 강습병 → 장창병
- 강습병 + 돌파병 → 돌격대장

이를 보존하기 위해 `FusionRecipe`에 A/B 선택 순서를 구분하는 방향성 Recipe 플래그를 추가했다.

일반 Recipe는 기존처럼 A+B와 B+A가 같은 Recipe를 찾고, 방향성 Recipe만 A/B 슬롯 순서에 따라 다른 결과를 반환한다.

---
## 합성 재료 분류 보정

87일차 레시피에는 창병·사수·캐논·카멜레온 등 Special 분류 기물이 실제 재료로 사용된다.

따라서 합성 가능 분류를 Basic·Fusion·Special로 확장했다.

다만 PieceMovementType의 기본값만으로 King을 판정하면 테스트용 임시 Basic/Fusion 기물까지 King으로 오인되는 문제가 발생해, 실제 `PieceCategory.Special + PieceMovementType.King` 조합만 합성 불가로 판정하도록 수정했다.

Monster와 Boss는 계속 합성 재료에서 제외된다.

---
## TestRunner 오류 수정

87일차 최초 적용 후 총 13개의 EditMode 회귀 오류가 확인됐다.

주요 원인은 두 가지였다.

첫째, King 제외 판정이 MovementType만 확인해 테스트용 임시 PieceDefinition까지 King으로 오인하면서 다수의 테스트에서 `MaterialNotFusable`이 발생했다.

둘째, 기존 Day72 테스트는 모든 Recipe가 재료 순서와 무관하다고 가정하고 있어 새 방향성 Recipe와 충돌했다.

수정 후 계약은 다음과 같다.

- Basic·Fusion·일반 Special 플레이어 기물은 합성 재료로 허용
- 실제 Special King은 합성 재료에서 제외
- 일반 Recipe는 A+B와 B+A 모두 같은 Recipe 반환
- 방향성 Recipe는 A/B 순서에 따라 다른 Recipe 반환 가능

---
## 회귀 검증 기준

Day87 전용 테스트에서는 다음 항목을 확인한다.

- PieceDatabase 55종 등록
- 1성 18종·2성 19종·3성 18종 등록
- FusionRecipeDatabase 41개 등록
- 1→2성 21개·2→3성 20개 등록
- 공개 37개·숨김 4개 구성
- RecipeId 중복 없음
- 잘못된 PieceDefinition 참조 없음
- 기본 등급 상승 규칙 준수
- 동일 카드 숨김 Recipe 정상 조회
- 방향성 Recipe의 A/B 결과 분기
- Special 재료 허용과 실제 King 제외
- 1성에서 3성까지 합성 경로 연결
- 다음 성장 단절 단계가 4성인지 확인

---
## 주요 변경 파일

- `Assets/ProjectEta/Data/PieceDatabase.asset` 수정
- `Assets/ProjectEta/Data/FusionRecipeDatabase.asset` 수정
- `Assets/ProjectEta/Data/AssaultTrooper.asset` 등 신규 2성 5종 추가
- `Assets/ProjectEta/Data/Paladin.asset` 등 신규 3성 18종 추가
- `Assets/ProjectEta/Data/Recipe_*.asset` 신규 Recipe 37개 추가
- `Assets/ProjectEta/Scripts/Fusion/FusionRecipe.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionRecipeDatabase.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionRuleValidator.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionRecipeContentValidator.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionProgressionAnalyzer.cs` 수정
- `Assets/ProjectEta/Scripts/Pieces/MovementConditionType.cs` 수정
- `Assets/ProjectEta/Scripts/Board/MovementRules/ConditionalMovementRule.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day87FusionExpansionTests.cs` 추가
- `Assets/ProjectEta/Tests/EditMode/Day72FusionContentTests.cs` 수정

---
## 검증 상태

- GitHub main 최신 커밋 `23a2dec` 기준 87일차 변경 파일 확인
- PieceDatabase 55종 등록 상태 확인
- FusionRecipeDatabase 41개 등록 상태 확인
- Special King 합성 제외 판정 보정 반영 확인
- 일반·방향성 Recipe 회귀 테스트 계약 반영 확인
- GitHub 커밋 상태 검사에서 별도 CI 상태 체크는 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

---
## 다음 개발 방향

88일차에는 3→4성 20개와 4→5성 9개 FusionRecipe를 등록하고 4성 18종·5성 8종 PieceDefinition을 실제 데이터에 연결한다.

4성 동일 기물 보유·배치 최대 2개, 5성 최대 1개 제한과 상위 결과 PieceDefinition 참조 무결성도 함께 검증한다.
