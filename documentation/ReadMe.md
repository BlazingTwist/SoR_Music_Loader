## For Modders
The default config comes with keys to replace all in-game tracks,  
however you can specify custom music by making use of either the Api-Methods or Mod-Configs

### Mod-Configs

Mod-Configs allow you to add custom music without writing your own mod.  
Custom music added this way requires a config-file in `[...]/BepInEx/config/blazingtwist.sor.musicloader/custom/`  
Config-files must have a `.cs` extension. A config-file `myCustomMusic.cs` may look like this:
```c#
- trackList :

-- "track1" :
--- audioType = MPEG
--- filePath = "C:/Music/someMusic.mp3"

-- "track2" :
--- audioType = WAV
--- filePath = "someOtherMusic.wav"
```
In this case `track1` uses an absolute path, which works as you'd expect.  
But `track2` is using a relative path. The musicLoader will look for track2 in
`[...]/blazingtwist.sor.musicloader/custom/myCustomMusic/someOtherMusic.wav`  
As such, if you want to use relative paths, you'll need to create a directory of the **same name** as your config-file.

---

### Api-Methods
Api-Methods allow your mod to take control over how music is loaded from the disk.  
Want to use a non-standard location or bundle your music in another format? You can.

For specific documentation on Api-Methods, check the `SoR_Music_Loader.music_loader.api.BTMusicLoader` class.

Some basic examples:

* `(MusicLoadResult, AudioClip) LoadMusic(string, AudioType, string)`  
    The least involved way to load and add music to the game.  
    Appropriate when you only care about using your Music's AudioClip in your mod.
    ```c#
    (MusicLoadResult status, AudioClip clip) = BTMusicLoader.LoadMusic("myMod_track1", AudioType.WAV, "C:/Music/track1.wav");
    if(status == MusicLoadResult.Success) {
        // use AudioClip to do something
    }
    ```
  
* `IEnumerator LoadMusicAsync(string, AudioType, string, Action<MusicLoadResult, AudioClip>)`  
    Equivalent to the basic `LoadMusic` method, but executes asynchronously.  
    Should be used whenever you don't need the AudioClip immediately.
    ```c#
    StartCoroutine(BTMusicLoader.LoadMusicAsync("myMod_track2", AudioType.MPEG, "track2.mp3", (status, clip) => {
        if(status == MusicLoadResult.Success) {
            // use AudioClip to do something
        }
    });
    ```
  
* `MusicLoadResult AddMusic(string, AudioClip)`  
    This allows you the most control over your music, but also means you'll have to load it yourself.  
    Useful when you already have a loaded AudioClip and want to register it as playable music.
    ```c#
    AudioClip someClip = ...
    MusicLoadResult result = BTMusicLoader.AddMusic("myMod_track3", someClip);
    if(result != MusicLoadResult.Success){
        // Something went wrong, music won't be available in game
    }
    ```
  
* `AudioClip Mp3ToWavClip(byte[])`  
    If you want to play mp3 Audio, you'll have to convert it to another format first.  
    Rather than creating your own method to do so, you can use this one.
    To get a byte[] of your file you can make use of
    Unity's `UnityWebRequest` (see `TrackLoadingUtils.LoadMusicTrack` for reference)
    or use `System.IO.File.ReadAllBytes`
    ```c#
    byte[] mp3Data = File.ReadAllBytes("C:/Music/track4.mp3");
    AudioClip clip = BTMusicLoader.Mp3ToWavClip(mp3Data);
    // use AudioClip to do something
    ```