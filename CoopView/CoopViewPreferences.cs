using BepInEx;
using System;
using System.IO;
using UnityEngine;

namespace CoopView
{
	public class CoopViewPreferences
	{
		public class PreferencesData : ScriptableObject
		{
			public PreferencesData()
			{
				secondWindowStartupResolution = OptionsManager.secondWindowStartupResolution;
				playerOneCamera = OptionsManager.playerOneCamera;
			}

			public int secondWindowStartupResolution;
			public int playerOneCamera;
		}

		private const string fileName = "CoopView_Preferences.txt"; 
		private static string filePath = null;
		private static string FilePath
		{
			get
			{
				if (filePath == null)
				{
					filePath = Path.Combine(Paths.ConfigPath, fileName);
				}
				return filePath;
			}
		}

		internal static void LoadPreferences()
		{
			try
			{
				if (File.Exists(FilePath))
				{
					string json = File.ReadAllText(FilePath);
					PreferencesData settingsData = ScriptableObject.CreateInstance<PreferencesData>();
					JsonUtility.FromJsonOverwrite(json, settingsData);
					OptionsManager.secondWindowStartupResolution = settingsData.secondWindowStartupResolution;
					OptionsManager.playerOneCamera = settingsData.playerOneCamera;
					return;
				}
				SavePreferences();
            }
            catch (Exception ex)
            {
				Debug.LogError($"Failed to load Coop View Preferences: {ex.Message}");
			}
		}

		internal static void SavePreferences()
		{
			try
			{
				string text = JsonUtility.ToJson(ScriptableObject.CreateInstance<PreferencesData>(), true);
				File.WriteAllText(FilePath, text);
			}
			catch (Exception ex)
            {
				Debug.LogError($"Failed to save Coop View Preferences: {ex.Message}");
			}
		}
	}
}
