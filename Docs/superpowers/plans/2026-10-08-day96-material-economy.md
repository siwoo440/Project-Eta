---
# 96일차 재료 공급·경제 측정 구현 계획

> 실행 방식: 사용자 승인한 96일차 방향을 현재 대화에서 직접 적용. executing-plans 및 TDD 적용, 마지막 독립 코드 검토. 이번 요청에서는 커밋·푸시하지 않음.

**목표:** 81종·70개 합성식을 기준으로 재료 공급과 경제를 재현 가능하게 측정하고, 실제 런에서 등급 도달 시점·Gold 수입과 지출을 저장·내보내기.
**기준:** main `8ed0be475d962bea868f10f27107bec971b6b5d6`.
**근거:** 직전 대화에서 승인한 96일차 구현 방향과 현재 보상·상점·이벤트 실행 경로.
**구조:** 기존 획득 규칙 유지, Resources의 RunBalanceProfile로 재료별 가중치·가격·승리 Gold 설정. RunState의 직렬화 가능한 측정 데이터에 실제 성공과 시도를 기록. 보고서는 실제 런과 후보 샘플링을 구분.
**기술:** Unity 6000.3.21f1, C#, 기존 EditMode·PlayMode TestRunner.

---
## 적용 조건과 결정

- 기존 81종·70개 레시피와 등급별 보유 제한 유지.
- 일반 Reward·Shop·Event 직접 카드 획득은 왕 제외 1성만 허용. 합성 결과 직접 지급 차단.
- 해금 상태, 사망 카드의 보유 상한, 동일 Seed 결정성 유지.
- 초기 재료별 가중치 100, 기존 가격 유지. 실측 근거 없는 재료 우대나 가격 변경 제외.
- 현재 전투 Gold 지급 누락 보완: 일반 15·정예 25·중간 보스 40·최종 보스 60, 페이즈당 +5의 개발용 임시값.
- 승리 보상은 페이즈·깊이별 1회. 지급 이력은 저장 복원 후에도 유지.
- 구버전 저장에는 빈 측정 데이터 적용. 복원·환불을 새 획득·수입으로 오인하지 않음.
- C# Allman, 모든 신규 줄에 짧은 한글 명사형 주석.
- 실제 완주 시간·난이도와 모의 후보 샘플링 결과를 구분. 전체 런 실측이 없으면 수치 확정 금지.
- 사용 중인 원본 Unity 에디터를 유지하고 검증 복사본에서 테스트·빌드.

---
## 검토 초점

1. 저장 직후 다시 지급되는 승리 보상과 다음 페이즈로 잘못 귀속되는 기록.
2. UI 재표시·이어하기로 후보와 카드 획득이 중복 기록되는 경우.
3. 실패 구매·환불·복원을 정상 수입이나 카드 획득으로 세는 경우.
4. 해금되지 않은 기물, 왕·합성 결과, 사망 카드 보유 상한의 우회.
5. 음수·과도한 가중치·가격, 손상·누락된 기록 목록, 기록 상한 이후 누락.

---
## 1. 재료 가중치와 가격 설정

생성: Scripts/Run/RunBalanceProfile.cs, Resources/RunBalance96.asset.
수정: CardRewardGenerator.cs, ShopPriceRules.cs.
인터페이스: CardRewardGenerator.GenerateBalanced(sourcePool, owned, dead, count, seed, snapshot, rewardProfile, balanceProfile).

- [x] 실제 후보 생성에서 재료 가중치가 분포를 변경하는 실패 테스트 작성·실행.
- [x] 왕·합성 결과·미해금·보유 상한 차단과 Seed 재현성 검증.
- [x] RunBalanceProfile의 재료 ID별 가중치 및 기존 가격 기본값 구현.
- [x] 가중치 0·음수·미등록 ID 안전 기본값과 가격 범위 보정 검증.

---
## 2. 실제 런 기록과 승리 Gold

생성: RunBalanceData.cs, RunBalanceTelemetry.cs, RunBattleGoldRewardService.cs.
수정: RunState.cs, RunSaveData.cs, RunEconomyState.cs, RunStageFlowService.cs, ShopService.cs, StageEventService.cs, CardRewardController.cs, StageActivityController.cs, BoardInputController.cs.
인터페이스: RunState.BalanceData, RunBalanceTelemetry.RecordOffers/RecordCard/RecordFusion/RecordGold, RunBattleGoldRewardService.TryGrant.

- [x] 기존 CompleteBattle 승리 수입 누락, 실패·중복·저장 후 지급 차단 실패 테스트 실행.
- [x] 성공 카드 획득, 실패 지출, 환불, 실제 합성 시점 기록 테스트 작성·실행.
- [x] 경제·획득·합성의 실제 확정 지점에 기록 연결.
- [x] 저장 데이터 복사·복원과 구버전 호환, 초기 Gold와 복원 Gold 구분 검증.
- [x] 기록 2048개 상한과 첫 등급 도달 기록·보상 지급 이력 보존 검증.

---
## 3. 보고서와 사용자 확인

생성: RunBalanceReport.cs, Editor/Day96BalanceValidationMenu.cs, Tests/EditMode/Day96BalanceTests.cs.
수정: ProjectEtaDebugWindow.cs.
인터페이스: RunBalanceReport.BuildSummary/Export, 에디터 재현 샘플 보고서 메뉴와 F1 기록 내보내기.

- [x] 실제 런 요약·JSON/CSV 내보내기의 환불·실패 지출·최초 합성 판정 테스트.
- [x] Reward·Shop·Event 후보 샘플링과 레시피 재료 사용량 보고서 생성.
- [x] 현재 기준 보고서를 저장하고 관측에 따라 1차 조정 또는 유지 근거 기록.
- [x] 전체 EditMode·PlayMode·Windows 개발 빌드 수행.
- [x] 독립 코드 검토 및 중요 결함 수정.
- [x] Docs/Day96Implementation.md에 생성·수정·제거 요소와 정확한 UI 확인 절차 기록.

---
## 진행 기록

- 사전 확인: 일반 획득은 1성으로 제한하지만 가중치는 등급별 값만 사용.
- 사전 확인: 전투 승리 Gold 지급 경로 없음, 이벤트 외 반복 수입 없음.

- 최종 검증: EditMode 800/800, PlayMode 2/2, Windows 개발 빌드 성공.
- 독립 검토 발견: 상세 상한 이후 총계 누락 수정, 이전 측정 저장의 빈 총계 객체 호환 수정 및 회귀 테스트 통과.
- 실제 전체 런 실측과 재료 가중치·가격 확정은 플레이 기록 확보 이후 진행.
