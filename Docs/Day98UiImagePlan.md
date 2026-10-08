---
# 98일차 임시 UI 이미지 제작 계획

- 조사 기준 커밋: `0bd2f66` — 97일차 : 페이즈별 난이도 곡선과 전투 측정 완성
- 기준 해상도: 1920×1080
- 출력 형식: 투명 배경 PNG
- 표현 방식: 반듯한 벡터풍 다크 판타지 체스 UI
- 텍스트 처리: 이미지에 포함하지 않고 Unity Text 사용

---
## 현재 UI 조사 결과

97일차 조사 당시 전용 래스터 UI 자산은 경로 지도 노드 아이콘 6종이었다. 나머지 패널·버튼·카드·슬롯·알림은 대부분 코드에서 단색 Image와 Outline으로 생성된다.

| 화면 | 현재 주요 UI와 버튼 | 이미지 적용 대상 |
|---|---|---|
| 메인 메뉴 | 새 게임, 이어하기, 영구 성장, 설정, 게임 종료, 확인·취소 모달 | 메뉴 배경, 메뉴 패널, 공통 버튼, 모달 프레임 |
| King 선택 | 좌우 이동, King 선택, 잠금 안내, 패시브 설명 | King 카드 프레임, 엠블럼 영역, 잠금 오버레이 |
| 튜토리얼 | 이전, 건너뛰기, 다음·시작 | 튜토리얼 패널, 페이지 장식, 공통 버튼 |
| 전투 HUD | 스테이지·Gold·턴, 배치 안내, 보스 HP, 행동 안내 | 상단 상태 프레임, 보스 바, 우측 정보 패널 |
| 손패·카드 | 카드 프레임, 등급, ATK·HP, 선택 강조 | 카드 프레임, 능력치 배지, 선택 효과 |
| 덱 패널 | 뽑을 카드, 죽은 카드, 목록, 닫기 | 카드 더미 버튼, 목록 패널, 닫기 아이콘 |
| 합성 | 합성 열기, 재료 슬롯, 결과 슬롯, 합성·취소 | 합성 버튼, 슬롯 프레임, 결과 강조 |
| 경로 지도 | Phase·Stage, 노드 상세, 노드 범례, Seed | 우측 지도 패널, 툴팁, 선택·잠금 고리 |
| 카드 보상 | 카드 3개, 선택 상세, 선택 확정 | 선택 카드 프레임, 상세 패널, 확정 버튼 |
| 상점·이벤트 | 선택 카드, 가격·설명, Hover 설명 | 선택 카드 프레임, 가격표, 품절·완료 표시 |
| 기물 정보 | 초상화, 등급, ATK·HP, 역할, 상태 효과 | 정보 패널, 능력 태그, 상태 효과 배지 |
| 전투 로그 | 접기·펼치기, 로그 목록 | 로그 탭, 로그 패널 |
| 알림 | 전투 시작·승리·패배, 시스템 Toast | 중앙 배너, 승리·패배 표식, Toast 프레임 |
| 일시정지 | 계속하기, 조작법, 설정, 메인 메뉴 | Pause 패널, 공통 버튼 |
| 설정 | 카테고리, 이전·다음, 슬라이더, 초기화·취소·적용 | 탭, 화살표, 슬라이더, 손잡이 |
| 영구 성장 | 카테고리, 해금 항목, 해금·취소·확정 | 해금 항목 프레임, 토큰 배지, 잠금 표시 |
| 런 결과 | 런 클리어·종료, 영구 성장 보기, 메인 메뉴 | 결과 패널, 승리·패배 장식 |
| Steam 상태 | 연결 상태, Overlay 열기 | 작은 상태 배지와 공통 버튼 |
| 개발 전용 | F1 디버그 패널, 임시 계속 버튼 | 전용 이미지 제외, 공통 패널 재사용 |

---
## 기존 유지 이미지

| 파일 | 용도 |
|---|---|
| `RouteBattleIcon.png` | 일반 전투 노드 |
| `RouteEliteIcon.png` | 정예 전투 노드 |
| `RouteRewardIcon.png` | 보상 노드 |
| `RouteShopIcon.png` | 상점 노드 |
| `RouteEventIcon.png` | 이벤트 노드 |
| `RouteBossIcon.png` | 보스 노드 |

