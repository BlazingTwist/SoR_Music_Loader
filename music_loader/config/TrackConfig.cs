using System.IO;
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

			if (Path.IsPathRooted(filePath)) {
				return filePath.Replace('/', Path.DirectorySeparatorChar);
			}

			return Path.GetFullPath(
					(Application.dataPath + "/../BepInEx/config/" + SorMusicLoader.pluginGuid + "/" + filePath)
					.Replace('/', Path.DirectorySeparatorChar)
			);
		}
	}
}