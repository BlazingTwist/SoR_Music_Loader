using System;
using System.Collections;
using System.IO;
using System.Threading;
using SoR_Music_Loader.music_loader.config;
using UnityEngine;
using UnityEngine.Networking;
using NAudio.Wave;

namespace SoR_Music_Loader.music_loader {
	internal static class TrackLoadingUtils {
		internal static AudioClip LoadMusicTrackNow(TrackConfig trackConfig) {
			AudioClip audioClip = null;
			string path = trackConfig.GetAbsolutePath();

			if (!File.Exists(path)) {
				Debug.LogError("Unable to load track at path: " + path + "! File does not exist.");
				return null;
			}

			UnityWebRequest webRequest = trackConfig.audioType == AudioType.MPEG
					? UnityWebRequest.Get(path)
					: UnityWebRequestMultimedia.GetAudioClip(path, trackConfig.audioType);

			webRequest.SendWebRequest();
			while (!webRequest.isDone) {
				Thread.Sleep(50);
			}

			if (webRequest.isNetworkError || webRequest.isHttpError) {
				Debug.LogError("Failed to load musicTrack at: " + path);
			} else {
				audioClip = trackConfig.audioType == AudioType.MPEG
						? Mp3ToWavClip(Path.GetFileName(path) + ".wav", webRequest.downloadHandler.data)
						: DownloadHandlerAudioClip.GetContent(webRequest);

				webRequest.Dispose();
			}

			return audioClip;
		}

		internal static IEnumerator LoadMusicTrack(TrackConfig trackConfig, Action<AudioClip> callback) {
			AudioClip audioClip = null;
			string path = trackConfig.GetAbsolutePath();

			if (!File.Exists(path)) {
				Debug.LogError("Unable to load track at path: " + path + "! File does not exist.");
				callback?.Invoke(null);
				yield break;
			}

			UnityWebRequest webRequest = trackConfig.audioType == AudioType.MPEG
					? UnityWebRequest.Get(path)
					: UnityWebRequestMultimedia.GetAudioClip(path, trackConfig.audioType);

			webRequest.SendWebRequest();
			while (!webRequest.isDone) {
				yield return new WaitForSecondsRealtime(0.05f);
			}

			if (webRequest.isNetworkError || webRequest.isHttpError) {
				Debug.LogError("Failed to load musicTrack at: " + path);
			} else {
				audioClip = trackConfig.audioType == AudioType.MPEG
						? Mp3ToWavClip(Path.GetFileName(path) + ".wav", webRequest.downloadHandler.data)
						: DownloadHandlerAudioClip.GetContent(webRequest);

				webRequest.Dispose();
			}

			callback.Invoke(audioClip);
		}

		internal static IEnumerator LoadMusicTrack(CustomMusicLoadSpec loadSpec) {
			AudioClip audioClip = null;
			string path = loadSpec.GetAbsolutePath();

			if (!File.Exists(path)) {
				Debug.LogError("Unable to load track at path: " + path + "! File does not exist.");
				loadSpec.callback?.Invoke(loadSpec, null);
				yield break;
			}

			UnityWebRequest webRequest = loadSpec.audioType == AudioType.MPEG
					? UnityWebRequest.Get(path)
					: UnityWebRequestMultimedia.GetAudioClip(path, loadSpec.audioType);

			webRequest.SendWebRequest();
			while (!webRequest.isDone) {
				yield return new WaitForSecondsRealtime(0.05f);
			}

			if (webRequest.isNetworkError || webRequest.isHttpError) {
				Debug.LogError("Failed to load musicTrack at: " + path);
			} else {
				audioClip = loadSpec.audioType == AudioType.MPEG
						? Mp3ToWavClip(Path.GetFileName(path) + ".wav", webRequest.downloadHandler.data)
						: DownloadHandlerAudioClip.GetContent(webRequest);

				webRequest.Dispose();
			}

			loadSpec.callback?.Invoke(loadSpec, audioClip);
		}

		internal static AudioClip Mp3ToWavClip(string clipName, byte[] mp3Data) {
			var mp3Stream = new MemoryStream(mp3Data);
			var mp3Reader = new Mp3FileReader(mp3Stream);
			WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
			var wav = new WAVConverter(AudioMemoryStream(waveStream).ToArray());

			AudioClip audioClip;
			if (wav.ChannelCount == 1 || wav.ChannelCount != 2) {
				audioClip = AudioClip.Create(clipName, wav.SampleCount, 1, wav.Frequency, false);
				audioClip.SetData(wav.LeftChannel, 0);
			} else {
				// for stereo, left/right channels have to be interleaved
				int sampleCount = wav.SampleCount;
				audioClip = AudioClip.Create(clipName, sampleCount, 2, wav.Frequency, false);
				float[] stereoData = new float[sampleCount * 2];
				float[] leftData = wav.LeftChannel;
				float[] rightData = wav.RightChannel;
				for (int i = 0; i < sampleCount; i++) {
					stereoData[i * 2] = leftData[i];
					stereoData[i * 2 + 1] = rightData[i];
				}

				audioClip.SetData(stereoData, 0);
			}

			return audioClip;
		}

		private static MemoryStream AudioMemoryStream(WaveStream waveStream) {
			var outputStream = new MemoryStream();
			using (var waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat)) {
				byte[] bytes = new byte[waveStream.Length];
				waveStream.Position = 0;
				waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
				waveFileWriter.Write(bytes, 0, bytes.Length);
				waveFileWriter.Flush();
			}

			return outputStream;
		}
	}
}