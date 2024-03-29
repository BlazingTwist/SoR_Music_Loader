﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
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

		private static bool runScript = false;

		private class UnlockData {
			public string unlockName;
			public string unlockNameType;
			public string unlockType;
			public bool unlocked;
			public bool nowAvailable;
			public bool unavailable;
			public bool isUpgrade;
			public bool cantSwap;
			public bool cantLose;
			public List<string> cancellations;
			public List<string> recommendations;
			public string upgrade;
			public int cost;
			public int cost2;
			public int cost3;

			public UnlockData(Unlock unlock) {
				unlockName = unlock.unlockName;
				unlockNameType = unlock.unlockNameType;
				unlockType = unlock.unlockType;
				unlocked = unlock.unlocked;
				nowAvailable = unlock.nowAvailable;
				unavailable = unlock.unavailable;
				isUpgrade = unlock.isUpgrade;
				cantSwap = unlock.cantSwap;
				cantLose = unlock.cantLose;
				cancellations = unlock.cancellations;
				recommendations = unlock.recommendations;
				upgrade = unlock.upgrade;
				cost = unlock.cost;
				cost2 = unlock.cost2;
				cost3 = unlock.cost3;
			}

			public static void AddHeaderRow(StringBuilder builder) {
				builder.Append(nameof(unlockName))
						.Append("\t").Append(nameof(unlockNameType))
						.Append("\t").Append(nameof(unlockType))
						.Append("\t").Append(nameof(unlocked))
						.Append("\t").Append(nameof(nowAvailable))
						.Append("\t").Append(nameof(unavailable))
						.Append("\t").Append(nameof(isUpgrade))
						.Append("\t").Append(nameof(cantSwap))
						.Append("\t").Append(nameof(cantLose))
						.Append("\t").Append(nameof(cancellations))
						.Append("\t").Append(nameof(recommendations))
						.Append("\t").Append(nameof(upgrade))
						.Append("\t").Append(nameof(cost))
						.Append("\t").Append(nameof(cost2))
						.Append("\t").Append(nameof(cost3));
			}
			
			public void ToString(StringBuilder builder) {
				builder.Append(unlockName)
						.Append("\t").Append(unlockNameType)
						.Append("\t").Append(unlockType)
						.Append("\t").Append(unlocked)
						.Append("\t").Append(nowAvailable)
						.Append("\t").Append(unavailable)
						.Append("\t").Append(isUpgrade)
						.Append("\t").Append(cantSwap)
						.Append("\t").Append(cantLose)
						.Append("\t").Append("[").Append(cancellations.Join(str => "'" + str + "'", ", ")).Append("]")
						.Append("\t").Append("[").Append(recommendations.Join(str => "'" + str + "'", ", ")).Append("]")
						.Append("\t").Append(upgrade)
						.Append("\t").Append(cost)
						.Append("\t").Append(cost2)
						.Append("\t").Append(cost3);
			}
		}

		private static void MusicLoadPrefix(AudioHandler __instance) {
			// TODO debug info
			if (!runScript) {
				using (StreamWriter writer = new StreamWriter(@"D:\sorUnlocksFile.txt", false)) {
					__instance.gc.unlocks.LoadInitialUnlocks();
					StringBuilder stringBuilder = new StringBuilder();
					UnlockData.AddHeaderRow(stringBuilder);
					stringBuilder.Append("\n");
					foreach (UnlockData unlockData in __instance.gc.sessionDataBig.traitUnlocksCharacterCreation.Select(unlock => new UnlockData(unlock))) {
						unlockData.ToString(stringBuilder);
						stringBuilder.Append("\n");
					}

					stringBuilder.Append("\n\nOther traits\n");
					foreach (UnlockData unlockData in __instance.gc.sessionDataBig.unlocks
							.Where(unlock => unlock.unlockType == "Trait")
							.Select(unlock => new UnlockData(unlock))) {
						unlockData.ToString(stringBuilder);
						stringBuilder.Append("\n");
					}
					
					writer.WriteLine(stringBuilder.ToString());
				}
				runScript = true;
			}
			// ===============

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