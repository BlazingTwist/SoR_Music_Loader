using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlazingTwistConfigTools.blazingtwist.config;
using SoR_Music_Loader.asm;
using SoR_Music_Loader.music_loader.config;
using UnityEngine;

namespace SoR_Music_Loader.music_loader {
	internal class CustomMusicManager : MonoBehaviour {
		internal static string configBasePath;

		private static GameObject targetParent;
		private static float nextConfigCheckIn;

		private static readonly Dictionary<string, TracksConfig> customTrackConfigs = new Dictionary<string, TracksConfig>();
		private static readonly Dictionary<string, AudioClip> customMusic = new Dictionary<string, AudioClip>();

		internal static void AttachToScene() {
			if (targetParent != null) {
				return;
			}

			try {
				targetParent = new GameObject("BTCustomMusicManagerObject");
				targetParent.AddComponent<CustomMusicManager>();
				DontDestroyOnLoad(targetParent);
			} catch (Exception e) {
				Debug.LogError("Attaching CustomMusicManager failed due to exception: " + e);
			}
		}

		private void Awake() {
			configBasePath = Application.dataPath + "/../BepInEx/config/" + SorMusicLoader.pluginGuid + "/custom/";
			FirstLoadConfig();
		}

		private void LateUpdate() {
			nextConfigCheckIn -= Time.unscaledDeltaTime;
			if (nextConfigCheckIn <= 0) {
				ReloadConfig();
			}
		}

		private static IEnumerable<string> FindCustomMusicConfigs() {
			string[] configFiles = Directory.GetFiles(configBasePath, "*.cs", SearchOption.TopDirectoryOnly);
			return configFiles.Select(Path.GetFileNameWithoutExtension);
		}

		private static void HandleTrackAddition(string trackName, string modDirName, TrackConfig config) {
			if (!IsTrackNameAvailable(modDirName + "_" + trackName)) {
				Debug.LogError("unable to load track: " + trackName + " from config: " + modDirName + "! TrackName is already in use.");
				return;
			}

			AudioHandlerPatcher.OnCustomTrackAddition(new CustomMusicLoadSpec {
					trackName = trackName,
					modDirName = modDirName,
					audioType = config.audioType,
					filePath = config.filePath,
					callback = OnCustomTrackLoaded
			});
		}

		private static void HandleTrackChanged(string trackName, string modDirName, TrackConfig config) {
			if (IsTrackNameAvailable(modDirName + "_" + trackName)) {
				Debug.LogWarning("detected track-change in config for mod: " + modDirName + " track: " + trackName +
						" but track didn't previously (mis-detected addition as change?)");
			}

			AudioHandlerPatcher.OnCustomTrackChanged(new CustomMusicLoadSpec {
					trackName = trackName,
					modDirName = modDirName,
					audioType = config.audioType,
					filePath = config.filePath,
					callback = OnCustomTrackLoaded
			});
		}

		private static void FirstLoadConfig() {
			foreach (string modDirName in FindCustomMusicConfigs()) {
				TracksConfig config = BTConfigUtils.LoadConfigFile(configBasePath + modDirName + ".cs", (TracksConfig) null);
				if (config == null) {
					continue;
				}

				customTrackConfigs[modDirName] = config;
				foreach ((string trackName, TrackConfig trackConfig) in config.trackList.Where(entry => !string.IsNullOrEmpty(entry.Value.filePath))) {
					HandleTrackAddition(trackName, modDirName, trackConfig);
				}
			}

			nextConfigCheckIn = 30f;
		}

		private static void ReloadConfig() {
			// store current config to identify changes
			// as mapping of modName->trackName->filePath
			Dictionary<string, Dictionary<string, string>> previousConfig = customTrackConfigs
					.ToDictionary(
							modConfig => modConfig.Key,
							modConfig => modConfig.Value.trackList
									.ToDictionary(track => track.Key, track => track.Value.filePath)
					);

			foreach (string modDirName in FindCustomMusicConfigs()) {
				Dictionary<string, string> previousModData = previousConfig.ContainsKey(modDirName) ? previousConfig[modDirName] : null;
				TracksConfig config = BTConfigUtils.LoadConfigFile(configBasePath + modDirName + ".cs", (TracksConfig) null);
				if (config == null) {
					continue;
				}

				customTrackConfigs[modDirName] = config;
				foreach ((string trackName, TrackConfig trackConfig) in config.trackList.Where(entry => !string.IsNullOrEmpty(entry.Value.filePath))) {
					if (previousModData == null || !previousModData.ContainsKey(trackName)) {
						// new track
						HandleTrackAddition(trackName, modDirName, trackConfig);
					} else if (!previousModData[trackName].Equals(trackConfig.filePath)) {
						// track changed filePath
						HandleTrackChanged(trackName, modDirName, trackConfig);
					}
				}
			}

			nextConfigCheckIn = 30f;
		}

		private static void OnCustomTrackLoaded(CustomMusicLoadSpec loadSpec, AudioClip clip) {
			string trackName = loadSpec.modDirName + "_" + loadSpec.trackName;

			if (clip == null) {
				customMusic.Remove(trackName);
			} else {
				customMusic[trackName] = clip;
			}
		}

		private static bool IsTrackNameAvailable(string trackName) {
			return !customMusic.ContainsKey(trackName);
		}

		internal static (MusicLoadResult, AudioClip) AddMusic(string trackName, AudioType formatType, string filePath) {
			if (!IsTrackNameAvailable(trackName)) {
				return (MusicLoadResult.DuplicateTrackName, null);
			}

			var trackConfig = new TrackConfig {
					audioType = formatType,
					filePath = filePath
			};
			if (!File.Exists(trackConfig.GetAbsolutePath())) {
				return (MusicLoadResult.FileNotFound, null);
			}

			AudioClip clip = TrackLoadingUtils.LoadMusicTrackNow(trackConfig);
			return clip == null ? (MusicLoadResult.UnknownError, null) : (MusicLoadResult.Success, clip);
		}

		internal static IEnumerator AddMusicAsync(string trackName, AudioType formatType, string filePath, Action<MusicLoadResult, AudioClip> callback = null) {
			if (!IsTrackNameAvailable(trackName)) {
				callback?.Invoke(MusicLoadResult.DuplicateTrackName, null);
				yield break;
			}
			
			var trackConfig = new TrackConfig {
					audioType = formatType,
					filePath = filePath
			};
			if (!File.Exists(trackConfig.GetAbsolutePath())) {
				callback?.Invoke(MusicLoadResult.FileNotFound, null);
				yield break;
			}

			yield return TrackLoadingUtils.LoadMusicTrack(trackConfig, clip => {
				callback?.Invoke(clip == null ? MusicLoadResult.UnknownError : MusicLoadResult.Success, clip);
			});
		}

		internal static MusicLoadResult AddMusic(string trackName, AudioClip clip) {
			if (clip == null) {
				throw new ArgumentNullException(nameof(clip));
			}
			
			if (!IsTrackNameAvailable(trackName)) {
				return MusicLoadResult.DuplicateTrackName;
			}

			customMusic[trackName] = clip;
			AudioHandlerPatcher.OnClipAddition(trackName, clip);
			return MusicLoadResult.Success;
		}

		internal static Dictionary<string, AudioClip> GetCustomMusic() {
			return customMusic;
		}
	}
}