모든 노드 아이콘은 256×256이며 현재 디자인을 유지한다. 선택·잠금·이동 가능 상태는 아이콘을 다시 만들지 않고 별도 고리 이미지로 표현한다.

---
## 1차 제작 이미지

핵심 플레이 화면을 먼저 통일하기 위한 필수 이미지다.

| 파일명 제안 | 크기 | 용도 |
|---|---:|---|
| `UiPanelDark.png` | 1024×1024 | 대부분의 패널에 사용하는 9-Slice 프레임 |
| `UiPanelGold.png` | 1024×1024 | 중요 정보와 선택 결과용 9-Slice 프레임 |
| `UiHeaderPlaque.png` | 1024×256 | 화면 제목과 패널 제목 배경 |
| `UiButtonBase.png` | 512×160 | 일반·확정 버튼 공통 바탕 |
| `UiButtonDanger.png` | 512×160 | 종료·취소·위험 선택 버튼 |
| `UiDivider.png` | 1024×64 | 패널 내부 황동 구분선 |
| `UiSelectionGlow.png` | 512×512 | 카드·항목 선택 강조 |
| `UiLockedOverlay.png` | 512×512 | 잠긴 카드·노드·King 표시 |
| `BattleBossBarFrame.png` | 1536×192 | 상단 보스 체력 바 프레임 |
| `BattlePhaseBadge.png` | 256×256 | 보스 단계와 위험 상태 표시 |
| `BattleStatusFrame.png` | 768×512 | 좌측 스테이지·Gold·턴 정보 |
| `BattleSidePanel.png` | 640×960 | 우측 전투 규칙·행동 안내 |
| `BattleHandTray.png` | 1536×384 | 하단 손패 받침대 |
| `BattlePileButton.png` | 384×192 | 뽑을 카드·죽은 카드 공용 버튼 |
| `BattleCardFrame.png` | 512×768 | 손패·보상·덱 목록 카드 공용 프레임 |
| `BattleLogTab.png` | 512×128 | 전투 로그 접기·펼치기 탭 |
| `RouteSidePanel.png` | 640×1080 | 지도 우측 정보·범례 패널 |
| `RouteNodeTooltip.png` | 640×384 | 노드 Hover 상세 정보 |
| `RouteNodeHighlightRing.png` | 512×512 | 이동 가능한 노드의 금빛 강조 |
| `RouteNodeLockedRing.png` | 512×512 | 잠긴 노드의 회색 고리 |

1차 제작 수량은 20개다. 패널과 버튼은 Unity 9-Slice와 Color Tint를 사용해 크기·Hover·Pressed·Disabled 상태를 확장한다.

---
## 2차 제작 이미지

보상·상점·이벤트·합성 화면을 통일하는 이미지다.

| 파일명 제안 | 크기 | 용도 |
|---|---:|---|
| `ChoiceCardFrame.png` | 640×896 | 보상·상점·이벤트 선택 카드 |
| `ChoiceCardSelected.png` | 640×896 | 현재 선택 카드 강조 |
| `ShopPriceTag.png` | 384×160 | Gold 가격 표시 |
| `ShopSoldOutOverlay.png` | 640×896 | 구매 완료·품절 표시 |
| `FusionMaterialSlot.png` | 512×512 | 합성 재료 슬롯 |
| `FusionResultSlot.png` | 512×512 | 합성 결과 슬롯 |
| `PieceInfoFrame.png` | 640×896 | 기물 상세 정보 패널 |
| `AbilityTag.png` | 512×128 | 능력·역할·상태 효과 태그 |
| `SystemToastFrame.png` | 768×256 | 우측 상단 시스템 알림 |
| `BattleAnnouncementFrame.png` | 1536×320 | 전투 시작·턴·보스 경고 배너 |
| `VictoryDefeatPlaque.png` | 1024×512 | 승리·패배 결과 공용 장식 |

2차 제작 수량은 11개다.

---
## 3차 제작 이미지

메뉴·설정·성장 화면을 통일하는 이미지다.

