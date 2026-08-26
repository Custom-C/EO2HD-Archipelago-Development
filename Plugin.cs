using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace EO2Archi;

[BepInPlugin("Etrian2.Archipelago", "Etrian Odyssey 2 Archipelago Plugin", "0.0.1")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        // Plugin startup logic
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}
