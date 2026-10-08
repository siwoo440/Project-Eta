---
# 98일차 UI 3단계 적용 기록

투명 PNG 11개를 개별 생성하고 런 진행 UI에 연결했다. 글자·가격·능력치는 기존 Unity Text를 유지한다.

---
## 적용 이미지

저장 경로: `Assets/ProjectEta/Resources/UI/Day98/`

| 파일 | 원본 크기 | 적용 위치 |
|---|---:|---|
| `ChoiceCardFrame.png` | 1024×1536 | 보상·상점·이벤트 프레임 |
| `ChoiceCardSelected.png` | 1024×1536 | 선택·마우스 강조 |
| `ShopPriceTag.png` | 1774×887 | 실제 상품 가격 표시 |
| `ShopSoldOutOverlay.png` | 1024×1536 | 구매 완료 상품 표시 |
| `FusionMaterialSlot.png` | 1254×1254 | 합성 재료 슬롯 |
| `FusionResultSlot.png` | 1254×1254 | 합성 결과 슬롯 |
| `PieceInfoFrame.png` | 1024×1536 | 기물 정보 패널 |
| `AbilityTag.png` | 2172×724 | 역할·능력 태그 |
| `SystemToastFrame.png` | 2135×736 | 시스템 알림 |
| `BattleAnnouncementFrame.png` | 2172×724 | 전투 시작·턴 알림 |
| `VictoryDefeatPlaque.png` | 2172×724 | 승리·패배·런 결과 알림 |

---
## 동작과 배치

- 선택 카드에만 금색 고리 표시, 강조 이미지의 클릭 간섭 차단
- Gold 부족과 구매 완료를 실제 상품 상태로 구분, 가격표 문구 표시
- 상점 선택지를 이벤트에 재사용할 때 가격표와 완료 이미지 제거
- 합성 재료·결과 프레임 적용, 제목·발견 알림·상태 문구·결과 설명의 패널 내부 배치 수정
- 승리·패배·런 완료·실패는 결과 팻말, 일반 전투 알림은 전투 프레임 적용
- Resources Sprite 캐시 추가, 알림 반복 표시 시 Sprite 재생성 방지
- 고해상도 원본의 테두리가 UI 글자를 덮지 않도록 표시 배율 보정
- 가로 명패의 투명 여백은 Unity Sprite 사용 영역으로 제외, PNG 원본 유지
- 새 이미지 11개에 투명 테두리 보정·Clamp·MipMap 비활성 적용

---
## 확인 범위

- EditMode: 848/848 통과
- PlayMode: 2/2 통과
- 보상·상점·합성·정보·시스템 알림·전투 알림·승리 팻말의 1920×1080·2560×1440 샘플 렌더 출력
- 샘플 렌더는 실제 UI 생성 코드에 테스트 데이터를 연결한 결과이며 전체 게임 플레이 화면은 별도 확인 대상
- Windows 개발 빌드: 성공

생성 도구와 개별 프롬프트: [Day98Stage3ImagePrompts.md](Day98Stage3ImagePrompts.md)

---
## 다음 작업

압축 4단계의 메뉴·King 선택·튜토리얼·설정·영구 성장·런 결과 이미지 14개 적용을 완료했다. 다음 작업은 99일차 스테이지 전용 규칙·보상 차이·콘텐츠 잠금 연결이다.
