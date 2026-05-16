using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SpectateFilterClient
{
	public static class SpectatePatch
	{
		private static bool IsBot(object player)
		{
			if (player == null)
			{
				return false;
			}
			bool flag = false;
			PropertyInfo property = player.GetType().GetProperty("IsAI");
			if (property != null)
			{
				try
				{
					flag = (bool)property.GetValue(player);
				}
				catch
				{
				}
			}
			FieldInfo field = player.GetType().GetField("IsObservedAI", BindingFlags.Instance | BindingFlags.Public);
			if (field != null)
			{
				try
				{
					flag = flag || (bool)field.GetValue(player);
				}
				catch
				{
				}
			}
			return flag;
		}
		
		private static bool IsBotListPlayer(object listPlayer)
		{
			if (listPlayer == null)
			{
				return false;
			}
			FieldInfo field = listPlayer.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
			return !(field == null) && SpectatePatch.IsBot(field.GetValue(listPlayer));
		}
		
		public static void ClearAndAddPlayersPostfix(object __instance)
		{
			try
			{
				FieldInfo field = __instance.GetType().GetField("_players", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(field == null))
				{
					IList list = field.GetValue(__instance) as IList;
					if (list != null)
					{
						int num = 0;
						for (int i = list.Count - 1; i >= 0; i--)
						{
							if (SpectatePatch.IsBot(list[i]))
							{
								list.RemoveAt(i);
								num++;
							}
						}
						SpectateFilterPlugin.Log.LogInfo(string.Format("ClearAndAddPlayersPostfix: removed {0} bots, remaining {1}", num, list.Count));
					}
				}
			}
			catch (Exception ex)
			{
				SpectateFilterPlugin.Log.LogError(string.Format("CAPostfix err: {0}", ex));
			}
		}
		
		public static void StartPostfix(object __instance)
		{
			SpectateFilterPlugin.Log.LogInfo("StartPostfix FIRED");
			try
			{
				FieldInfo field = __instance.GetType().GetField("_playersTracker", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field == null)
				{
					SpectateFilterPlugin.Log.LogWarning("StartPostfix: _playersTracker not found");
				}
				else
				{
					IDictionary dictionary = field.GetValue(__instance) as IDictionary;
					if (dictionary != null)
					{
						int num = 0;
						List<object> list = new List<object>();
						foreach (object obj in dictionary)
						{
							DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
							if (SpectatePatch.IsBotListPlayer(dictionaryEntry.Value))
							{
								list.Add(dictionaryEntry.Key);
								num++;
							}
						}
						foreach (object obj2 in list)
						{
							dictionary.Remove(obj2);
						}
						SpectateFilterPlugin.Log.LogInfo(string.Format("StartPostfix: removed {0} bots from tracker, remaining {1}", num, dictionary.Count));
					}
				}
			}
			catch (Exception ex)
			{
				SpectateFilterPlugin.Log.LogError(string.Format("StartPostfix err: {0}", ex));
			}
		}
		
		public static bool OnPlayerSpawnedPrefix(object player)
		{
			if (SpectatePatch.IsBot(player))
			{
				SpectateFilterPlugin.Log.LogInfo("OnPlayerSpawnedPrefix: blocked bot");
				return false;
			}
			return true;
		}
		
		public static void CycleSpectatePlayersPrefix(object __instance)
		{
			SpectateFilterPlugin.Log.LogInfo("CycleSpectatePlayersPrefix FIRED");
			try
			{
				FieldInfo field = __instance.GetType().GetField("_playersTracker", BindingFlags.Instance | BindingFlags.NonPublic);
				if (!(field == null))
				{
					IDictionary dictionary = field.GetValue(__instance) as IDictionary;
					if (dictionary != null)
					{
						int num = 0;
						List<object> list = new List<object>();
						foreach (object obj in dictionary)
						{
							DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
							if (SpectatePatch.IsBotListPlayer(dictionaryEntry.Value))
							{
								list.Add(dictionaryEntry.Key);
								num++;
							}
						}
						foreach (object obj2 in list)
						{
							dictionary.Remove(obj2);
						}
						SpectateFilterPlugin.Log.LogInfo(string.Format("CyclePrefix: removed {0} bots from tracker, remaining {1}", num, dictionary.Count));
					}
				}
			}
			catch (Exception ex)
			{
				SpectateFilterPlugin.Log.LogError(string.Format("CyclePrefix err: {0}", ex));
			}
		}
		
		public static bool DetachCameraPrefix(bool force)
		{
			if (force)
			{
				return true;
			}
			SpectateFilterPlugin.Log.LogInfo("DetachCameraPrefix: blocked free camera detach key");
			return false;
		}
	}
}
