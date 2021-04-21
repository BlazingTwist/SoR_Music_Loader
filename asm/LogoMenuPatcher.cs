using System;
using System.Collections.Generic;
using System.Reflection;
using BlazingTwist_Core;
using HarmonyLib;
using SoR_Music_Loader.music_loader;
using SoR_Music_Loader.music_loader.config;
using UnityEngine;

namespace SoR_Music_Loader.asm {
	public class LogoMenuPatcher : RuntimePatcher {
		public const string TitleScreenTrack = "TitleScreen";

		public override void ApplyPatches() {
			Type originalType = typeof(LogoMenu);
			Type patcherType = typeof(LogoMenuPatcher);

			MethodInfo logoAnimationOriginal = AccessTools.Method(originalType, nameof(LogoMenu.LogoAnimation));

			var logoAnimationPrefix = new HarmonyMethod(patcherType, nameof(LogoAnimationPrefix));

			harmony.Patch(logoAnimationOriginal, logoAnimationPrefix);
		}

		private static void LogoAnimationPrefix() {
			if (TrackConfigManager.trackConfig == null) {
				return;
			}

			Dictionary<string, TrackConfig> trackList = TrackConfigManager.trackConfig.trackList;
			if (trackList == null || !trackList.ContainsKey(TitleScreenTrack)) {
				return;
			}

			TrackConfig track = trackList[TitleScreenTrack];
			if (track == null || string.IsNullOrEmpty(track.filePath)) {
				return;
			}

			AudioClip clip = TrackLoadingUtils.LoadMusicTrackNow(track);
			if (clip != null && AudioHandler.audioHandler != null) {
				AudioHandler.audioHandler.musicTrackRealList[23] = clip;
				AudioHandler.audioHandler.gc.sessionDataBig.musicTrackDic[TitleScreenTrack] = clip;
			}
		}
	}
}