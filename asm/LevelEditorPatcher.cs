using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BlazingTwist_Core;
using HarmonyLib;
using SoR_Music_Loader.music_loader;

namespace SoR_Music_Loader.asm {
	public class LevelEditorPatcher : RuntimePatcher {
		public override void ApplyPatches() {
			Type originalType = typeof(LevelEditor);
			Type patcherType = typeof(LevelEditorPatcher);

			MethodInfo createMusicListOriginal = AccessTools.Method(originalType, nameof(LevelEditor.CreateMusicList));

			var createMusicListPrefix = new HarmonyMethod(patcherType, nameof(CreateMusicListPrefix), new[] { originalType, typeof(float).MakeByRefType() });

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
			__instance.StartCoroutine(SetScrollBarPlacement(__instance));
			return false;
		}

		private static IEnumerator SetScrollBarPlacement(LevelEditor instance) {
			yield return null;
			int scrollBarNum = instance.GetScrollBarNum();
			instance.scrollBarLoad.value = scrollBarNum == -1
					? 1f
					: instance.loadScrollbarPosAll[scrollBarNum];
		}
	}
}