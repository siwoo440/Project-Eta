---
# 86일차 : 신규 1성 기물 영구 해금 및 획득 풀 연결

---
## 개발 목표

85일차에 고정한 81종 목표 로스터 중 신규 1성 6종을 실제 PieceDefinition으로 등록하고, 메타 토큰 영구 해금과 다음 런의 Reward·Shop·Event 카드 후보 풀에 연결했다.

기존 26종 시작 덱은 유지하면서 신규 기물은 해금 전에는 등장하지 않고, 해금 후 새 런의 RunContentUnlockSnapshot에 포함된 경우에만 일반 획득 후보로 들어가도록 구성했다.

---
## 작업 내용

- 창병·사수·방패병·깃발병·추격병·척후병 1성 PieceDefinition 6종 추가
- 신규 6종의 진영 방향 반전형 조건부 이동 규칙 추가
- PieceDatabase를 기존 26종에서 32종으로 확장
- 신규 6종 전용 영구 해금 ID와 30 Meta Token 비용 추가
- 기존 임시 기물 해금 슬롯을 실제 6종 해금 항목으로 교체
- 신규 6종 전용 PlayerUnlockablePiecePool86 리소스 추가
- Reward·Shop·Event 카드 후보에 해금된 신규 1성 기물을 병합하도록 수정
- 일반 카드 획득 후보를 1성 기물 기준으로 제한하도록 수정
- 기존 Day26·Day84·Day85 회귀 테스트를 32종·1성 18종 기준으로 수정
- Day86 신규 기물·해금·Snapshot·이동 규칙 회귀 테스트 추가
- CardRewardGenerator의 System.Random과 UnityEngine.Random 이름 충돌 수정

---
## 신규 1성 기물

| 기물 | PieceId | 영구 해금 ID | 비용 |
| --- | --- | --- | ---: |
| 창병 | `spearman` | `piece_spearman` | 30 |
| 사수 | `shooter` | `piece_shooter` | 30 |
| 방패병 | `shield_guard` | `piece_shield_guard` | 30 |
| 깃발병 | `flag_bearer` | `piece_flag_bearer` | 30 |
| 추격병 | `pursuer` | `piece_pursuer` | 30 |
| 척후병 | `scout` | `piece_scout` | 30 |

현재 PieceDatabase는 32종이며 등급별 등록 상태는 1성 18종, 2성 14종, 3~5성 0종이다.

81종 목표 로스터 대비 미등록 기물은 49종이다.

---
## 이동 규칙

신규 6종은 모두 PieceMovementType.Custom과 MovementConditionType 기반 조건부 이동을 사용한다.

- 창병은 상하좌우 1칸 이동과 같은 직선 방향 최대 2칸 공격을 사용
- 사수는 진영 기준 전방 대각선 두 방향으로 슬라이드 이동
- 방패병은 진영 기준 좌우와 후방으로 1칸 이동
- 깃발병은 진영 기준 전방과 좌우로 1칸 이동
- 추격병은 진영 기준 전방 1칸과 후방 대각선 1칸 이동
- 척후병은 진영 기준 전방 대각선과 좌우 1칸 이동

아군은 +Y, 적군은 -Y를 전방으로 사용해 같은 PieceDefinition을 양 진영에서 사용할 수 있도록 했다.

깃발병의 인접 아군 공격력 증가 효과는 공통 Aura 프레임워크 구현 시 연결하도록 현재는 보류했다.

---
## 영구 해금과 런 Snapshot

신규 기물은 각각 별도의 MetaUnlockType.Piece 항목을 사용하며 해금 비용은 30 Meta Token이다.

해금 상태는 진행 중인 런에 즉시 소급 적용하지 않는다.

기존 런의 RunContentUnlockSnapshot에 신규 기물 해금 ID가 없다면 MetaProgressState에서 해금한 뒤에도 해당 런에서는 후보에 등장하지 않는다.

다음 런 시작 시 새 Snapshot을 생성하면 해금된 기물이 Reward·Shop·Event 후보에 포함된다.

---
## 카드 획득 풀

기존 `PlayerStartingDeck26.asset`은 시작 덱 전용으로 유지했다.

신규 6종은 `PlayerUnlockablePiecePool86.asset`에 별도로 등록하고, `CardRewardGenerator`가 기존 카드 풀과 신규 해금 풀을 병합한 뒤 다음 조건을 적용한다.

- RunContentUnlockSnapshot 기준 영구 해금 여부 확인
- 일반 Reward 획득 규칙 확인
- 정상·사망 카드 보유 수 기준 중복 상한 확인
- 동일 PieceId 후보 중복 제거

Reward·Shop·Event가 같은 CardRewardGenerator를 사용하므로 세 획득 경로가 동일한 해금 규칙을 공유한다.

---
## 회귀 테스트

`Day86UnlockablePieceTests`에 다음 검증을 추가했다.

- PieceDatabase가 32종이고 1성이 18종인지 확인
- 신규 1성 6종의 ID·등급·분류·영구 해금 ID 확인
- 신규 기물 해금 항목이 6종이며 각각 30 Token인지 확인
- 미해금 상태에서 신규 6종이 카드 후보에 나오지 않는지 확인
- 진행 중 런에서 해금해도 기존 Snapshot에는 소급되지 않는지 확인
- 다음 런 Snapshot에서는 신규 6종이 카드 후보에 들어오는지 확인
- 일반 카드 획득 후보가 1성으로 제한되는지 확인
- 사수 이동 방향이 아군·적군에서 반대로 계산되는지 확인
- 창병이 1칸 이동과 직선 2칸 공격을 구분하는지 확인
- PlayerUnlockablePiecePool86에 신규 6종만 등록되는지 확인

기존 Day26·Day84·Day85 테스트의 현재 등록 수 기준도 32종·1성 18종·미등록 49종으로 갱신했다.

---
## 오류 수정

`CardRewardGenerator.cs`에서 `using System;`과 `using UnityEngine;`이 동시에 존재해 `Random` 형식이 모호해지는 CS0104 컴파일 오류를 확인했다.

`using Random = System.Random;` 별칭을 추가해 기존 재현 가능한 System.Random 기반 카드 후보 난수 로직을 유지하면서 이름 충돌을 제거했다.

---
## 주요 변경 파일

- `Assets/ProjectEta/Data/Spearman.asset` 등 신규 PieceDefinition 6종 추가
- `Assets/ProjectEta/Data/PieceDatabase.asset` 수정
- `Assets/ProjectEta/Resources/PlayerUnlockablePiecePool86.asset` 추가
- `Assets/ProjectEta/Scripts/Pieces/MovementConditionType.cs` 수정
- `Assets/ProjectEta/Scripts/Board/MovementRules/ConditionalMovementRule.cs` 수정
- `Assets/ProjectEta/Scripts/Meta/MetaUnlockDefinition.cs` 수정
- `Assets/ProjectEta/Scripts/Run/CardRewardGenerator.cs` 수정
- `Assets/ProjectEta/Scripts/Run/RunContentPoolRules.cs` 수정
- `Assets/ProjectEta/Tests/EditMode/Day86UnlockablePieceTests.cs` 추가

---
## 다음 개발 방향

87일차에는 81종 목표 로스터와 합성 성장 구조를 기준으로 1→2성 21개와 2→3성 20개 FusionRecipe 등록을 진행한다.

레시피 등록 과정에서는 재료 조합 중복, 결과 기물 미등록, 등급 상승 규칙 위반과 1성 시작 성장 경로 단절을 기존 진단·검증 체계로 함께 확인한다.
