# 71일차 : Shop 상품·가격 규칙 정식화 및 5페이즈 경제 흐름 연동

## 개발 목표

71일차에서는 기존에 동작하던 Shop 프로토타입을 5페이즈 런 구조에 맞는 정식 콘텐츠 형태로 확장했다.

기존 상점은 카드 구매·제거·회복·강화 기능 자체는 존재했지만, 상품 생성과 가격이 단순 고정 규칙에 가까웠고 제거·강화 대상 카드도 앞쪽 일부만 표시하는 제한이 있었다.

이번 일차에서는 다음 항목을 중심으로 구조를 정리했다.

- Shop 전용 상품 데이터 추가
- Phase·Stage·RouteMap Node를 반영한 상품 Seed 구성
- 진행도에 따른 Shop 카드 등급 가중치 적용
- 카드 등급·Phase·Stage 기반 구매 가격 계산
- 카드 제거·회복·강화 가격 규칙 분리
- 구매·제거·회복·강화 실행 로직을 ShopService로 분리
- 구매 완료 상품 재구매 차단
- 제거·강화 대상 카드 6장 제한 제거
- 제거·강화 목록 4장 단위 페이지 처리
- 현재 Phase·Gold·King HP를 Shop UI에 표시
- Day71 Shop 규칙 EditMode 테스트 추가

## ShopOffer 추가

상점 카드 한 개의 상태를 담당하는 `ShopOffer`를 추가했다.

각 상품은 다음 정보를 가진다.

- 판매 카드 `PieceDefinition`
- 구매 가격
- 구매 완료 여부

카드 구매가 성공하면 해당 `ShopOffer`를 구매 완료 상태로 바꿔 같은 상점 방문 중 동일 상품을 다시 구매하지 못하도록 했다.

## Shop 전용 상품 생성

기존 Shop은 카드 Reward 생성기를 직접 호출해 단순 후보를 가져오는 방식이었다.

71일차에서는 `ShopOfferGenerator`를 추가해 상점 상품 생성 책임을 분리했다.

상품 수는 현재 최대 3개이며 다음 정보를 Seed에 포함한다.

- RouteMap MapSeed
- 현재 Phase
- 현재 Stage
- 현재 RouteMap NodeId
- 현재 보유 카드 수

동일한 조건에서는 동일한 Seed를 생성하고, Phase 또는 Node가 달라지면 다른 Seed가 만들어지도록 구성했다.

이를 통해 각 Phase에서 Stage 번호가 다시 1부터 시작하더라도 서로 다른 Shop을 구분할 수 있게 했다.

## Shop 상품 등급 가중치

Shop 상품은 기존 `CardRewardGenerator`의 해금·중복·획득 가능 규칙을 재사용하되 Shop 전용 품질 프로필을 전달한다.

현재 Phase 진행도에 따른 1성·2성·3성 가중치는 다음과 같다.

| Phase | 1성 | 2성 | 3성 |
|---|---:|---:|---:|
| Phase 1 | 70 | 25 | 5 |
| Phase 2 | 55 | 35 | 10 |
| Phase 3 | 40 | 45 | 15 |
| Phase 4 | 30 | 45 | 25 |
| Phase 5 | 20 | 45 | 35 |

초반에는 1성 중심으로 상품이 구성되고, 후반 Phase로 진행할수록 2성·3성 비중이 높아진다.

실제 획득 가능 여부는 기존 카드 Reward 규칙을 그대로 사용하므로 King, Fusion 전용 카드, Monster, Boss, 일반 Reward에서 제외되는 고등급 카드 등은 기존 제한을 유지한다.

## 카드 구매 가격 규칙

`ShopPriceRules`를 추가해 기존 고정 구매 가격을 카드 등급과 진행도 기반으로 변경했다.

기본 가격은 다음과 같다.

