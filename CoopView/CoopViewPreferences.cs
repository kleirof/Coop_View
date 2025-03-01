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

		internal static void LoadPreferences()
		{
			if (File.Exists(Path.Combine(ETGMod.ResourcesDirectory, fileName)))
			{
				string json = File.ReadAllText(Path.Combine(ETGMod.ResourcesDirectory, fileName));
				PreferencesData settingsData = ScriptableObject.CreateInstance<PreferencesData>();
				JsonUtility.FromJsonOverwrite(json, settingsData);
				OptionsManager.secondWindowStartupResolution = settingsData.secondWindowStartupResolution;
				OptionsManager.playerOneCamera = settingsData.playerOneCamera;
				return;
			}
			SavePreferences();
		}

		internal static void SavePreferences()
		{
			string text = JsonUtility.ToJson(ScriptableObject.CreateInstance<PreferencesData>(), true);
			if (File.Exists(Path.Combine(ETGMod.ResourcesDirectory, fileName)))
			{
				File.Delete(Path.Combine(ETGMod.ResourcesDirectory, fileName));
			}
			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(ETGMod.ResourcesDirectory, fileName), true))
			{
				streamWriter.WriteLine(text);
			}
		}
	}
}
