---
# 89일차 : 영구 합성 도감 및 전체 성장 트리 검증

---
## 개발 목표

88일차에 완성한 플레이어 기물 81종과 FusionRecipe 70개를 기준으로 전체 합성 성장 경로를 최종 검증하고, 숨김 Recipe 7개의 발견 상태를 런을 넘어 유지되는 영구 Fusion Atlas 데이터로 승격했다.

기존 런 단위 FusionDiscoveryLog는 구버전 저장 호환과 현재 런 기록을 위해 유지하면서, MetaProgressState에 영구 발견 Recipe ID를 추가하고 결과 기물·재료 기물·5성 도달 경로를 빠르게 조회할 수 있는 검색 인덱스를 구축했다.

---
## 작업 내용

- MetaProgressSaveData 저장 버전을 v3으로 확장
- MetaProgressState에 영구 FusionRecipe 발견 ID 저장 구조 추가
- 숨김 Recipe 최초 발견 시 meta_progress.json에 영구 저장하도록 확장
- 기존 RunSave 발견 Recipe를 영구 Fusion Atlas로 병합하는 호환 경로 추가
- FusionDiscoveryLog가 현재 런과 영구 메타 발견 상태를 함께 조회하도록 수정
- RecipeId·결과 기물·재료 기물 검색용 FusionRecipeIndex 추가
- 특정 기물에서 도달 가능한 5성 결과·경로 검색 기능 추가
- 영구 발견 상태를 기준으로 숨김 Recipe 표시 여부를 판단하는 FusionAtlasService 추가
- 81종·70 Recipe 전체 성장 경로 검증용 FusionTreeValidator 추가
- 2~5성 모든 기물의 제작 Recipe 존재 여부 검증 추가
- 킹 제외 1~4성 기물의 상위 합성 재료 사용 여부 검증 추가
- 2~5성 전체 기물의 1성 시작 도달 가능 여부 검증 추가
- Meta v2 저장 파일을 v3에서 빈 Fusion Atlas로 호환 복원하도록 수정
- Day89 Fusion Atlas·전체 트리 EditMode 테스트 추가
- Day89 Editor 검증 메뉴 추가

---
## 영구 Fusion Atlas 저장 구조

기존 숨김 Recipe 발견 기록은 RunState 내부 FusionDiscoveryLog와 RunSaveData.discoveredRecipeIds에만 저장됐다.

89일차부터는 기존 런 단위 기록을 유지하면서 MetaProgressState에 별도 영구 발견 집합을 추가했다.

```text
RunState
└─ FusionDiscoveryLog
   └─ 현재 런 발견 Recipe ID

MetaProgressState
└─ DiscoveredFusionRecipeIds
   └─ 런을 넘어 유지되는 영구 Fusion Atlas
```

숨김 Recipe를 실제로 처음 합성하면 현재 런 발견 기록과 영구 메타 발견 기록을 함께 갱신하고 MetaProgressService.Save()를 통해 저장 파일에 반영한다.

새 런에서도 MetaProgressState에 RecipeId가 남아 있으므로 이미 발견한 숨김 Recipe를 다시 미발견 상태로 처리하지 않는다.

---
## Meta 저장 포맷

MetaProgressSaveData.CurrentVersion을 2에서 3으로 올렸다.

새 필드는 다음과 같다.

```text
discoveredFusionRecipeIds
```

기존 v1·v2 저장 파일에는 해당 필드가 없으므로 null 목록을 빈 Fusion Atlas로 처리한다.

따라서 기존 메타 토큰·기물 해금·킹 해금·패시브 해금·런 보상 Claim 기록은 그대로 복원되며 새 Fusion Atlas만 빈 상태로 시작한다.

---
## 기존 Run Save 발견 기록 승격

기존 RunSaveData.discoveredRecipeIds와 FusionDiscoveryLog는 삭제하지 않았다.

기존 저장 런을 복원했을 때 현재 런 발견 목록을 다시 FusionDiscoveryLog에 넣은 뒤, 실제 플레이 상태에서는 영구 MetaProgressState와 병합한다.

이미 영구 도감에 존재하는 RecipeId는 HashSet 기준으로 중복 저장하지 않는다.

이 구조로 89일차 이전 런에서 발견한 숨김 Recipe도 기존 런 저장 파일에 기록이 남아 있다면 영구 Fusion Atlas로 승격할 수 있다.

---
## Fusion Recipe 검색 인덱스

FusionRecipeIndex를 추가해 70개 Recipe를 다음 기준으로 인덱싱한다.

- RecipeId → FusionRecipe
- Result PieceId → 해당 기물을 제작하는 Recipe 목록
- Material PieceId → 해당 기물을 재료로 사용하는 Recipe 목록

동일 카드 Recipe는 재료 인덱스에 중복 등록하지 않는다.

기존 FusionRecipeDatabase의 실제 Recipe 에셋을 원본 데이터로 사용하므로 별도의 합성 목록을 수동으로 중복 관리하지 않는다.

---
## Fusion Atlas 검색 기능

FusionAtlasService를 통해 다음 검색을 제공한다.

### 이 기물을 만드는 방법

결과 PieceId를 기준으로 해당 기물의 제작 Recipe를 조회한다.

숨김 Recipe는 영구 발견 상태에 따라 표시 여부를 결정한다.

### 이 기물이 재료로 들어가는 조합

Material PieceId를 기준으로 상위 합성에 사용되는 모든 Recipe를 조회한다.

