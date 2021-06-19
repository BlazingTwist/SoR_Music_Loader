using System;
using System.Collections.Generic;
using System.Linq;
using BlazingTwistConfigTools.blazingtwist.config;
using SoR_Music_Loader.asm;
using SoR_Music_Loader.music_loader.config;
using UnityEngine;

namespace SoR_Music_Loader.music_loader {
	internal class TrackConfigManager : MonoBehaviour {
		internal static TrackConfigManager instance;
		internal static TracksConfig trackConfig;

		private static GameObject targetParent;
		private static float nextConfigCheckIn;
		private static string configPath;

		internal static void AttachToScene() {
			if (targetParent != null) {
				return;
			}

			try {
				targetParent = new GameObject("BTTrackConfigManagerObject");
				targetParent.AddComponent<TrackConfigManager>();
				DontDestroyOnLoad(targetParent);
			} catch (Exception e) {
				Debug.LogError("Attaching (Music-) TrackManager failed due to exception: " + e);
			}
		}

		private void Awake() {
			instance = this;
			configPath = Application.dataPath + "/../BepInEx/config/" + SorMusicLoader.pluginGuid + ".cs";
			ReloadConfig();
		}

		private void LateUpdate() {
			nextConfigCheckIn -= Time.unscaledDeltaTime;
			if (nextConfigCheckIn <= 0) {
				ReloadConfig();
			}
		}

		private static void ReloadConfig() {
			if (trackConfig == null) {
				trackConfig = BTConfigUtils.LoadConfigFile(configPath, trackConfig);

				if (trackConfig != null) {
					// on first load every track is "new"
					IEnumerable<KeyValuePair<string, TrackConfig>>
							newTracks = trackConfig.trackList.Where(entry => !string.IsNullOrEmpty(entry.Value.filePath));
					foreach ((string key, TrackConfig value) in newTracks) {
						AudioHandlerPatcher.OnTrackAddition(key, value);
					}
				}
			} else {
				// otherwise memorize current tracks and call events selectively
				Dictionary<string, string> previousData = trackConfig.trackList
						.ToDictionary(entry => entry.Key, entry => entry.Value.filePath);
				trackConfig = BTConfigUtils.LoadConfigFile(configPath, trackConfig);

				if (trackConfig != null) {
					IEnumerable<KeyValuePair<string, TrackConfig>> newTracks = trackConfig.trackList
							.Where(entry => !previousData.ContainsKey(entry.Key) && !string.IsNullOrEmpty(entry.Value.filePath));
					foreach ((string key, TrackConfig value) in newTracks) {
						AudioHandlerPatcher.OnTrackAddition(key, value);
					}

					IEnumerable<KeyValuePair<string, TrackConfig>> tracksWithChangedFilePath = trackConfig.trackList
							.Where(entry => previousData.ContainsKey(entry.Key)
									&& !string.IsNullOrEmpty(entry.Value.filePath)
									&& !previousData[entry.Key].Equals(entry.Value.filePath));
					foreach ((string key, TrackConfig value) in tracksWithChangedFilePath) {
						AudioHandlerPatcher.OnTrackChanged(key, value);
					}
				}
			}

			nextConfigCheckIn = 30f;
		}
	}
}