using Godot;
using System;

namespace Project.Core
{
	public class SaveManager : Node
	{
		public static bool saveDataInitialized; //Has the initial save data check been completed?

		public override void _EnterTree()
		{
			LoadSettings();
			LoadGame();
		}

		#region Settings
		public enum VoiceLanguage
		{
			English,
			Japanese
		}
		public enum TextLanguage
		{
			English,
			Japanese,
			German,
			Italian,
			French,
			Spanish
		}
		private enum AudioBuses
		{
			MASTER,
			BGM,
			SFX,
			VOICE,
			COUNT
		}

		public static SettingsData settings;
		public static bool UseEnglishVoices => settings.voiceLanguage == VoiceLanguage.English;

		public class SettingsData
		{
			//Video
			public bool useVsync;
			public bool isFullscreen;
			public Vector2 screenResolution;

			//Audio
			public bool isMasterMuted;
			public float masterVolume = .5f;
			public bool isBgmMuted;
			public float bgmVolume = 1f;
			public bool isSfxMuted;
			public float sfxVolume = 1f;
			public bool isVoiceMuted;
			public float voiceVolume = 1f;

			//Language
			public bool subtitlesEnabled = true;
			public VoiceLanguage voiceLanguage;
			public TextLanguage textLanguage;
		}

		public void LoadSettings()
		{
			//Attempt to load.
			if (settings == null)
				settings = new SettingsData(); //Default settings

			//TODO Attempt to load control configuration. for now, default controls
			InputManager.DefaultControls();

			//Load and apply settings
			OS.VsyncEnabled = settings.useVsync;
			//OS.WindowSize = settings.screenResolution;
			OS.WindowFullscreen = settings.isFullscreen;

			/*
			SetAudioBusVolume((int)AudioBuses.MASTER, settings.masterVolume, settings.isMasterMuted);
			SetAudioBusVolume((int)AudioBuses.BGM, settings.bgmVolume, settings.isBgmMuted);
			SetAudioBusVolume((int)AudioBuses.SFX, settings.sfxVolume, settings.isSfxMuted);
			SetAudioBusVolume((int)AudioBuses.VOICE, settings.voiceVolume, settings.isVoiceMuted);
			*/

			saveDataInitialized = true;
		}

		public void SaveSettings()
		{
			//Save data portably in the same folder as the application.
		}

		private void SetAudioBusVolume(int bus, float volume, bool forceMute = default)
		{
			bool isMuted = Mathf.IsZeroApprox(volume) || forceMute;
			AudioServer.SetBusMute(bus, isMuted); //Mute or unmute

			if (isMuted) return;

			float volumeDb = (Mathf.Log(volume) / Mathf.Log(10)) * 20; //Convert ratio to db
			AudioServer.SetBusVolumeDb(bus, volumeDb);
		}

		#endregion

		#region Game data
		public static GameData ActiveGameData { get; private set; }
		public class GameData
		{
			//public bool[] worldRingsCollected;
			public int exp;
			public int level;
			public float playTime;

			public SkillRing skillRing;
		}

		public void SaveGame()
		{

		}

		public void LoadGame()
		{
			//Debug game data
			ActiveGameData = new GameData()
			{
				level = 0,
				skillRing = new SkillRing(),
			};
		}
		#endregion

		#region Skills
		public class SkillRing
		{
			public Skills equippedSkills;

			[Flags]
			public enum Skills
			{
				//Standard skills
				None = 0,
				Susan = 1,
				PearlAttractor = 2,
				Karen = 4
			}
		}

		#endregion
	}
}