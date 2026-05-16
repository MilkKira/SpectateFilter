using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SpectateFilterClient
{
    [BepInPlugin("com.milkkira.spectatefilter", "Spectate Filter", "1.1.0")]
	[BepInProcess("EscapeFromTarkov.exe")]
	public class SpectateFilterPlugin : BaseUnityPlugin
	{

		internal static ManualLogSource Log;
		private static Harmony _harmony;
		private static bool _patched;
		private static int _tryCount;
		private void Awake()
		{
			Log = base.Logger;
			Log.LogInfo("Spectate Filter v1.1.0");
		}
		
		private void Update()
		{
			if (_patched)
			{
				return;
			}
			_tryCount++;
			if (_tryCount % 300 == 0)
			{
				this.TryPatch();
			}
		}
		
		private void TryPatch()
		{
			try
			{
				Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name == "Fika.Core");
				if (!(assembly == null))
				{
					Type type = null;
					foreach (Type type2 in SafeGetTypes(assembly))
					{
						if (type2.Name == "FreeCamera")
						{
							type = type2;
							break;
						}
					}
					if (!(type == null))
					{
						MethodInfo methodInfo = type.GetMethod("ClearAndAddPlayers", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (methodInfo == null)
						{
							methodInfo = type.GetMethod("ClearAndAddPlayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						}
						if (methodInfo == null)
						{
							Log.LogWarning("ClearAndAddPlayers not found");
						}
						else
						{
							_harmony = new Harmony("com.milkkira.spectatefilter");
							_harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(SpectatePatch), "ClearAndAddPlayersPostfix", null), null, null, null);
							MethodInfo method = type.GetMethod("Start", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (method != null)
							{
								_harmony.Patch(method, null, new HarmonyMethod(typeof(SpectatePatch), "StartPostfix", null), null, null, null);
								Log.LogInfo("Patched Start");
							}
							else
							{
								Log.LogWarning("Start method not found");
							}
							MethodInfo method2 = type.GetMethod("OnPlayerSpawned", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (method2 != null)
							{
								_harmony.Patch(method2, new HarmonyMethod(typeof(SpectatePatch), "OnPlayerSpawnedPrefix", null), null, null, null, null);
								Log.LogInfo("Patched OnPlayerSpawned");
							}
							else
							{
								Log.LogWarning("OnPlayerSpawned not found");
							}
							MethodInfo method3 = type.GetMethod("CycleSpectatePlayers", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (method3 != null)
							{
								_harmony.Patch(method3, new HarmonyMethod(typeof(SpectatePatch), "CycleSpectatePlayersPrefix", null), null, null, null, null);
								Log.LogInfo("Patched CycleSpectatePlayers");
							}
							else
							{
								Log.LogWarning("CycleSpectatePlayers not found");
							}
							_patched = true;
							Log.LogInfo("PATCHED: Spectate Filter active (4 methods)");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.LogError(string.Format("Patch error: {0}", ex));
			}
		}
		
		
		private static IEnumerable<Type> SafeGetTypes(Assembly asm)
		{
			IEnumerable<Type> enumerable;
			try
			{
				enumerable = asm.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				enumerable = ex.Types.Where((Type t) => t != null);
			}
			catch
			{
				enumerable = Array.Empty<Type>();
			}
			return enumerable;
		}
	}
}