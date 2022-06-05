using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BubbleGauntlet.Config;
using UnityEngine;

namespace BubbleGauntlet.Utilities {
    public class AssetLoader {
        public static Sprite LoadInternal(string folder, string file, Vector2Int size, TextureFormat format) {
            return CreateSprite($"{ModSettings.ModEntry.Path}Assets{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}{file}", size, format);
        }
        public static Texture2D LoadTexture(string folder, string file, Vector2Int size, TextureFormat format) {
            return CreateTexture($"{ModSettings.ModEntry.Path}Assets{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}{file}", size, format);
        }
        public static Sprite CreateSprite(string filePath, Vector2Int size, TextureFormat format) {
            return Sprite.Create(CreateTexture(filePath, size, format), new Rect(0, 0, size.x, size.y), new Vector2(0, 0));
        }
        public static Texture2D CreateTexture(string filePath, Vector2Int size, TextureFormat format) {
            var bytes = File.ReadAllBytes(filePath);
            var texture = new Texture2D(size.x, size.y, format, false);
            texture.mipMapBias = 15.0f;
            _ = texture.LoadImage(bytes);
            return texture;
        }

        public static Dictionary<string, GameObject> Objects = new();
        public static Dictionary<string, Sprite> Sprites = new();
        public static Dictionary<string, Mesh> Meshes = new();
        public static Dictionary<string, Material> Materials = new();

        public static void RemoveBundle(string loadAss, bool unloadAll = false) {
            AssetBundle bundle;
            if (bundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(x => x.name == loadAss))
                bundle.Unload(unloadAll);
            if (unloadAll) {
                Objects.Clear();
                Sprites.Clear();
                Meshes.Clear();
            }
        }

        public static UnityEngine.Object[] Assets;

        public static void AddBundle(string loadAss) {
            try {
                AssetBundle bundle;

                RemoveBundle(loadAss, true);

                var path = Path.Combine(Main.ModPath + loadAss);
                Main.Log($"loading from: {path}");

                bundle = AssetBundle.LoadFromFile(path);
                if (!bundle) throw new Exception($"Failed to load AssetBundle! {Main.ModPath + loadAss}");

                Assets = bundle.LoadAllAssets();

                foreach (var obj in Assets) {
                    Main.Log($"Found asset <{obj.name}> of type [{obj.GetType()}]");
                }

                foreach (var obj in Assets) {
                    if (obj is GameObject gobj)
                        Objects[obj.name] = gobj;
                    else if (obj is Mesh mesh)
                        Meshes[obj.name] = mesh;
                    else if (obj is Sprite sprite)
                        Sprites[obj.name] = sprite;
                    else if (obj is Material material)
                        Materials[obj.name] = material;
                }

                RemoveBundle(loadAss);
            } catch (Exception ex) {
                Main.Error(ex, "LOADING ASSET");
            }
        }
    }
}