| 파일명 제안 | 크기 | 용도 |
|---|---:|---|
| `MainMenuBackdrop.png` | 1920×1080 | 메인 메뉴 전용 배경 |
| `MainMenuEmblem.png` | 768×768 | PROJECT η 상징 장식 |
| `KingCardFrame.png` | 768×1024 | King 선택 카드 |
| `KingEmblemFrame.png` | 512×512 | King 초상화·문양 영역 |
| `TutorialPageFrame.png` | 1280×896 | 튜토리얼 페이지 패널 |
| `UiModalFrame.png` | 1024×640 | 확인·취소 모달 |
| `UiCategoryTab.png` | 512×192 | 설정·영구 성장 카테고리 탭 |
| `UiSliderTrack.png` | 768×96 | 음량·UI Scale 슬라이더 |
| `UiSliderHandle.png` | 192×192 | 슬라이더 손잡이 |
| `MetaUnlockTile.png` | 640×256 | 영구 성장 해금 항목 |
| `RunResultFrame.png` | 1024×768 | 런 종료·클리어 결과 패널 |
| `UiCloseIcon.png` | 256×256 | 패널 닫기 아이콘 |
| `UiArrowLeft.png` | 256×256 | 이전·왼쪽 이동 |
| `UiArrowRight.png` | 256×256 | 다음·오른쪽 이동 |

3차 제작 수량은 14개다.

---
## 전체 제작 규모

| 구분 | 수량 |
|---|---:|
| 전체 화면 디자인 참고 시안 | 2개 |
| 1차 핵심 플레이 이미지 | 20개 |
| 2차 선택·정보 이미지 | 11개 |
| 3차 메뉴·설정 이미지 | 14개 |
| 프로젝트용 투명 이미지 합계 | 45개 |

전체 화면 참고 시안은 전투 화면과 경로 지도 화면으로 제작한다. 실제 프로젝트에는 참고 시안 자체를 사용하지 않고, 시안에서 분리한 투명 UI 요소만 적용한다.

---
## 제작 규칙

- 모든 프레임과 버튼은 글자·숫자·아이콘 문구 없이 제작
- 완전 투명한 바깥 배경 유지
- 테두리 두께와 모서리 장식의 좌우 대칭 유지
- 작은 크기에서도 형태가 유지되는 단순한 면 구성
- 검은 석재·짙은 청록·황동색을 기본 팔레트로 사용
- 위험 상태는 와인색, 확정 상태는 금색, 정보 상태는 청록색 사용
- 버튼 상태는 별도 그림을 늘리지 않고 Unity Color Tint 사용
- 패널·버튼·카드 프레임은 Unity 9-Slice 여백을 확보
- 기존 경로 노드 아이콘과 시각적 두께를 맞춤
- 한 이미지 안에 여러 자산을 모은 Sprite Sheet 방식 제외

---
## 압축 진행 현황

이미지는 각각 독립된 투명 PNG로 생성한다. 단계는 생성·Unity Import·코드 적용·회귀 검증을 함께 처리하는 작업 묶음만 의미한다.

| 압축 단계 | 범위 | 개별 이미지 수 | 상태 |
|---:|---|---:|---|
| 1 | 공통 패널 기반 | 2 | `UiPanelDark`·`UiPanelGold` 적용 완료 |
| 2 | 공통 조작 요소·전투 HUD·경로 지도 | 18 | 생성·코드 적용·자동 검증 완료 |
| 3 | 보상·상점·이벤트·합성·정보·알림 | 11 | 생성·코드 적용·자동 검증 완료 |
| 4 | 메인 메뉴·King·튜토리얼·설정·영구 성장·결과 | 14 | 생성·코드 적용·자동 검증 완료 |
---
## 개별 자산 적용 현황