| 등급 | 기본 가격 |
|---|---:|
| 1성 | 25 Gold |
| 2성 | 40 Gold |
| 3성 | 60 Gold |
| 4성 | 90 Gold |
| 5성 | 120 Gold |

여기에 현재 Phase와 Stage 진행에 따른 추가 가격을 더한다.

- Phase가 하나 증가할 때마다 구매 가격 가산
- Stage가 일정 구간 진행될 때마다 구매 가격 가산

4성·5성 가격은 가격 규칙에 예비값으로 존재하지만, 현재 일반 Shop 상품 생성은 기존 `CardRewardRules`를 사용하므로 일반 상품 풀에서는 기존 고등급 획득 제한을 유지한다.

## Shop 서비스 가격 정리

카드 구매 외 서비스 가격도 `ShopPriceRules`에서 계산한다.

현재 규칙은 다음 방향으로 구성했다.

- 카드 제거: 후반 Phase일수록 가격 증가
- King HP 회복: 후반 Phase일수록 가격 증가
- 카드 강화: Phase와 Stage 진행에 따라 가격 증가
- 회복량: King HP +1

따라서 모든 서비스가 하나의 고정 상수만 사용하는 구조에서 벗어나 현재 런 진행도에 맞춰 가격을 계산한다.

## ShopService 분리

구매·제거·회복·강화의 실제 상태 변경을 `ShopService`로 분리했다.

`StageActivityController`는 화면과 입력 흐름을 담당하고, 실제 경제·덱 변경은 ShopService가 처리한다.

### 카드 구매

카드 구매 시 다음 순서로 처리한다.

1. RunState·Gold·상품 상태 확인
2. 이미 구매한 상품인지 검사
3. 기존 CardRewardRules 획득 가능 여부 검사
4. Gold 지불
5. OwnedCardPool에 카드 추가
6. 카드 추가 실패 시 Gold 환불
7. 상품 구매 완료 처리
8. StageChoiceResult 생성

### 카드 제거

제거 가능한 카드인지 검사한 뒤 현재 Phase의 제거 가격만큼 Gold를 사용한다.

덱 제거에 실패하면 사용한 Gold를 다시 복구한다.

King, Monster, Boss는 Shop 관리 대상에서 제외한다.

### King HP 회복

현재 King HP가 최대 HP보다 낮을 때만 회복할 수 있다.

현재 Phase 기준 회복 가격을 지불한 뒤 King HP를 +1 적용하고 최대 HP를 넘지 않도록 제한한다.

현재 최대 HP 기준은 기존 `RunEconomyRules.PrototypeKingMaxHp`를 유지한다.

### 카드 강화

현재 Phase·Stage 기준 강화 가격을 지불한 뒤 기존 `RuntimeCardUpgradeService`를 사용한다.

강화는 기존 방식과 동일하게 선택 카드의 HP와 ATK를 각각 +1 한 런타임 복제로 교체한다.

강화 실패 시 Gold를 환불한다.

## 제거·강화 카드 목록 개선

기존 Shop은 제거·강화 페이지에서 조작 가능한 카드를 최대 6장까지만 가져왔다.

71일차에서는 이 제한을 제거하고 전체 관리 가능 카드를 조회하도록 변경했다.

한 화면에 모든 카드를 표시하지 않고 현재 4장 단위로 페이지를 나눈다.

목록이 4장을 초과하면 다음·이전 버튼으로 다른 페이지를 확인할 수 있다.

따라서 보유 카드 수가 증가하더라도 뒤쪽 카드가 제거·강화 대상에서 제외되지 않는다.

## Shop UI 상태 정보

기존 `StageBoardOverlayUI` 구조는 그대로 사용하면서 Shop 페이지에 현재 런 정보를 추가했다.

상점 메인에서는 다음 정보를 함께 표시한다.

- 현재 Phase / 전체 5 Phase
- 현재 Gold
- 현재 King HP / 최대 HP

구매 페이지에서는 카드별 실제 구매 가격을 표시한다.

