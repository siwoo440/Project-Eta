---
# 98일차 압축 4단계 이미지 생성 프롬프트

- 생성일: 2026-10-08~2026-10-09
- 생성 도구: Codex 내장 `image_gen.imagegen`
- 생성 방식: 자산마다 개별 호출, `transparent_background=true`
- 스타일 참조: `Assets/ProjectEta/Resources/UI/Day98/ChoiceCardFrame.png`
- 저장 폴더: `Assets/ProjectEta/Resources/UI/Day98`
- 글자는 Unity Text로 표시하고 이미지에는 글자·숫자를 포함하지 않음
- 원본 PNG 크기를 유지하며 Unity Import 최대 크기 2048 적용
- 전체 14개 PNG의 네 모서리 알파 값 0 확인. 경계 중점은 메뉴 문양 하단 1/255, 나머지 0 확인

| 파일 | 원본 크기 | 생성 원본 |
|---|---:|---|
| `MainMenuBackdrop.png` | 1672×941 | `exec-a7616dac-2915-47d0-8ad9-5d255380a74f.png` |
| `MainMenuEmblem.png` | 1254×1254 | `exec-9f0924fd-5299-4213-94df-5bdd008670ce.png` |
| `KingCardFrame.png` | 1086×1448 | `exec-e44e4c05-e87e-472a-921d-15f672ff4a71.png` |
| `KingEmblemFrame.png` | 1254×1254 | `exec-8e03f4d9-2b5a-4059-8bd9-73f7c9930157.png` |
| `TutorialPageFrame.png` | 1448×1086 | `exec-cfc80f6a-4b48-4721-a4f8-b8725834aa77.png` |
| `UiModalFrame.png` | 1586×992 | `exec-8bb795ad-d1ab-45a6-9f2a-c5257672b247.png` |
| `UiCategoryTab.png` | 2048×768 | `exec-6a685ae8-034c-4c6a-962c-1a2453a866d2.png` |
| `UiSliderTrack.png` | 2172×724 | `exec-3fdbeb3d-c10a-4b90-a0d7-77817e921330.png` |
| `UiSliderHandle.png` | 1254×1254 | `exec-b51fd322-f0bd-4de5-a01b-94313afd90c2.png` |
| `MetaUnlockTile.png` | 1983×793 | `exec-a155f21b-94f8-4cb0-8f76-5b3fa62d1092.png` |
| `RunResultFrame.png` | 1448×1086 | `exec-183c61f8-6586-4fb6-9c38-74503fe7d490.png` |
| `UiCloseIcon.png` | 1254×1254 | `exec-614ea6d5-cde3-42cb-b51b-42d637aab8e3.png` |
| `UiArrowLeft.png` | 1254×1254 | `exec-a98f5583-cb2e-4681-9e19-8ddaf606fcc1.png` |
| `UiArrowRight.png` | 1254×1254 | `exec-29804766-0380-43d0-a4b4-64da8db09dc9.png` |

---
## MainMenuBackdrop.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: MainMenuBackdrop. wide 16:9 charcoal and dark teal main menu background panel, extremely faint geometric chessboard engraving only along outer edges, thin understated antique gold perimeter, completely quiet broad center for menu UI, outermost two percent transparent. Reference image is style reference only: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## MainMenuEmblem.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: MainMenuEmblem. square isolated symmetric emblem of a geometric chess king topped with a small crown, framed by a restrained gold circle, antique gold and charcoal, no lettering, transparent interior gaps and outside. Reference image is style reference only: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## KingCardFrame.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: KingCardFrame. tall portrait 3:4 selectable king card frame, narrow antique gold border, tiny corner diamonds, completely plain dark charcoal center for existing artwork and text. Reference image is style reference only: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## KingEmblemFrame.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: KingEmblemFrame. square portrait medallion frame with a circular antique gold rim inside, hollow fully transparent center for existing king portrait, restrained small corner ornaments. Match the provided style reference: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## TutorialPageFrame.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: TutorialPageFrame. landscape 4:3 tutorial page frame, narrow gold perimeter with very small corner diamonds, quiet charcoal center with no illustrations or internal dividers. Match the provided style reference: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## UiModalFrame.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: UiModalFrame. landscape 8:5 confirmation dialog frame, quiet charcoal center with thin gold border and tiny restrained corner accents, empty interior. Match the provided style reference: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## UiCategoryTab.png

```text
Use case: ui-mockup. Generate ONE isolated production PNG asset for Unity: UiCategoryTab. wide 8:3 category tab panel, flat charcoal teal center, thin gold rim, subtly clipped corners, center empty for a short label. Match the provided style reference: restrained antique gold, charcoal and dark teal. Flat precise vector-like geometric design, straight crisp symmetrical edges. Frame or glyph fills ninety-four percent of canvas with only three percent clear outer padding. Outside the asset must be genuinely fully transparent, no haze, glow, shadow, fog, checkerboard or colored canvas. No text, numbers, watermark, signature. No multiple assets, no sprite sheet.
```