| 순서 | 자산 | 적용 위치 | 상태 |
|---:|---|---|---|
| 1 | `UiPanelDark.png` | 전투 화면 좌측 상태 패널 | 생성·비례형 9-Slice 적용 완료 |
| 2 | `UiPanelGold.png` | 카드 보상 선택 상세 패널 | 생성·비례형 9-Slice 적용 완료 |
| 3 | `UiHeaderPlaque.png` | 카드 보상 화면 제목 명패 | 생성·적용 완료 |
| 4 | `UiButtonBase.png` | 카드 보상 선택 확정 버튼 | 생성·적용 완료 |
| 5 | `UiButtonDanger.png` | 죽은 카드 더미 위험 버튼 | 생성·적용 완료 |
| 6 | `UiDivider.png` | 지도 정보 패널 구분선 | 생성·적용 완료 |
| 7 | `UiSelectionGlow.png` | 합성 재료 선택 카드 강조 | 생성·적용 완료 |
| 8 | `UiLockedOverlay.png` | 사용할 수 없는 손패 카드 | 생성·적용 완료 |
| 9 | `BattleBossBarFrame.png` | 화면 상단 보스 체력바 | 생성·적용 완료 |
| 10 | `BattlePhaseBadge.png` | 보스 Phase 2 상태 줄 | 생성·적용 완료 |
| 11 | `BattleStatusFrame.png` | 상단 전투 상태 HUD | 생성·적용 완료 |
| 12 | `BattleSidePanel.png` | 좌측 전투 행동 안내 | 생성·적용 완료 |
| 13 | `BattleHandTray.png` | 화면 하단 손패 받침 | 생성·적용 완료 |
| 14 | `BattlePileButton.png` | 뽑을 카드 더미 버튼 | 생성·적용 완료 |
| 15 | `BattleCardFrame.png` | 손패 카드 외곽선 | 생성·적용 완료 |
| 16 | `BattleLogTab.png` | 전투 로그 접기·펼치기 탭 | 생성·적용 완료 |
| 17 | `RouteSidePanel.png` | 지도 우측 정보·범례 패널 | 생성·적용 완료 |
| 18 | `RouteNodeTooltip.png` | 지도 노드 상세 설명 | 생성·적용 완료 |
| 19 | `RouteNodeHighlightRing.png` | 다음 이동 가능 노드 금빛 고리 | 생성·적용 완료 |
| 20 | `RouteNodeLockedRing.png` | 선택 불가 노드 회색 고리 | 생성·적용 완료 |
| 21 | `ChoiceCardFrame.png` | 보상·상점·이벤트 선택 카드 | 생성·적용 완료 |
| 22 | `ChoiceCardSelected.png` | 선택 카드 강조 | 생성·적용 완료 |
| 23 | `ShopPriceTag.png` | 상점 Gold 가격 | 생성·적용 완료 |
| 24 | `ShopSoldOutOverlay.png` | 구매 완료 상품 | 생성·적용 완료 |
| 25 | `FusionMaterialSlot.png` | 합성 재료 슬롯 | 생성·적용 완료 |
| 26 | `FusionResultSlot.png` | 합성 결과 슬롯 | 생성·적용 완료 |
| 27 | `PieceInfoFrame.png` | 기물 상세 정보 | 생성·적용 완료 |
| 28 | `AbilityTag.png` | 역할·능력 태그 | 생성·적용 완료 |
| 29 | `SystemToastFrame.png` | 시스템 알림 | 생성·적용 완료 |
| 30 | `BattleAnnouncementFrame.png` | 전투 시작·턴 알림 | 생성·적용 완료 |
| 31 | `VictoryDefeatPlaque.png` | 승리·패배 알림 | 생성·적용 완료 |
| 32 | `MainMenuBackdrop.png` | 메인 메뉴 배경 | 생성·적용 완료 |
| 33 | `MainMenuEmblem.png` | 메인 메뉴 체스 문양 | 생성·적용 완료 |
| 34 | `KingCardFrame.png` | King 선택 카드 | 생성·적용 완료 |
| 35 | `KingEmblemFrame.png` | King 초상화 프레임 | 생성·적용 완료 |
| 36 | `TutorialPageFrame.png` | 튜토리얼 페이지 | 생성·적용 완료 |
| 37 | `UiModalFrame.png` | 확인·취소 모달 | 생성·적용 완료 |
| 38 | `UiCategoryTab.png` | 설정·영구 성장 탭 | 생성·적용 완료 |
| 39 | `UiSliderTrack.png` | 설정 슬라이더 트랙 | 생성·적용 완료 |
| 40 | `UiSliderHandle.png` | 설정 슬라이더 손잡이 | 생성·적용 완료 |
| 41 | `MetaUnlockTile.png` | 영구 성장 해금 항목 | 생성·적용 완료 |
| 42 | `RunResultFrame.png` | 런 결과 패널 | 생성·적용 완료 |
| 43 | `UiCloseIcon.png` | 결과 패널 닫기 장식 | 생성·적용 완료 |
| 44 | `UiArrowLeft.png` | King·설정 이전 버튼 | 생성·적용 완료 |
| 45 | `UiArrowRight.png` | King·설정 다음 버튼 | 생성·적용 완료 |
---
## 압축 작업 순서

