using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BlazingTwist_Core;
using HarmonyLib;
using SoR_Music_Loader.music_loader;

namespace SoR_Music_Loader.asm {
	[HarmonyPatch]
	public static class LevelEditorExtension {
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(LevelEditor), "SetScrollBarPlacement")]
		public static IEnumerator SetScrollBarPlacementReverse(this LevelEditor __instance) {
			throw new NotImplementedException("SetScrollBarPlacement hasn't been patched yet!");
		}
	}

	public class LevelEditorPatcher : RuntimePatcher {
		public override void ApplyPatches() {
			Type originalType = typeof(LevelEditor);
			Type patcherType = typeof(LevelEditorPatcher);

			MethodInfo createMusicListOriginal = AccessTools.Method(originalType, nameof(LevelEditor.CreateMusicList));

			var createMusicListPrefix = new HarmonyMethod(patcherType, nameof(CreateMusicListPrefix), new[] { originalType, typeof(bool).MakeByRefType() });

			harmony.Patch(createMusicListOriginal, createMusicListPrefix);
		}

		private static bool CreateMusicListPrefix(LevelEditor __instance, ref float ___numButtonsLoad) {
			List<string> trackList = new List<string> {
					"Home_Base_v2",
					"Level1_1",
					"Level1_2",
					"Level1_3",
					"Level2_1",
					"Level2_2",
					"Level2_3",
					"Level3_1",
					"Level3_2",
					"Level3_3",
					"Level4_1",
					"Level4_2",
					"Level4_3",
					"Level5_1",
					"Level5_2",
					"Level5_3",
					"Level6"
			};
			trackList.AddRange(CustomMusicManager.GetCustomMusic().Keys);
			__instance.ActivateLoadMenu();
			___numButtonsLoad = trackList.Count;
			__instance.OpenObjectLoad(trackList);
			__instance.StartCoroutine(__instance.SetScrollBarPlacementReverse());
			return false;
		}
	}
}