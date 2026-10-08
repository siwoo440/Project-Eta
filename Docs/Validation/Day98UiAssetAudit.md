---
# 98일차 UI 이미지 연결 검증

- 확인일: 2026-10-09
- 대상 프로젝트: F:\Project-Eta
- 개별 PNG 45개·메타 45개·UI 연결 45종 확인
- 공통 패널은 TryApplyDarkPanel·TryApplyGoldPanel 경유 참조 포함
- 이미지 외곽 모서리 알파: 42개 모두 0, 나머지 3개 일부 모서리 1/255
- 모서리 검사만으로 이미지 전체 바깥 영역의 알파를 보장하지 않음

---
## 자산별 연결 위치

| PNG 이름 | 참조 줄 수 | UI 코드 |
|---|---:|---|
| UiPanelDark.png | 9 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs, Assets/ProjectEta/Scripts/King/StrategyKingSelectionUI.cs, Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs, Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs, Assets/ProjectEta/Scripts/UI/MainMenuController.cs, Assets/ProjectEta/Scripts/UI/RunResultMainMenuController.cs, Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiPanelGold.png | 1 | Assets/ProjectEta/Scripts/UI/CardRewardUI.cs |
| UiHeaderPlaque.png | 1 | Assets/ProjectEta/Scripts/UI/CardRewardUI.cs |
| UiButtonBase.png | 10 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs, Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs, Assets/ProjectEta/Scripts/UI/CardRewardUI.cs, Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs, Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs, Assets/ProjectEta/Scripts/UI/MainMenuController.cs, Assets/ProjectEta/Scripts/UI/RunResultMainMenuController.cs, Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiButtonDanger.png | 6 | Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/UI/DeckPanelUI.cs, Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs, Assets/ProjectEta/Scripts/UI/MainMenuController.cs, Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiDivider.png | 1 | Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs |
| UiSelectionGlow.png | 1 | Assets/ProjectEta/Scripts/UI/CardView.cs |
| UiLockedOverlay.png | 1 | Assets/ProjectEta/Scripts/UI/CardView.cs |
| BattleBossBarFrame.png | 1 | Assets/ProjectEta/Scripts/Boss/BossHealthUI.cs |
| BattlePhaseBadge.png | 1 | Assets/ProjectEta/Scripts/Boss/BossPhaseStatusUI.cs |
| BattleStatusFrame.png | 1 | Assets/ProjectEta/Scripts/UI/BattleHUD.cs |
| BattleSidePanel.png | 1 | Assets/ProjectEta/Scripts/UI/BattleInteractionStatusUI.cs |
| BattleHandTray.png | 1 | Assets/ProjectEta/Scripts/UI/HandUI.cs |
| BattlePileButton.png | 1 | Assets/ProjectEta/Scripts/UI/DeckPanelUI.cs |
| BattleCardFrame.png | 1 | Assets/ProjectEta/Scripts/UI/CardView.cs |
| BattleLogTab.png | 1 | Assets/ProjectEta/Scripts/UI/CombatLogUI.cs |
| RouteSidePanel.png | 1 | Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs |
| RouteNodeTooltip.png | 1 | Assets/ProjectEta/Scripts/UI/Day66RouteMapUI.cs |
| RouteNodeHighlightRing.png | 1 | Assets/ProjectEta/Scripts/Board/RouteNodeSelectionHighlight.cs |
| RouteNodeLockedRing.png | 1 | Assets/ProjectEta/Scripts/Board/RouteNodeSelectionHighlight.cs |
| ChoiceCardFrame.png | 2 | Assets/ProjectEta/Scripts/UI/CardRewardUI.cs, Assets/ProjectEta/Scripts/UI/StageBoardOverlayUI.cs |
| ChoiceCardSelected.png | 2 | Assets/ProjectEta/Scripts/UI/CardRewardUI.cs, Assets/ProjectEta/Scripts/UI/StageBoardOverlayUI.cs |
| ShopPriceTag.png | 1 | Assets/ProjectEta/Scripts/UI/StageBoardOverlayUI.cs |
| ShopSoldOutOverlay.png | 1 | Assets/ProjectEta/Scripts/UI/StageBoardOverlayUI.cs |
| FusionMaterialSlot.png | 1 | Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs |
| FusionResultSlot.png | 1 | Assets/ProjectEta/Scripts/UI/Day64FusionUI.cs |
| PieceInfoFrame.png | 1 | Assets/ProjectEta/Scripts/UI/PieceInfoPanelUI.cs |
| AbilityTag.png | 1 | Assets/ProjectEta/Scripts/UI/PieceInfoPanelUI.cs |
| SystemToastFrame.png | 1 | Assets/ProjectEta/Scripts/UI/SystemToastUI.cs |
| BattleAnnouncementFrame.png | 1 | Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncementUI.cs |
| VictoryDefeatPlaque.png | 1 | Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncementUI.cs |
| MainMenuBackdrop.png | 1 | Assets/ProjectEta/Scripts/UI/MainMenuController.cs |
| MainMenuEmblem.png | 1 | Assets/ProjectEta/Scripts/UI/MainMenuController.cs |
| KingCardFrame.png | 2 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs, Assets/ProjectEta/Scripts/King/StrategyKingSelectionUI.cs |
| KingEmblemFrame.png | 1 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs |
| TutorialPageFrame.png | 1 | Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs |
| UiModalFrame.png | 2 | Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/UI/MainMenuController.cs |
| UiCategoryTab.png | 2 | Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiSliderTrack.png | 1 | Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiSliderHandle.png | 1 | Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| MetaUnlockTile.png | 2 | Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/UI/MainMenuController.cs |
| RunResultFrame.png | 1 | Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs |
| UiCloseIcon.png | 1 | Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs |
| UiArrowLeft.png | 4 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs, Assets/ProjectEta/Scripts/Meta/MetaProgressPanelController.cs, Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |
| UiArrowRight.png | 3 | Assets/ProjectEta/Scripts/King/KingSelectionUI.cs, Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs, Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs |

---
## 최종 실행 검증

- 실제 프로젝트 EditMode 859개·PlayMode 3개 통과
- Windows 개발 빌드 성공
- 이미지 Import: alphaIsTransparency 활성·Clamp·MipMap 비활성
- 샘플 렌더 40장: 1920×1080·2560×1440, 실제 UI 생성 코드 사용
- 전체 런의 수동 시각 검수와 밸런스 측정은 별도 작업
