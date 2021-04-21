using System.Collections.Generic;
using JetBrains.Annotations;

namespace SoR_Music_Loader.music_loader.config {
	[PublicAPI]
	public class TracksConfig {
		public Dictionary<string, TrackConfig> trackList = new Dictionary<string, TrackConfig>();
	}
}