---
### 압축 1단계 — 공통 패널 기반

- `UiPanelDark.png`
- `UiPanelGold.png`
- 공통 비례형 9-Slice 로더
- 전투 상태 패널과 카드 보상 상세 패널 적용
- 상태: 완료

---
### 압축 2단계 — 핵심 플레이 UI

- 공통 요소 6개: 제목 팻말, 일반 버튼, 위험 버튼, 구분선, 선택 강조, 잠금 오버레이
- 전투 HUD 8개: 보스 바, 단계 배지, 상태 프레임, 우측 패널, 손패 받침대, 더미 버튼, 카드 프레임, 로그 탭
- 경로 지도 4개: 우측 패널, 노드 툴팁, 선택 고리, 잠금 고리
- 경로 지도 전체 참고 시안 1개
- 개별 PNG 생성 후 전투·지도 화면 일괄 적용
- EditMode 839개·PlayMode 2개·Windows 개발 빌드 검증 완료
- 1920×1080·2560×1440 실제 화면 시각 확인 대기
- 상태: 코드 적용 완료

---
### 압축 3단계 — 런 진행 UI

- 보상·상점·이벤트 선택 카드
- 가격표와 품절 오버레이
- 합성 재료·결과 슬롯
- 기물 정보와 능력 태그
- 시스템 Toast와 전투 알림
- 승리·패배 결과 팻말
- 개별 PNG 11개 생성·런 진행 화면 적용
- 선택 카드 변경·구매 불가/완료 구분·알림 자산 전환 검증
- 테두리 표시 배율과 합성 패널 내부 배치 보정
- 1920×1080·2560×1440 샘플 UI 렌더 확인
- EditMode 848개·PlayMode 2개·Windows 개발 빌드 검증 완료
- 상태: 코드 적용 완료

---
### 압축 4단계 — 메뉴·시스템 UI

- 메인 메뉴 배경과 문양
- King 카드와 문양 프레임
- 튜토리얼 페이지
- 모달·카테고리 탭
- 슬라이더 트랙·손잡이
- 영구 성장 항목
- 런 결과 패널
- 닫기·좌우 화살표
- 개별 PNG 14개 생성·메뉴 화면 적용 완료
- 설정 값·화살표·상태 문구와 조작법 제목 배치 보정
- 영구 성장 목록에 스크롤·마스크 적용, 최초 항목과 마지막 항목 접근 검증
- 13개 샘플 화면을 1920×1080·2560×1440으로 렌더하여 총 26장 확인
- EditMode 859개·PlayMode 3개·Windows 개발 빌드 검증 완료
- 상태: 코드 적용 완료, 실제 게임 전체 흐름의 시각 검수 대기

각 이미지는 별도 파일과 별도 Unity 메타 파일을 유지한다. Sprite Sheet는 사용하지 않는다.
---
## 일정 변경

- 98일차: 임시 UI 디자인 시안·공통 이미지·전투·지도·런 진행·메뉴·설정·성장 UI 적용
- 99일차: 기존 98일차 계획이었던 스테이지 전용 규칙·보상 차이·콘텐츠 잠금 연결

---
## 98일차 마무리 확인

- 2026-10-09 전체 45종의 PNG·메타·UI 코드 연결 확인
- 기존 이미지 20종의 투명 처리·Clamp·MipMap Import 설정 보정
- 실제 프로젝트 EditMode 859개·PlayMode 3개·Windows 개발 빌드 재검증
- 개발일지: Devlogs/Day98/README.md
- 자산별 연결 기록: Docs/Validation/Day98UiAssetAudit.md
