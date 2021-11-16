using BubbleGauntlet.Utilities;
using Kingmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private static Sprite none;
        private static Sprite maskSprite;

        public static bool Visible {
            set {
                Root?.SetActive(value);
            }
        }

        private static Image[] Encounters = new Image[10];

        public static void Install() {
            if (MainCanvas == null)
                return;
            if (Root != null)
                GameObject.Destroy(Root);

            rest = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Camp_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            fight = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Group_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            shop = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Pack_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            none = AssetLoader.LoadInternal("sprites", "UI_HudIcon_Cancel_Default.png", new Vector2Int(106, 106), TextureFormat.ARGB32);
            maskSprite = AssetLoader.LoadInternal("sprites", "UI_CircleMask256.png", new Vector2Int(256, 256), TextureFormat.ARGB32);

            Root = new GameObject("bubblegauntlet-progressindicator-root", typeof(RectTransform));
            var rect = Root.Rect();
            //var layout = Root.AddComponent<LayoutElement>();

            var bg = new GameObject("bg", typeof(RectTransform));
            bg.AddComponent<Image>().sprite = ActionBar.GetComponent<Image>().sprite;

            Root.AddTo(MainCanvas);
            rect.sizeDelta = new Vector2(800, 50);
            rect.SetAnchor(0.5, 1);
            rect.pivot = new Vector2(0.5f, 1);

            bg.AddTo(Root);
            bg.FillParent();
            bg.Rect().anchoredPosition = Vector2.zero;
            bg.Rect().localScale = new Vector2(1, -1);

            var contents = new GameObject("contents", typeof(RectTransform));
            contents.AddTo(Root);
            contents.FillParent();
            contents.Rect().anchoredPosition = Vector2.zero;
            contents.Rect().pivot = new Vector2(0, 0.5f);

            var grid = contents.AddComponent<GridLayoutGroup>();
            grid.constraintCount = 1;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.cellSize = new Vector2(50, 50);
            grid.spacing = new Vector2(30, 0);
            grid.padding.left = 15;

            for (int i = 0; i < 10; i++) {
                var encounter = new GameObject($"encounter{i}", typeof(RectTransform));
                var mask = new GameObject("icon", typeof(RectTransform));

                mask.AddTo(encounter);
                mask.Rect().SetAnchor(0.2, 0.8, 0.2, 0.8);
                mask.Rect().sizeDelta = Vector2.zero;
                mask.AddComponent<Image>().sprite = maskSprite;
                var maskComponent = mask.AddComponent<Mask>();

                var icon = new GameObject("icon-icon", typeof(RectTransform));
                icon.AddTo(mask);
                icon.FillParent();
                icon.Rect().SetAnchor(-.1, 1.1, -.1, 1.1);
                Encounters[i] = icon.AddComponent<Image>();
                Encounters[i].sprite = none;
                encounter.AddTo(contents);
            }

        }

        internal static void Refresh() {
            Main.Log($"refreshign with {GauntletController.Floor.EncountersCompleted}");
            for (int i = 0; i < 10; i++) {
                if (i >= GauntletController.Floor.EncountersCompleted) {
                    Encounters[i].sprite = none;
                } else {
                    Encounters[i].sprite = GauntletController.Floor.Encounters[i] switch {
                        EncounterType.Fight => fight,
                        EncounterType.EliteFight => fight,
                        EncounterType.Rest => rest,
                        EncounterType.Shop => shop,
                        _ => none,
                    };
                }
            }
        }

        public static void Uninstall() {
            GameObject.Destroy(Root);
        }
    }
}
