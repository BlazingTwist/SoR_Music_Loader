using System;
using System.IO;
using UnityEngine;

namespace SoR_Music_Loader.music_loader {
	internal class CustomMusicLoadSpec {
		public string trackName;
		public string modDirName;
		public AudioType audioType;
		public string filePath;
		public Action<CustomMusicLoadSpec, AudioClip> callback;

		public string GetDisplayName() {
			return modDirName + "_" + trackName;
		}
		
		public string GetAbsolutePath() {
			if (filePath == null) {
				return null;
			}

			string fixedFilePath = filePath.Replace('/', Path.DirectorySeparatorChar);
			if (Path.IsPathRooted(fixedFilePath)) {
				return fixedFilePath;
			}

			return Path.GetFullPath(
					(CustomMusicManager.configBasePath + modDirName + "/" + filePath)
					.Replace("//", "/")
					.Replace('/', Path.DirectorySeparatorChar)
			);
		}

		public override string ToString() {
			return "CustomMusicLoadSpec{" + GetDisplayName() + " | type: " + audioType + " | path: " + filePath + "}";
		}
	}
}