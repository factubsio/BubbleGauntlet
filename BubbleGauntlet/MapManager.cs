using BubbleGauntlet.Utilities;
using Kingmaker.Blueprints.Area;
using Kingmaker.RandomEncounters.Settings;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {

    public class AreaMap {
        public BlueprintAreaEnterPoint AreaEnter;
        public BlueprintArea Area;
        public Func<AreaMap, UnitPlacer> GetVendorSpawnLocation;
        public Func<AreaMap, UnitPlacer> GetBubbleLocation;
        public Func<AreaMap, UnitPlacer> GetServiceLocation;
        public Func<AreaMap, UnitPlacer> GetCombatLocation;

        public UnitPlacer VendorSpawnLocation => GetVendorSpawnLocation(this);
        public UnitPlacer BubbleLocation => GetBubbleLocation(this);
        public UnitPlacer ServiceLocation => GetServiceLocation(this);
        public UnitPlacer CombatLocation => GetCombatLocation(this);

        public static AreaMap FromRE(string re) {
            AreaMap map = new();
            var areaRE = BP.GetBlueprint<BlueprintRandomEncounter>(re);
            map.AreaEnter = areaRE.AreaEntrance;
            map.Area = map.AreaEnter.Area;
            map.Area.LoadingScreenSprites.Add(MapManager.BGSprite);
            map.Area.CampingSettings.CampingAllowed = false;
            return map;
        }

        public static AreaMap FromEnterPoint(string id) {
            AreaMap map = new();
            map.AreaEnter = BP.GetBlueprint<BlueprintAreaEnterPoint>(id);
            map.Area = map.AreaEnter.Area;
            map.Area.LoadingScreenSprites.Clear();
            map.Area.LoadingScreenSprites.Add(MapManager.BGSprite);
            map.Area.CampingSettings.CampingAllowed = false;
            return map;
        }

        internal static Func<AreaMap, UnitPlacer> FromEnterPoint(float dx, float dz, Vector3 toward) {
            return area => {
                var pos = AreaEnterPoint.FindAreaEnterPointOnScene(area.AreaEnter).transform.position;
                pos.x += dx;
                pos.z += dz;
                return (at: pos, look: toward);
            };
        }

        internal static Func<AreaMap, UnitPlacer> Absolute(double x, double y, double z, Vector3 look) {
            return _ => {
                return (new Vector3((float)x, (float)y, (float)z), look);
            };
        }
    }

    public struct UnitPlacer {
        public Vector3 at;
        public Vector3 look;

        public UnitPlacer(Vector3 at, Vector3 look) {
            this.at = at;
            this.look = look;
        }

        public override bool Equals(object obj) {
            return obj is UnitPlacer other &&
                   at.Equals(other.at) &&
                   look.Equals(other.look);
        }

        public override int GetHashCode() {
            int hashCode = -97652386;
            hashCode = hashCode * -1521134295 + at.GetHashCode();
            hashCode = hashCode * -1521134295 + look.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out Vector3 at, out Vector3 look) {
            at = this.at;
            look = this.look;
        }

        public static implicit operator (Vector3 at, Vector3 look)(UnitPlacer value) {
            return (value.at, value.look);
        }

        public static implicit operator UnitPlacer((Vector3 at, Vector3 look) value) {
            return new UnitPlacer(value.at, value.look);
        }
    }

    public static class MapManager {

        private static Dictionary<Guid, AreaMap> MapByArea = new();
        public static List<AreaMap> Maps = new();
        public static Sprite BGSprite;

        internal static AreaMap MapForArea(Guid guid) {
            return MapByArea[guid];
        }
        public static void Install() {
            BGSprite = AssetLoader.LoadInternal("sprites", "gauntlet_loading.png", new Vector2Int(2048, 1024), TextureFormat.DXT5);

            try {
                var map = AreaMap.FromEnterPoint("87ad2ce9c34a0d242b7cf8c28c292b83");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(5, 4, new Vector3(-1, 0, -1));
                map.GetServiceLocation = AreaMap.FromEnterPoint(3, 4, new Vector3(-1, 0, -1));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(3, -2, new Vector3(0, 0, 1));
                map.GetCombatLocation = AreaMap.Absolute(3.5562, 35.4344, 20.1404, new Vector3(1, 0, 0));
                Maps.Add(map);
            } catch (Exception ex) {
                Main.Error(ex, "loading area");
            }
            try {
                var map = AreaMap.FromEnterPoint("9bd3c794a80b80849b839b535cb4f1d8");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(10, 4, new Vector3(-1, 0, -1));
                map.GetServiceLocation = AreaMap.FromEnterPoint(12, 4, new Vector3(-1, 0, -1));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(10, -2, new Vector3(0, 0, 1));
                map.GetCombatLocation = AreaMap.Absolute(11.0368, 40.0144, 21.7244, new Vector3(1, 0, 0));
                Maps.Add(map);
            } catch (Exception ex) {
                Main.Error(ex, "loading area");
            }
            try {
                var map = AreaMap.FromEnterPoint("d17f3127e74dfa54fb8af62425a40832");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(5, 4, new Vector3(-1, 0, -1));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(5, 1, new Vector3(-1, 0, 0));
                map.GetServiceLocation = AreaMap.FromEnterPoint(3, 4, new Vector3(0, 0, -1));
                map.GetCombatLocation = AreaMap.Absolute(2.1631, 41.8513, -3.3925, new Vector3(1, 0, 0));
                Maps.Add(map);
            } catch (Exception ex) {
                Main.Error(ex, "loading area");
            }
            try {
                var map = AreaMap.FromEnterPoint("18e65357a77c7f2418430d408ddc9051");
                map.GetBubbleLocation = AreaMap.Absolute(-5.7884, 0.495, -7.1511, new Vector3(1, 0, 0));
                map.GetVendorSpawnLocation = AreaMap.Absolute(-6.1021, 0.495, -5.8734, new Vector3(1, 0, -1));
                map.GetServiceLocation = AreaMap.Absolute(-6.5722, 0.495, -9.359, new Vector3(1, 0, 0));
                map.GetCombatLocation = AreaMap.Absolute(0.0621, 4.5452, 18.8564, new Vector3(-1, 0, 0));
                Maps.Add(map);
            } catch (Exception ex) {
                Main.Error(ex, "loading area");
            }
            {
                var map = AreaMap.FromEnterPoint("865aff956d145824f952e9cb5f086ef7");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(-2, -3, new Vector3(0, 0, 1));
                map.GetServiceLocation = AreaMap.FromEnterPoint(0, -4, new Vector3(0, 0, 1));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(-4, -2, new Vector3(0, 0, 1));
                map.GetCombatLocation = AreaMap.Absolute(68.4138, 39.6377, 76.6277, new Vector3(-1, 0, 0));
                Maps.Add(map);
            }
            {
                var map = AreaMap.FromEnterPoint("3375e2ac9d3cf97409dda787c301a868");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(3, -1, new Vector3(-1, 0, 0));
                map.GetServiceLocation = AreaMap.FromEnterPoint(3, 1, new Vector3(-1, 0, 0));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(-4, -4, new Vector3(0, -1, 0));
                map.GetCombatLocation = AreaMap.Absolute(9.3785, 40.0557, 139.38, new Vector3(1, 0, 0));
                Maps.Add(map);
            }
            {
                var map = AreaMap.FromRE("5bb1c0aeb0cda2b41aca56ba6af52980");
                map.GetBubbleLocation = AreaMap.FromEnterPoint(-2, -4, new Vector3(0, 0, 1));
                map.GetServiceLocation = AreaMap.FromEnterPoint(0, -4, new Vector3(0, 0, 1));
                map.GetVendorSpawnLocation = AreaMap.FromEnterPoint(-4, -4, new Vector3(0, 0, 1));
                map.GetCombatLocation = AreaMap.Absolute(27.0143, 39.9988, 43.9386, new Vector3(0, 0, -1));
                Maps.Add(map);
            }

            foreach (var map in Maps)
                MapByArea[map.Area.AssetGuid.m_Guid] = map;
        }
    }
}
