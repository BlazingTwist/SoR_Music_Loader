﻿using System.Collections.Generic;

// src: https://answers.unity.com/questions/737002/wav-byte-to-audioclip.html
namespace SoR_Music_Loader.music_loader {
	public class WAVConverter {
		// convert two bytes to one float in the range -1 to 1
		private static float bytesToFloat(byte firstByte, byte secondByte) {
			// convert two bytes to one short (little endian)
			short s = (short) ((secondByte << 8) | firstByte);
			// convert to range from -1 to (just below) 1
			return s / 32768.0F;
		}

		private static int bytesToInt(IReadOnlyList<byte> bytes, int offset = 0) {
			int value = 0;
			for (int i = 0; i < 4; i++) {
				value |= bytes[offset + i] << (i * 8);
			}

			return value;
		}

		// properties
		public float[] LeftChannel { get; }
		public float[] RightChannel { get; }
		public int ChannelCount { get; }
		public int SampleCount { get; }
		public int Frequency { get; }

		public WAVConverter(IReadOnlyList<byte> wav) {
			// Determine if mono or stereo
			ChannelCount = wav[22]; // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

			// Get the frequency
			Frequency = bytesToInt(wav, 24);

			// Get past all the other sub chunks to get to the data sub-chunk:
			int pos = 12; // First sub-chunk ID from 12 to 16

			// Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
			while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97)) {
				pos += 4;
				int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
				pos += 4 + chunkSize;
			}

			pos += 8;

			// Pos is now positioned to start of actual sound data.
			SampleCount = (wav.Count - pos) / 2; // 2 bytes per sample (16 bit sound mono)
			if (ChannelCount == 2) {
				SampleCount /= 2; // 4 bytes per sample (16 bit stereo)
			}

			// Allocate memory (right will be null if only mono sound)
			LeftChannel = new float[SampleCount];
			RightChannel = ChannelCount == 2 ? new float[SampleCount] : null;

			// Write to double array/s:
			int i = 0;
			while (pos < wav.Count) {
				LeftChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
				pos += 2;
				if (ChannelCount == 2) {
					RightChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
					pos += 2;
				}

				i++;
			}
		}

		public override string ToString() {
			return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel,
					ChannelCount, SampleCount, Frequency);
		}
	}
}