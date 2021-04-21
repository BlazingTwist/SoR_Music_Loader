using System;
using System.Collections.Generic;
using System.Reflection;
using BlazingTwist_Core;
using HarmonyLib;
using SoR_Music_Loader.music_loader;
using SoR_Music_Loader.music_loader.config;
using UnityEngine;

namespace SoR_Music_Loader.asm {
	public class AudioHandlerPatcher : RuntimePatcher {
		private static readonly Dictionary<string, TrackConfig> trackAdditionQueue = new Dictionary<string, TrackConfig>();
		private static readonly Dictionary<string, TrackConfig> trackChangedQueue = new Dictionary<string, TrackConfig>();

		public override void ApplyPatches() {
			TrackConfigManager.onTrackAddition += (name, track) => { trackAdditionQueue[name] = track; };
			TrackConfigManager.onTrackChanged += (name, track) => { trackChangedQueue[name] = track; };

			Type originalType = typeof(AudioHandler);
			Type patcherType = typeof(AudioHandlerPatcher);

			MethodInfo musicLoadOriginal = AccessTools.Method(originalType, nameof(AudioHandler.MusicLoad));

			var musicLoadPrefix = new HarmonyMethod(patcherType, nameof(MusicLoadPrefix), new[] { typeof(AudioHandler) });

			harmony.Patch(musicLoadOriginal, musicLoadPrefix);
		}

		private static void MusicLoadPrefix(AudioHandler __instance) {
			MusicLoad(__instance);
		}

		private static void OnTrackLoaded(AudioHandler audioHandler, string trackName, AudioClip audioClip) {
			audioHandler.gc.sessionDataBig.musicTrackDic[trackName] = audioClip;
		}

		public static void MusicLoad(AudioHandler audioHandler) {
			foreach (KeyValuePair<string, TrackConfig> trackAddition in trackAdditionQueue) {
				TrackConfigManager.instance.StartCoroutine(
						TrackLoadingUtils.LoadMusicTrack(
								trackAddition.Value,
								clip => OnTrackLoaded(audioHandler, trackAddition.Key, clip)
						)
				);
			}

			trackAdditionQueue.Clear();

			foreach (KeyValuePair<string, TrackConfig> trackAddition in trackChangedQueue) {
				TrackConfigManager.instance.StartCoroutine(
						TrackLoadingUtils.LoadMusicTrack(
								trackAddition.Value,
								clip => OnTrackLoaded(audioHandler, trackAddition.Key, clip)
						)
				);
			}

			trackChangedQueue.Clear();
			RunTitleScreenFix();
		}

		private static void RunTitleScreenFix() {
			// have to patch title screen manually here because the logo screen used the index instead of the key
			var audioHandler = AudioHandler.audioHandler;
			if (audioHandler != null) {
				Dictionary<string,AudioClip> musicTrackDic = audioHandler.gc.sessionDataBig.musicTrackDic;
				if (musicTrackDic.ContainsKey(LogoMenuPatcher.TitleScreenTrack) && musicTrackDic[LogoMenuPatcher.TitleScreenTrack] != null) {
					AudioClip realClip = musicTrackDic["TitleScreen"];
					if (audioHandler.musicTrackRealList.Count > 23 && realClip != audioHandler.musicTrackRealList[23]) {
						audioHandler.musicTrackRealList[23] = realClip;
					}
				}
			} else {
				Debug.LogError("AudioHandler was null!");
			}
		}
	}
}