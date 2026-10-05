using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.UI
{
    public sealed class PieceAbilityOverlayUI : MonoBehaviour
    {
        private BoardInputController _boardInput;
        private Canvas _canvas;
        private Text _text;
        private static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<PieceAbilityOverlayUI>() != null) return;

            var host = new GameObject("PieceAbilityOverlayUI");
            host.AddComponent<PieceAbilityOverlayUI>();
        }

        private IEnumerator Start()
        {
            EnsureUI();

            for (int frame = 0; frame < 120 && _boardInput == null; frame++)
            {
                _boardInput = Object.FindFirstObjectByType<BoardInputController>();
                if (_boardInput == null) yield return null;
            }

            if (_boardInput != null)
            {
                _boardInput.SelectionChanged += HandleSelectionChanged;
            }

            Refresh(null);
        }

        private void HandleSelectionChanged(PieceRuntimeState piece)
        {
            Refresh(piece);
        }

        private void Refresh(PieceRuntimeState piece)
        {
            if (_text == null) return;

            if (piece?.Definition == null || piece.Definition.Abilities.Length == 0)
            {
                _text.transform.parent.gameObject.SetActive(false);
                return;
            }

            _text.transform.parent.gameObject.SetActive(true);

            var builder = new StringBuilder();
            builder.AppendLine("ABILITIES");

            PieceAbilityDefinition[] abilities = piece.Definition.Abilities;

            for (int i = 0; i < abilities.Length; i++)
            {
                PieceAbilityDefinition ability = abilities[i];
                if (ability == null) continue;

                string type = ability.ActionCost == AbilityActionCost.PlayerAction
                    ? "Active"
                    : "Passive";

                builder.Append("• ");
                builder.Append(ability.DisplayName);
                builder.Append(" [");
                builder.Append(type);
                builder.AppendLine("]");

                if (!string.IsNullOrWhiteSpace(ability.Description))
                {
                    builder.AppendLine(ability.Description);
                }
            }

            _text.text = builder.ToString().TrimEnd();
        }

        private void EnsureUI()
        {
            var canvasObject = new GameObject(
                "PieceAbilityOverlayCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 96;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject(
                "AbilityPanel",
                typeof(RectTransform),
                typeof(Image));

            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-10f, -120f);
            panelRect.sizeDelta = new Vector2(360f, 250f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.1f, 0.92f);
            image.raycastTarget = false;

            var textObject = new GameObject(
                "AbilityText",
                typeof(RectTransform),
                typeof(Text));

            textObject.transform.SetParent(panel.transform, false);
            _text = textObject.GetComponent<Text>();
            _text.font = GetFont();
            _text.fontSize = 15;
            _text.alignment = TextAnchor.UpperLeft;
            _text.color = Color.white;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Truncate;
            _text.raycastTarget = false;

            RectTransform textRect = _text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 12f);
            textRect.offsetMax = new Vector2(-14f, -12f);

            panel.SetActive(false);
        }

        private static Font GetFont()
        {
            if (_font != null) return _font;

            _font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" },
                20);

            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _font;
        }

        private void OnDestroy()
        {
            if (_boardInput != null)
            {
                _boardInput.SelectionChanged -= HandleSelectionChanged;
            }
        }
    }
}
