---
# 95일차 5성 능력 구현 계획

> 실행 방식: 현재 대화에서 직접 구현. 사용자가 앞서 설명한 95일차 방향의 적용을 지시함.
> 적용 지침: superpowers:executing-plans, test-driven-development, 마지막 독립 코드 검토.

**목표:** 등록된 5성 8종의 기획서 능력을 실제 플레이어 입력·AI·전투·저장에 연결.
**기준:** main 6c2fc8027fb356ef2bab7058d6edae5578de88b5.
**기획 근거:** [기물 탭](https://docs.google.com/document/d/1kjDKNNy9T0lNyxoRq7SGl3w7RSzAJpKSlgWFHQYO2gE/edit?tab=t.6m3kgqkyrl8k), 2026-10-08 확인.
**구조:** 공통 Aura·Heal·ApplyStatus 실행기 재사용. 조건부 피해·분담·다음 피격 보호는 전용 서비스. PieceRuntimeState에 전투 중 상태를 저장.

---
## 적용 조건

- 기존 HP·ATK, 이동 규칙, 81종 등록, 70개 합성식 유지.
- 동일 5성 보유·배치 상한 1개 유지.
- C# Allman 및 추가 코드 각 줄에 짧은 한국어 주석.
- 미정 수치는 임시값 표시: 대성기사 피해 2 감소/일반 턴당 수호자 1회, 기마장군 돌파 +2.
- 대현자 선택 범위는 거리 2, 피해 감소는 1로 개발용 임시 적용.
- 피해 감소 후 최소 피해 1. 철벽군주는 HP 1을 남길 수 있는 양까지만 분담.
- 환영장군은 배치 턴 보드 선택 후 형태 버튼 사용, 이동 시 기존 순환 유지.
- 공성대장 포격은 기존 스크린 이동 규칙과 원거리 처치 정책 유지.
- 황제 동일 공격력 오라 비중첩. 배치 턴마다 부상 인접 아군 중 가장 낮은 HP 1개 회복 1.
- 초기 황제 배치 후 첫 일반 턴에서 회복 1회 적용; 주기 배치에서는 진입 시 적용.

---
## 검토 초점

1. 다른 보드의 대상·사망 기물·아군 독·원거리 회복 차단.
2. 보호 중복 소비·턴 이벤트 중복 발행·저장 후 재충전 방지.
3. 라이더 단일 도약·막힌 반복 착지점은 돌파 제외.
4. AI의 독 대상 진영 및 능력 참조 보존.
5. 능력 대상 선택 취소·턴 변경·배치 시 이동 입력 차단.

---
## 1. 데이터와 전투 처리

- [x] 기존 코드로 Day95 전투 테스트 실행: 24개 중 20개 예상 실패, 4개 통과.
- [x] Abilities/FiveStarAbilityIds.cs, FiveStarCombatAbilityResolver.cs, FiveStarTurnAbilityResolver.cs 생성.
- [x] 11개 Ability 에셋을 Data/Abilities/FiveStar에 생성하고 8개 PieceDefinition에 연결.
- [x] CombatResolver.ResolveAttack 및 DamageResolver.ApplyDamage에 조건부 공격/보호 연결.
- [x] BattleHooks와 BattleController의 배치/턴 진입 연결.
- [x] 동일 이벤트 중복 회복·수호 리셋 및 라이더 이동 제한 테스트 통과 확인.

---
## 2. 선택 입력과 AI

- [x] FiveStarActiveAbilityService.TrySelectStartForm/ValidateSageChoice/EvaluateGuard 생성.
- [x] BoardInputController.TryBeginSelectedAbility/TryUseSelectedAbilityAt 입력 추가.
- [x] PieceAbilityOverlayUI에 능력 선택·취소·형태 버튼 추가.
- [x] 기존 EnemyAIAbilityCandidateBuilder 및 ScoreEvaluator에 독 후보 연결.
- [x] Preview가 전투 상태를 변경하지 않는지, 행동권 1회 소비 검증.

---
## 3. 저장과 검증

- [x] PieceRuntimeState의 보호 사용 여부/턴 번호/회복 발동 기록 저장.
- [x] RunSaveData와 RunState의 필드 추가는 구버전 기본값 호환 유지.
- [x] 저장 후 복원된 환영 형태 및 보호 상태 검증.
- [x] Day95 테스트, 전체 EditMode·PlayMode, Windows 개발 빌드 수행.
- [x] 실제 UI 검증 또는 수행 불가 범위를 명시하고 사용자 조작 절차 기록.
- [x] 독립 코드 검토 후 중요 결함 수정.
- [x] 사용자 커밋·푸시 요청 확인과 개발 일지·커밋 메시지 작성.

---
## 최종 검증 결과

- 전체 EditMode 770/770 통과, 95일차 신규 40개 포함.
- PlayMode 능력 버튼 통합 테스트 2/2 통과.
- 최종 Windows 64비트 개발 빌드 성공.
- 독립 코드 검토에서 발견한 정산 우회, 왕 HP 동기화, 전투 씬 능력 UI 설치 누락 수정.
- 실제 시각 배치와 전체 런 밸런스는 미검증. Docs/Day95Implementation.md에 확인 조작 기록.
