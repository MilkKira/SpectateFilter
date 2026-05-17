using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SpectateFilterClient
{
	public static class SpectatePatch
	{
		private static DateTime _lastFreeCameraBlockLog = DateTime.MinValue;
		
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
		
		private static object GetListPlayer(object listPlayer)
		{
			if (listPlayer == null)
			{
				return null;
			}
			FieldInfo field = listPlayer.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
			return field == null ? null : field.GetValue(listPlayer);
		}
		
		private static bool HasSpectatableHuman(object __instance)
		{
			if (__instance == null)
			{
				return false;
			}
			FieldInfo playersField = __instance.GetType().GetField("_players", BindingFlags.Instance | BindingFlags.NonPublic);
			IList players = playersField == null ? null : playersField.GetValue(__instance) as IList;
			if (players != null)
			{
				foreach (object player in players)
				{
					if (!SpectatePatch.IsBot(player))
					{
						return true;
					}
				}
			}
			FieldInfo trackerField = __instance.GetType().GetField("_playersTracker", BindingFlags.Instance | BindingFlags.NonPublic);
			IDictionary tracker = trackerField == null ? null : trackerField.GetValue(__instance) as IDictionary;
			if (tracker != null)
			{
				foreach (object obj in tracker)
				{
					DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
					object player = SpectatePatch.GetListPlayer(dictionaryEntry.Value);
					if (player != null && !SpectatePatch.IsBot(player))
					{
						return true;
					}
				}
			}
			return false;
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
						if (num > 0)
						{
							SpectateFilterPlugin.Log.LogDebug(string.Format("ClearAndAddPlayersPostfix: removed {0} bots, remaining {1}", num, list.Count));
						}
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
						if (num > 0)
						{
							SpectateFilterPlugin.Log.LogDebug(string.Format("StartPostfix: removed {0} bots from tracker, remaining {1}", num, dictionary.Count));
						}
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
				return false;
			}
			return true;
		}
		
		public static void CycleSpectatePlayersPrefix(object __instance)
		{
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
						if (num > 0)
						{
							SpectateFilterPlugin.Log.LogDebug(string.Format("CyclePrefix: removed {0} bots from tracker, remaining {1}", num, dictionary.Count));
						}
					}
				}
			}
			catch (Exception ex)
			{
				SpectateFilterPlugin.Log.LogError(string.Format("CyclePrefix err: {0}", ex));
			}
		}
		
		public static bool DetachCameraPrefix(object __instance, object[] __args)
		{
			try
			{
				if (__args != null && __args.Length > 0 && __args[0] is bool && (bool)__args[0])
				{
					return true;
				}
				if (!SpectatePatch.HasSpectatableHuman(__instance))
				{
					return true;
				}
				DateTime now = DateTime.UtcNow;
				if ((now - _lastFreeCameraBlockLog).TotalSeconds >= 3.0)
				{
					_lastFreeCameraBlockLog = now;
					SpectateFilterPlugin.Log.LogDebug("DetachCameraPrefix: blocked free camera because spectatable teammate exists");
				}
				return false;
			}
			catch (Exception ex)
			{
				SpectateFilterPlugin.Log.LogError(string.Format("DetachCameraPrefix err: {0}", ex));
				return true;
			}
		}
	}
}
