using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace SoR_Music_Loader.music_loader.api {
	[PublicAPI]
	public static class BTMusicLoader {
		/// <summary>
		/// Loads and adds a track synchronously
		/// </summary>
		/// 
		/// <param name="trackName">
		/// DisplayName of the track, has to be unique. (preferably prefixed with your mod's name or acronym, eg `myMod_trackName`)
		/// </param>
		/// 
		/// <param name="formatType">
		/// format of the file, e.g. MPEG for .mp3 or WAV for .wav files
		/// </param>
		/// 
		/// <param name="filePath">
		/// The path to the music-file, either absolute or relative to `[...]/BepInEx/config/blazingtwist.sor.musicloader/`
		/// You can use either `/` or System.IO.Path.DirectorySeparatorChar for the path.
		/// </param>
		/// 
		/// <returns>
		/// A Tuple of MusicLoadResult and the final AudioClip (when MusicLoadResult == Success) or null otherwise
		/// </returns>
		public static (MusicLoadResult, AudioClip) LoadMusic(string trackName, AudioType formatType, string filePath) {
			return CustomMusicManager.AddMusic(trackName, formatType, filePath);
		}

		/// <summary>
		/// Same as Music, but asynchronous. Will call the callback once execution has finished.
		/// Can be used to minimize lag when loading tracks.
		/// </summary>
		/// 
		/// <param name="trackName">
		/// DisplayName of the track, has to be unique. (preferably prefixed with your mod's name or acronym, eg `myMod_trackName`)
		/// </param>
		/// 
		/// <param name="formatType">
		/// format of the file, e.g. MPEG for .mp3 or WAV for .wav files
		/// </param>
		/// 
		/// <param name="filePath">
		/// The path to the music-file, either absolute or relative to `[...]/BepInEx/config/blazingtwist.sor.musicloader/`
		/// You can use either `/` or System.IO.Path.DirectorySeparatorChar for the path.
		/// </param>
		/// 
		/// <param name="callback">
		/// optional callback. Called once execution is finished with MusicLoadResult and the final AudioClip (when MusicLoadResult == Success) or null otherwise
		/// </param>
		/// 
		/// <returns>
		/// IEnumerator
		/// </returns>
		public static IEnumerator LoadMusicAsync(string trackName, AudioType formatType, string filePath, Action<MusicLoadResult, AudioClip> callback = null) {
			yield return CustomMusicManager.AddMusicAsync(trackName, formatType, filePath, callback);
		}

		/// <summary>
		/// Adds a track to the game
		/// can be used as an alternative to the LoadMusic/LoadMusicAsync methods if you want to load the file yourself or already have an AudioClip for other reasons
		/// </summary>
		/// 
		/// <param name="trackName">
		/// DisplayName of the track, has to be unique. (preferably prefixed with your mod's name or acronym, eg `myMod_trackName`)
		/// </param>
		/// 
		/// <param name="music">
		/// Non-Null AudioClip to play when loading the track
		/// </param>
		/// 
		/// <returns>
		/// MusicLoadResult
		/// </returns>
		public static MusicLoadResult AddMusic(string trackName, AudioClip music) {
			return CustomMusicManager.AddMusic(trackName, music);
		}

		/// <summary>
		/// Loads an mp3 file as a wav AudioClip (Unity disallows "streaming" of mp3 music)
		/// </summary>
		/// 
		/// <param name="mp3Data">
		/// The content of the file as a byte[]
		/// </param>
		/// 
		/// <returns>
		/// An AudioClip that can be played at will in-game.
		/// </returns>
		public static AudioClip Mp3ToWavClip(byte[] mp3Data) {
			return TrackLoadingUtils.Mp3ToWavClip("mp3ToWav.wav", mp3Data);
		}
	}
}