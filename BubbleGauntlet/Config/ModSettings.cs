using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using static UnityModManagerNet.UnityModManager;

namespace BubbleGauntlet.Config {
    static class ModSettings {
        public static ModEntry ModEntry;
        public static Fixes Fixes;
        public static AddedContent AddedContent;
        public static Blueprints Blueprints;

        public static void LoadAllSettings() {
            LoadSettings("Fixes.json", ref Fixes);
            LoadSettings("AddedContent.json", ref AddedContent);
            LoadSettings("Blueprints.json", ref Blueprints);
        }
        public static void LoadSettings<T>(string fileName, ref T setting, bool fromFile = false) {
            var assembly = Assembly.GetExecutingAssembly();
            string userConfigFolder = ModEntry.Path + "UserSettings";
            Directory.CreateDirectory(userConfigFolder);
            var resourcePath = $"BubbleGauntlet.Config.{fileName}";

            StreamReader reader;
            if (fromFile)
                reader = File.OpenText(Path.Combine(userConfigFolder, fileName));
            else {
                Stream stream = assembly.GetManifestResourceStream(resourcePath);
                reader = new StreamReader(stream);
            }

            JsonSerializer serializer = new JsonSerializer {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented
            };
            var jReader = new JsonTextReader(reader);
            setting = serializer.Deserialize<T>(jReader);

            reader.Close();
            reader.Dispose();

        }

    }
}
