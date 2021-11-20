using BubbleGauntlet.Utilities;
using Kingmaker;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._VM.Tooltip;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.MVVM._VM.Tooltip.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleGauntlet {
    public static class ProgressIndicator {
        public static Transform MainCanvas => Game.Instance?.UI?.Canvas?.transform;
        public static GameObject ActionBar {
            get {
                Transform parent = Game.Instance.UI.Canvas.transform.Find("ActionBarPcView/ActionBarBottomView/ActionBarMainContainer/ActionBarContainer");
                for (int i = 0; i < parent.childCount; i++) {
                    Transform child = parent.GetChild(i);
                    if (child.name == "Background" && child.childCount == 0) {
                        return child.gameObject;
                    }
                }
                return null;
            }
        }
        private static GameObject Root;

        private static Sprite rest;
        private static Sprite fight;
        private static Sprite shop;
        private static Sprite maskSprite;
        private static Sprite borderSprite;
        private static Sprite stairSprite;

        public static bool Visible {
            set {
                Root?.SetActive(value);
            }
        }

        private static EncounterChupa[] Encounters = new EncounterChupa[10];
        private static TextMeshProUGUI CurrentFloorLabel;
        private static TextMeshProUGUI CurrentThemeLabel;

        public static void Install() {
            if (MainCanvas == null)
                return;
            if (Root != null)
                GameObject.Destroy(Root);

            rest = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Camp_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            fight = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Group_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            shop = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Pack_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            maskSprite = AssetLoader.LoadInternal("sprites", "UI_CircleMask256.png", new Vector2Int(256, 256), TextureFormat.ARGB32);

            borderSprite = AssetLoader.LoadInternal("sprites", "circle_border.png", new Vector2Int(108, 108), TextureFormat.ARGB32);

            stairSprite = AssetLoader.LoadInternal("sprites", "stair.png", new Vector2Int(128, 128), TextureFormat.ARGB32);

            Root = new GameObject("bubblegauntlet-progressindicator-root", typeof(RectTransform));
            var rect = Root.Rect();

            Root.AddTo(MainCanvas);
            Root.transform.SetAsFirstSibling();
            rect.sizeDelta = new Vector2(800, 50);
            rect.SetAnchor(0.5, 1);
            rect.pivot = new Vector2(0.5f, 1);

            var bg = new GameObject("bg", typeof(RectTransform));
            bg.AddComponent<Image>().sprite = ActionBar.GetComponent<Image>().sprite;

            bg.AddTo(Root);
            bg.FillParent();
            bg.Rect().anchoredPosition = Vector2.zero;
            bg.Rect().localScale = new Vector2(1, -1);

            var line = new GameObject("line", typeof(RectTransform));
            line.AddComponent<Image>().color = Color.gray;

            line.AddTo(Root);
            line.FillParent();
            line.Rect().anchoredPosition = Vector2.zero;
            line.Rect().SetAnchor(.1, .9, 0.45, 0.55);

            var currentFloorIcon = new GameObject("current-floor-icon", typeof(RectTransform));
            currentFloorIcon.AddComponent<Image>().sprite = stairSprite;
            currentFloorIcon.AddTo(Root);
            currentFloorIcon.Rect().pivot = new Vector2(0, 0.5f);
            currentFloorIcon.Rect().SetAnchor(0.01, .5);
            currentFloorIcon.Rect().sizeDelta = new Vector2(16, 16);

            var currentTheme = new GameObject("current-theme-icon", typeof(RectTransform));
            //var currentThemeLabel = currentThemeIcon.AddComponent<TextMeshProUGUI>();
            currentTheme.AddTo(Root);
            currentTheme.Rect().pivot = new Vector2(1, 0.5f);
            currentTheme.Rect().SetAnchor(0.98, .5);
            currentTheme.Rect().sizeDelta = new Vector2(32, 32);
            currentTheme.AddComponent<Image>().sprite = maskSprite;
            currentTheme.GetComponent<Image>().color = new Color(0.9f, .9f, .8f);

            TooltipHelper.SetTooltip(currentTheme.GetComponent<Image>(), new TooltipFloorThemeTemplate());

            var currentThemeBorder = new GameObject("current-theme-border", typeof(RectTransform));
            currentThemeBorder.AddTo(currentTheme);
            currentThemeBorder.FillParent();
            currentThemeBorder.Rect().localScale = new Vector3(1.1f, 1.1f, 1.0f);
            currentThemeBorder.AddComponent<Image>().sprite = borderSprite;
            currentThemeBorder.GetComponent<Image>().color = Color.blue;

            var currentThemeLabel = new GameObject("current-theme-label", typeof(RectTransform));
            currentThemeLabel.AddTo(currentTheme);
            currentThemeLabel.FillParent();
            var currentThemeLabelText = currentThemeLabel.AddComponent<TextMeshProUGUI>();
            currentThemeLabelText.text = "<size=150%>" + UIUtilityTexts.GetSingleEnergyTextSymbol(Kingmaker.Enums.Damage.DamageEnergyType.Fire) + "</size>";
            currentThemeLabelText.alignment = TextAlignmentOptions.Midline;
            CurrentThemeLabel = currentThemeLabelText;

            var currentFloor = new GameObject("current-floor-label", typeof(RectTransform));
            var currentFloorLabel = currentFloor.AddComponent<TextMeshProUGUI>();
            currentFloorLabel.alignment = TextAlignmentOptions.MidlineLeft;
            currentFloorLabel.color = new Color(.2f, .2f, .2f);
            currentFloorLabel.text = "-";
            CurrentFloorLabel = currentFloorLabel;
            currentFloor.AddTo(Root);
            currentFloor.Rect().pivot = new Vector2(0, 0.5f);
            currentFloor.Rect().SetAnchor(.035, .035, 0, 1);

            var contents = new GameObject("contents", typeof(RectTransform));
            contents.AddTo(Root);
            //contents.FillParent();
            contents.Rect().anchoredPosition = Vector2.zero;
            contents.Rect().pivot = new Vector2(0.5f, 0.5f);

            var grid = contents.AddComponent<GridLayoutGroup>();
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.cellSize = new Vector2(50, 50);
            grid.spacing = new Vector2(20, 0);

            for (int i = 0; i < 10; i++) {
                var encounter = new GameObject($"encounter{i}", typeof(RectTransform));
                encounter.AddComponent<NullBehaviour>().SetTooltip(new TooltipEncounterTemplate(i));

                var inset = new GameObject("inset", typeof(RectTransform));

                inset.AddTo(encounter);
                inset.Rect().SetAnchor(0.2, 0.8, 0.2, 0.8);
                inset.Rect().sizeDelta = Vector2.zero;


                var mask = new GameObject("icon", typeof(RectTransform));
                mask.AddComponent<Image>().sprite = maskSprite;
                mask.AddComponent<Mask>();
                mask.AddTo(inset);
                mask.FillParent();

                var icon = new GameObject("icon-icon", typeof(RectTransform));
                icon.AddTo(mask);
                icon.FillParent();
                icon.Rect().SetAnchor(-.1, 1.1, -.1, 1.1);
                Encounters[i].Main = icon.AddComponent<Image>();
                Encounters[i].Main.sprite = null;
                Encounters[i].Main.color = Color.gray;

                var border = new GameObject("borderb", typeof(RectTransform));
                Encounters[i].Border = border.AddComponent<Image>();
                Encounters[i].Border.sprite = borderSprite;
                Encounters[i].Border.color = Color.gray;
                border.AddTo(inset);
                border.FillParent();
                border.Rect().SetAnchor(-.1, 1.1, -.1, 1.1);


                encounter.AddTo(contents);
            }

        }

        internal static void Refresh() {
            var state = GauntletController.Floor;
            CurrentFloorLabel.text = state.Level.ToString();
            CurrentThemeLabel.text = UIUtilityTexts.GetSingleEnergyTextSymbol(state.DamageTheme);
            for (int i = 0; i < 10; i++) {
                if (i >= state.EncountersCompleted) {
                    Encounters[i].Main = null;
                    Encounters[i].Main.color = Color.gray;
                } else {
                    Encounters[i].Main.sprite = state.Encounters[i] switch {
                        EncounterType.Fight => fight,
                        EncounterType.EliteFight => fight,
                        EncounterType.Rest => rest,
                        EncounterType.Shop => shop,
                        _ => null,
                    };
                    Encounters[i].Main.color = Color.white;
                }

                if (i == state.ActiveEncounter)
                    Encounters[i].Border.color = Color.yellow;
                else
                    Encounters[i].Border.color = Color.white;
            }
        }

        public static void Uninstall() {
            GameObject.Destroy(Root);
        }
    }
    public class TooltipFloorThemeTemplate : TooltipBaseTemplate {
        public override IEnumerable<ITooltipBrick> GetHeader(TooltipTemplateType type) {
            yield return new TooltipBrickText("Current Theme", TooltipTextType.BoldCentered);
        }
        public override IEnumerable<ITooltipBrick> GetBody(TooltipTemplateType type) {
            var state = GauntletController.Floor;
            yield return new TooltipBrickText($"Damage Element: {state.DamageTheme} " + UIUtilityTexts.GetSingleEnergyTextSymbol(state.DamageTheme));
        }
    }
    public class TooltipEncounterTemplate : TooltipBaseTemplate {
        private int index;

        private static ITooltipBrick FutureEncounter => new TooltipBrickText("A future encounter");

        public TooltipEncounterTemplate(int index) {
            this.index = index;
        }

        public override IEnumerable<ITooltipBrick> GetHeader(TooltipTemplateType type) {
            yield return new TooltipBrickText("Encounter", TooltipTextType.BoldCentered);
        }
        public override IEnumerable<ITooltipBrick> GetBody(TooltipTemplateType type) {
            var state = GauntletController.Floor;
            if (index < state.EncountersCompleted) {
                yield return new TooltipBrickText("You chose to " + state.Encounters[index].ToString());
            } else {
                yield return FutureEncounter;
            }
        }
    }

    public class NullBehaviour : MonoBehaviour { }

    internal struct EncounterChupa {
        public Image Main;
        public Image Border;

        public EncounterChupa(Image sprite, Image border) {
            this.Main = sprite;
            this.Border = border;
        }

        public override bool Equals(object obj) {
            return obj is EncounterChupa other &&
                   EqualityComparer<Image>.Default.Equals(Main, other.Main) &&
                   EqualityComparer<Image>.Default.Equals(Border, other.Border);
        }

        public override int GetHashCode() {
            int hashCode = 1629461171;
            hashCode = hashCode * -1521134295 + EqualityComparer<Image>.Default.GetHashCode(Main);
            hashCode = hashCode * -1521134295 + EqualityComparer<Image>.Default.GetHashCode(Border);
            return hashCode;
        }

        public void Deconstruct(out Image sprite, out Image border) {
            sprite = this.Main;
            border = this.Border;
        }

        public static implicit operator (Image sprite, Image border)(EncounterChupa value) {
            return (value.Main, value.Border);
        }

        public static implicit operator EncounterChupa((Image sprite, Image border) value) {
            return new EncounterChupa(value.sprite, value.border);
        }
    }
}