제거·강화 페이지에서는 현재 페이지 번호와 전체 페이지 수를 표시한다.

## Event 흐름 유지

`StageActivityController`는 Shop과 Event를 함께 관리하고 있으므로 71일차 수정에서는 기존 Event 콘텐츠 흐름을 유지했다.

이번 일차의 변경 대상은 Shop 상품·가격·서비스 처리와 제거·강화 카드 페이지 구조에 집중했다.

## Day71 EditMode 테스트

`Day71ShopContentTests`를 추가했다.

현재 테스트는 다음 규칙을 검증한다.

- 동일 Shop 조건에서 같은 Seed 생성
- 서로 다른 Phase에서 다른 Shop Seed 생성
- 카드 등급이 높을수록 구매 가격 증가
- 후반 Phase에서 동일 등급 구매 가격 증가
- 카드 제거·회복·강화 가격이 후반 Phase에서 증가
- ShopOffer 구매 완료 상태 기록

현재 GitHub에는 Commit Status와 Workflow Run이 등록되어 있지 않으므로 Unity 컴파일과 전체 EditMode Test Runner의 실제 실행 통과 여부는 GitHub 기준으로 확인할 수 없다.

또한 현재 Day71 테스트는 가격·Seed·ShopOffer 상태 규칙 중심이며, ShopService의 실제 구매·제거·회복·강화 트랜잭션 전체를 직접 검증하는 테스트까지 포함하지는 않는다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Run/ShopOffer.cs`
- `Assets/ProjectEta/Scripts/Run/ShopOfferGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/ShopPriceRules.cs`
- `Assets/ProjectEta/Scripts/Run/ShopService.cs`
- `Assets/ProjectEta/Tests/EditMode/Day71ShopContentTests.cs`
- 신규 파일의 `.meta`
- `Devlogs/Day71/README.md`

### 수정

- `Assets/ProjectEta/Scripts/Run/StageActivityController.cs`

### 삭제

- 최종 커밋 기준 영구 삭제 파일 없음

## 결과

71일차에서는 기존 Shop 프로토타입을 5페이즈 런 진행과 연결되는 경제 콘텐츠 구조로 정리했다.

상품 생성은 현재 Phase·Stage·Node를 기준으로 분리되고, 진행도가 높아질수록 상점에서 2성·3성 카드의 비중이 증가한다.

카드 구매 가격 역시 카드 등급과 런 진행도에 따라 달라지고 카드 제거·회복·강화도 Phase 또는 Stage에 따라 비용이 증가한다.

상점의 실제 상태 변경은 `ShopService`로 분리해 UI와 경제 로직의 책임을 구분했다.

또한 제거·강화 대상의 기존 6장 제한을 없애고 4장 단위 페이지 구조를 적용해 카드 풀이 커져도 전체 보유 카드를 관리할 수 있게 했다.

## 검증 상태

GitHub `main` 최신 커밋은 `f9c25b7beee41210905ed3591a0aa6b1fc4aae7c`이며 현재 커밋 메시지는 `71`이다.

이전 Day70 커밋 `fed115756547efcfaece0703da6cb93fc51c1a9a`보다 1개 커밋 앞서 있으며, Day71 변경은 총 11개 경로로 구성되어 있다.

변경 내용은 Shop 관련 신규 런타임 클래스 4개와 각각의 `.meta`, `StageActivityController.cs` 수정, `Day71ShopContentTests.cs` 및 `.meta` 추가다.

최신 `main`에는 아직 `Devlogs/Day71/README.md`가 존재하지 않는다.

GitHub Commit Status와 Workflow Run은 현재 등록된 항목이 없다.

소스 연결 관계와 변경 범위를 확인한 기준에서는 개발 일지를 막을 정도의 명확한 문제는 확인되지 않았으나, Unity Editor 실제 컴파일과 전체 EditMode Test Runner 통과 여부는 별도 실행 결과로 확인해야 한다.