---
## UiSliderTrack.png

```text
Create ONE Unity UI asset PNG: UiSliderTrack. very wide short 3:1 UI slider rail, a single straight dark teal trough with antique gold thin border, no handle, no segments, no marks, rail takes eighty percent canvas height. Use reference only for precise geometric vector-like gold edge and charcoal teal palette. Gold bevels shallow, NO photographic surface. The outer perimeter is alpha zero: ALL space outside frame/glyph fully TRANSPARENT. NO background glow or gradient outside object. No haze, shadows, fog, checkerboard. Asset fills 94% canvas with 3% clear edge padding. The canvas is a transparent layer. No text, marks, numbers, watermark, multiple assets, sprite sheet. Empty frame interiors dark charcoal unless specified hollow.
```

---
## UiSliderHandle.png

```text
Create ONE Unity UI asset PNG: UiSliderHandle. square compact UI slider thumb, a single small antique gold diamond with a charcoal outer edge, centered, no circle, no letters. Use reference only for precise geometric vector-like gold edge and charcoal teal palette. Gold bevels shallow, NO photographic surface. The outer perimeter is alpha zero: ALL space outside frame/glyph fully TRANSPARENT. NO background glow or gradient outside object. No haze, shadows, fog, checkerboard. Asset fills 94% canvas with 3% clear edge padding. The canvas is a transparent layer. No text, marks, numbers, watermark, multiple assets, sprite sheet. Empty frame interiors dark charcoal unless specified hollow.
```

---
## MetaUnlockTile.png

```text
Create ONE Unity UI asset PNG: MetaUnlockTile. wide 5:2 unlock item card, flat charcoal center, thin gold perimeter and minimal clipped corners, entire interior empty for name cost and status. Use reference only for precise geometric vector-like gold edge and charcoal teal palette. Gold bevels shallow, NO photographic surface. The outer perimeter is alpha zero: ALL space outside frame/glyph fully TRANSPARENT. NO background glow or gradient outside object. No haze, shadows, fog, checkerboard. Asset fills 94% canvas with 3% clear edge padding. The canvas is a transparent layer. No text, marks, numbers, watermark, multiple assets, sprite sheet. Empty frame interiors dark charcoal unless specified hollow.
```

---
## RunResultFrame.png

```text
Create ONE Unity UI asset PNG: RunResultFrame. landscape 4:3 run result panel, plain charcoal center, restrained golden laurel accents confined to extreme top left and top right corners, narrow gold edge, no middle crest or inner dividers. Use reference only for precise geometric vector-like gold edge and charcoal teal palette. Gold bevels shallow, NO photographic surface. The outer perimeter is alpha zero: ALL space outside frame/glyph fully TRANSPARENT. NO background glow or gradient outside object. No haze, shadows, fog, checkerboard. Asset fills 94% canvas with 3% clear edge padding. The canvas is a transparent layer. No text, marks, numbers, watermark, multiple assets, sprite sheet. Empty frame interiors dark charcoal unless specified hollow.
```

---
## UiCloseIcon.png

```text
Generate ONE isolated Unity PNG UI icon: UiCloseIcon. square isolated minimal antique gold X close glyph, exactly two straight equal diagonal bars with crisp charcoal outline, centered and symmetrical, no background panel. Precise clean flat vector-like design antique gold bevel with charcoal outline matching reference palette. Single icon only; no rectangular or circular frame, no dark filled plate, no ornament. Large centered icon fills 80% canvas, transparent outside AND between its bars. Every area except the bars must be completely alpha-zero transparent, no glow, shadows, haze, photographic texture, gradients, background canvas, checkerboard, text, watermark, sprite sheet.
```

---
## UiArrowLeft.png

```text
Generate ONE isolated Unity PNG UI icon: UiArrowLeft. square isolated minimal antique gold left chevron glyph, two straight equal bars forming a clear left pointing angle, crisp charcoal outline, no background panel. Precise clean flat vector-like design antique gold bevel with charcoal outline matching reference palette. Single icon only; no rectangular or circular frame, no dark filled plate, no ornament. Large centered icon fills 80% canvas, transparent outside AND between its bars. Every area except the bars must be completely alpha-zero transparent, no glow, shadows, haze, photographic texture, gradients, background canvas, checkerboard, text, watermark, sprite sheet.
```

---
## UiArrowRight.png

```text
Generate ONE isolated Unity PNG UI icon: UiArrowRight. square isolated minimal antique gold right chevron glyph, two straight equal bars forming a clear right pointing angle, crisp charcoal outline, no background panel. Precise clean flat vector-like design antique gold bevel with charcoal outline matching reference palette. Single icon only; no rectangular or circular frame, no dark filled plate, no ornament. Large centered icon fills 80% canvas, transparent outside AND between its bars. Every area except the bars must be completely alpha-zero transparent, no glow, shadows, haze, photographic texture, gradients, background canvas, checkerboard, text, watermark, sprite sheet.
```
