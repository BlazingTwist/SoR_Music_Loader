using System.Reflection;
using BepInEx;
using BlazingTwist_Core;
using HarmonyLib;
using SoR_Music_Loader.music_loader;

namespace SoR_Music_Loader {
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("StreetsOfRogue.exe")]
	public class SorMusicLoader : BaseUnityPlugin {
		public const string pluginGuid = "blazingtwist.sor.musicloader";
		private const string pluginName = "BlazingTwist SoR MusicLoader";
		private const string pluginVersion = "1.0.0";

		public void Awake() {
			var harmony = new Harmony(pluginGuid);
			RuntimePatcherUtils.RunPatchers(Logger, harmony, RuntimePatcherUtils.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "SoR_Music_Loader.asm"));

			TrackConfigManager.AttachToScene();
			CustomMusicManager.AttachToScene();
		}
	}
}