﻿using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace SoR_Music_Loader.music_loader.config {
	[PublicAPI]
	public class TrackConfig {
		public AudioType audioType;
		public string filePath;

		public string GetAbsolutePath() {
			if (filePath == null) {
				return null;
			}

			string fixedFilePath = filePath.Replace('/', Path.DirectorySeparatorChar);
			if (Path.IsPathRooted(fixedFilePath)) {
				return fixedFilePath;
			}

			return Path.GetFullPath(
					(Application.dataPath + "/../BepInEx/config/" + SorMusicLoader.pluginGuid + "/" + filePath)
					.Replace("//", "/")
					.Replace('/', Path.DirectorySeparatorChar)
			);
		}
	}
}