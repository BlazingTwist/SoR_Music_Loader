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
		internal static void OnTrackAddition(string trackName, TrackConfig trackConfig) {
			trackAdditionQueue[trackName] = trackConfig;
		}

		internal static void OnTrackChanged(string trackName, TrackConfig trackConfig) {
			trackChangedQueue[trackName] = trackConfig;
		}

		internal static void OnClipAddition(string trackName, AudioClip clip) {
			clipAdditionQueue[trackName] = clip;
		}

		internal static void OnCustomTrackAddition(CustomMusicLoadSpec loadSpec) {
			Debug.Log("Enqueueing new custom track: " + loadSpec);
			customMusicAdditionQueue.Add(loadSpec);
		}

		internal static void OnCustomTrackChanged(CustomMusicLoadSpec loadSpec) {
			Debug.Log("Enqueueing changed custom track: " + loadSpec);
			customMusicChangedQueue.Add(loadSpec);
		}

		private static readonly Dictionary<string, TrackConfig> trackAdditionQueue = new Dictionary<string, TrackConfig>();
		private static readonly Dictionary<string, TrackConfig> trackChangedQueue = new Dictionary<string, TrackConfig>();
		private static readonly Dictionary<string, AudioClip> clipAdditionQueue = new Dictionary<string, AudioClip>();
		private static readonly List<CustomMusicLoadSpec> customMusicAdditionQueue = new List<CustomMusicLoadSpec>();
		private static readonly List<CustomMusicLoadSpec> customMusicChangedQueue = new List<CustomMusicLoadSpec>();

		public override void ApplyPatches() {
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
			Debug.Log("CustomTrack " + trackName + " finished loading, clip is " + (audioClip == null ? "null" : "not null"));
			if (audioClip == null) {
				audioHandler.gc.sessionDataBig.musicTrackDic.Remove(trackName);
			} else {
				audioHandler.gc.sessionDataBig.musicTrackDic[trackName] = audioClip;
			}
		}

		private static void MusicLoad(AudioHandler audioHandler) {
			foreach ((string key, TrackConfig config) in trackAdditionQueue) {
				TrackConfigManager.instance.StartCoroutine(
						TrackLoadingUtils.LoadMusicTrack(
								config,
								clip => OnTrackLoaded(audioHandler, key, clip))
				);
			}
			trackAdditionQueue.Clear();

			foreach ((string key, TrackConfig config) in trackChangedQueue) {
				TrackConfigManager.instance.StartCoroutine(
						TrackLoadingUtils.LoadMusicTrack(
								config,
								clip => OnTrackLoaded(audioHandler, key, clip))
				);
			}
			trackChangedQueue.Clear();

			foreach ((string trackName, AudioClip clip) in clipAdditionQueue) {
				OnTrackLoaded(audioHandler, trackName, clip);
			}
			clipAdditionQueue.Clear();

			foreach (CustomMusicLoadSpec customMusicLoadSpec in customMusicAdditionQueue) {
				Debug.Log("Starting load for new custom track: " + customMusicLoadSpec);
				Action<CustomMusicLoadSpec, AudioClip> previousCallback = customMusicLoadSpec.callback;
				customMusicLoadSpec.callback = (loadSpec, clip) => {
					previousCallback?.Invoke(loadSpec, clip);
					OnTrackLoaded(audioHandler, loadSpec.GetDisplayName(), clip);
				};
				TrackConfigManager.instance.StartCoroutine(TrackLoadingUtils.LoadMusicTrack(customMusicLoadSpec));
			}
			customMusicAdditionQueue.Clear();

			foreach (CustomMusicLoadSpec customMusicLoadSpec in customMusicChangedQueue) {
				Debug.Log("Starting load for changed custom track: " + customMusicLoadSpec);
				Action<CustomMusicLoadSpec, AudioClip> previousCallback = customMusicLoadSpec.callback;
				customMusicLoadSpec.callback = (loadSpec, clip) => {
					previousCallback?.Invoke(loadSpec, clip);
					OnTrackLoaded(audioHandler, loadSpec.GetDisplayName(), clip);
				};
				TrackConfigManager.instance.StartCoroutine(TrackLoadingUtils.LoadMusicTrack(customMusicLoadSpec));
			}
			customMusicChangedQueue.Clear();

			RunTitleScreenFix();
		}

		private static void RunTitleScreenFix() {
			// have to patch title screen manually here because the logo screen used the index instead of the key
			var audioHandler = AudioHandler.audioHandler;
			if (audioHandler != null) {
				Dictionary<string, AudioClip> musicTrackDic = audioHandler.gc.sessionDataBig.musicTrackDic;
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