하나의 기물이 여러 상위 기물로 분기되는 경우도 같은 인덱스에서 모두 반환한다.

### 도달 가능한 5성 경로

선택한 기물에서 시작해 해당 기물이 재료로 들어가는 상위 Recipe를 재귀적으로 따라가며 최종 5성 결과까지의 경로를 계산한다.

각 경로는 FusionAtlasPath로 저장하며 시작 PieceId, 단계별 Recipe 목록, 최종 5성 결과를 조회할 수 있다.

---
## 숨김 Recipe 표시 규칙

현재 전체 Recipe 70개 중 숨김 Recipe는 7개다.

영구 Fusion Atlas 조회에서는 기본적으로 다음 규칙을 사용한다.

- 공개 Recipe는 항상 표시
- 숨김 Recipe는 MetaProgressState에 발견 RecipeId가 있을 때 표시
- 개발·검증용 revealUndiscoveredHidden 옵션에서는 미발견 숨김 Recipe도 조회 가능

---
## 전체 성장 트리 검증

FusionTreeValidator를 추가해 기존 등급 단위 진단보다 강한 전체 트리 검증을 수행한다.

확인 항목은 다음과 같다.

- 플레이어 PieceDefinition 81종
- FusionRecipe 70개
- 공개 Recipe 63개
- 숨김 Recipe 7개
- 2~5성 모든 기물에 최소 1개의 제작 Recipe 존재
- 킹 제외 합성 가능한 1~4성 모든 기물이 최소 1개의 상위 Recipe 재료로 사용
- 모든 Recipe의 MaterialA·MaterialB·Result가 PieceDatabase에 등록
- 전체 Recipe의 기존 규칙·등급·중복 검증 통과
- 1성 합성 재료에서 시작해 모든 2~5성 결과에 도달 가능

문제가 있는 경우 제작법 누락, 상위 재료 사용 누락, 도달 불가 PieceId, 잘못된 Recipe 참조를 각각 별도 목록으로 반환한다.

---
## 회귀 테스트

Day89FusionAtlasTests에 다음 검증을 추가했다.

- 영구 발견 Recipe의 Meta 저장·복원 확인
- 동일 숨김 Recipe 중복 발견 차단 확인
- Meta v2 JSON의 v3 호환 로드 확인
- 빈 Meta 상태에서 공개 Recipe 63개만 노출되는지 확인
- 숨김 Recipe 발견 후 표시 수가 증가하는지 확인
- 숨김 7개 전체의 영구 저장·복원 확인
- 기존 Run Discovery를 Meta Progress로 병합하는지 확인
- 결과 기물 기준 제작 Recipe 인덱스 확인
- 재료 기물 기준 사용 Recipe 인덱스 확인
- 실제 기물에서 5성 결과로 도달하는 경로 확인
- 81종·70 Recipe 전체 성장 트리 완전성 확인
- 숨김 Recipe가 영구 발견 전에는 Atlas 검색에서 가려지는지 확인

---
## Editor 검증

메뉴 경로:

```text
Project Eta
└─ Day 89
   └─ Validate Fusion Atlas And Full Tree
```

검증 로그에는 다음 정보를 표시한다.

- 기물 81종 등록 상태
- Recipe 70개 등록 상태
- 공개 63개·숨김 7개 상태
- 제작 Recipe 누락 수
- 상위 합성 재료 사용 누락 수
- 1성 시작 도달 불가 기물 수
- PieceDefinition 참조 오류 수
- 영구 발견 숨김 Recipe 수
- 대표 기물의 5성 도달 경로 수
- Meta save version

---
## 주요 변경 파일

- `Assets/ProjectEta/Scripts/Meta/MetaProgressSaveData.cs` 수정
- `Assets/ProjectEta/Scripts/Meta/MetaProgressState.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionDiscoveryLog.cs` 수정
- `Assets/ProjectEta/Scripts/Fusion/FusionRecipeIndex.cs` 추가
- `Assets/ProjectEta/Scripts/Fusion/FusionAtlasService.cs` 추가
- `Assets/ProjectEta/Scripts/Fusion/FusionTreeValidator.cs` 추가
- `Assets/ProjectEta/Tests/EditMode/Day89FusionAtlasTests.cs` 추가
- `Assets/ProjectEta/Editor/Day89FusionAtlasValidationMenu.cs` 추가

---
## 검증 상태

- GitHub main 89일차 커밋 기준 변경 파일 반영 확인
- MetaProgressSaveData v3 및 discoveredFusionRecipeIds 필드 확인
- MetaProgressState 영구 Recipe 발견 API 확인
- FusionRecipeIndex 결과·재료·5성 경로 검색 API 확인
- FusionTreeValidator 81종·70개·숨김 7개 기준 확인
- Day89 EditMode 회귀 테스트와 Editor 검증 메뉴 확인
- GitHub 커밋 상태 검사에서 별도 CI 상태 체크는 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

---
## 다음 개발 방향

90일차에는 데이터 중심 합성 확장 작업을 마치고 기물 고유 능력을 공통 방식으로 실행하기 위한 Ability 프레임워크를 구축한다.

Heal·ModifyDamage·ApplyStatus·Aura·Summon·ModifyMoveRange·BlockTile·DestroyObstacle·RedirectDamage를 공통 인터페이스와 실행 컨텍스트로 정의하고 이후 91~95일차 개별 능력 구현의 기반으로 사용한다.
