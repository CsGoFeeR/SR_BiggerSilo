using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using PluginManager.Plugin;
using PlayHooky;

namespace BiggerSilo {

	/// <summary>Hooks for SiloStorage</summary>
	public static class SiloStorageHooks {

		//handle to private method SiloStorage.OnAdded.
		private static MethodInfo onAddedHandle = null;

		/// <summary>Static constructor, gets reflection handles at start.</summary>
		static SiloStorageHooks() {

			//Get handle to private method OnAdded, so we stick to vanilla's requirement for achievements (full siloa achievement requires 100 items).
			onAddedHandle = typeof(SiloStorage).GetMethod("OnAdded", BindingFlags.Instance | BindingFlags.NonPublic);

		}

		/// <summary>Adds the given item to the given slot, if there is enough room.</summary>
		/// <param name="this">The instance of SiloStorage this method is hooking.</param>
		/// <param name="identifiable">Identifier object of the item being added.</param>
		/// <param name="slotIdx">ID of silo slot this item is maybe added to.</param>
		/// <returns>True if this object was added, false otherwise.</returns>
		public static bool MaybeAddIdentifiable(SiloStorage @this, Identifiable identifiable, int slotIdx) {

			//Attempts to add the item to the silo slot.
			bool result = @this.GetRelevantAmmo().MaybeAddToSpecificSlot(identifiable, slotIdx, BiggerSilo.Config.maxItemSlots);

			//achievement trigger
			OnAdded(@this);

			//returns true if the item was added, false otherwise
			return result;

		}

		/// <summary>Adds the given item to the given slot, if there is enough room.</summary>
		/// <param name="this">The instance of SiloStorage this method is hooking.</param>
		/// <param name="id">Identifier ID of the item being added.</param>
		/// <param name="identifiable">Identifier object of the silo slot.</param>
		/// <returns>True if this object was added, false otherwise.</returns>
		public static bool MaybeAddIdentifiable(SiloStorage @this, Identifiable.Id id, Identifiable identifiable) {

			//Attempts to add the item to the silo slot.
			bool result = @this.GetRelevantAmmo().MaybeAddToSlot(id, identifiable, BiggerSilo.Config.maxItemSlots);

			//achievement trigger
			OnAdded(@this);

			//returns true if the item was added, false otherwise
			return result;

		}

		/// <summary>Checks if this Silo has enough room to add the given slot.</summary>
		/// <param name="this">The instance of SiloStorage this method is hooking.</param>
		/// <param name="id">ID of the Silo slot.</param>
		/// <returns>True if there is room, false otherwise.</returns>
		public static bool CanAccept(SiloStorage @this, Identifiable.Id id) => @this.GetRelevantAmmo().CouldAddToSlot(id, BiggerSilo.Config.maxItemSlots);

		/// <summary>Calls the private method SiloStorage.OnAdded.</summary>
		/// <param name="this">The instance of SiloStorage this method is hooking.</param>
		private static void OnAdded(SiloStorage @this) => onAddedHandle.Invoke(@this, null);

	}

	/// <summary>Hooks for SiloSlotUI.</summary>
	public static class SiloSlotUIHooks {

		//Sure, we could get these from SiloSlutUI with reflection, but we'd lose FPS because it would be done every single frame.
		private static LookupDirector lookupDir = null;

		/// <summary>Adjusts the UI rendering to account for value of <see cref="Configuration.maxItemSlots"/>.</summary>
		/// <param name="this">The instance of SiloSlotUI this method is hooking.</param>
		public static void Update(SiloSlotUI @this) {

			//set lookupDir only once (only if it's null)
			lookupDir = lookupDir ?? SRSingleton<GameContext>.Instance.LookupDirector;

			//this has to be done here, because we don't have access to the private field
			//We can't just save it, because there will be many SiloSlotUIs, and they don't all share the same SiloStorage
			SiloStorage storage = @this.GetComponentInParent<SiloStorage>();

			//Get the slot ID for the Silo slot this screen is on
			Identifiable.Id slotIdentifiable = storage.GetSlotIdentifiable(@this.slotIdx);

			//check if there is any item in this slot
			if (slotIdentifiable != Identifiable.Id.NONE) {

				//do render stuff
				@this.slotIcon.sprite = lookupDir.GetIcon(slotIdentifiable);
				@this.slotIcon.enabled = true;

				//rather than scaling our current item count, we just change the max slots
				@this.bar.maxValue = BiggerSilo.Config.maxItemSlots;
				@this.bar.currValue = storage.GetSlotCount(@this.slotIdx);

				@this.bar.barColor = lookupDir.GetColor(slotIdentifiable);
				@this.frontFrameIcon.sprite = @this.frontFilled;
				@this.backFrameIcon.sprite = @this.backFilled;

			} else {

				//do more render stuff
				@this.slotIcon.enabled = false;
				@this.bar.currValue = 0f;
				@this.bar.barColor = Color.black;
				@this.frontFrameIcon.sprite = @this.frontEmpty;
				@this.backFrameIcon.sprite = @this.backEmpty;

			}

		}

	}

	/// <summary>Configuration for this plugin.</summary>
	[Serializable]
	public class Configuration {

		/// <summary>Maximum number of items allowed in a single silo storage slot.</summary>
		public int maxItemSlots = 999;

	}

	/// <summary>MonoBehaviour that is initialized when the plugin is loaded.</summary>
	[OnGameInit]
	public class BiggerSilo : MonoBehaviour {

		/// <summary>Configuration loaded from BiggerSilo.json, this is really ugly but pretty much the least ugly way we can do it.</summary>
		public static Configuration Config { get; set; }

		private Configuration LoadConfig() {

			//loaded or created config
			Configuration config = null;

			//if true, we save the config before returning
			bool saveConfig = false;

			//path to config file, game root directory + "plugins" + config file name. Plugin folder will always be called Plugin.
			//NOTE: Relative paths won't work here.
			string fullConfigPath = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine("Plugins", "BiggerSilo.json"));

			try {

				//check if config file exists
				if (File.Exists(fullConfigPath)) {

					//load config
					config = JsonUtility.FromJson<Configuration>(File.ReadAllText(fullConfigPath));

					//validate max item slots range
					if (config.maxItemSlots <= 0 || config.maxItemSlots > float.MaxValue) {

						//reset config
						print($"Configuration maxItemSlots is outside acceptable range. Resetting configuration to defaults.");
						saveConfig = true;

					}

				} else {

					//save config
					print("Configuration does not exist. Creating...");
					saveConfig = true;

				}

				//check if we should save the config
				if (saveConfig) {

					//create default config
					config = new Configuration();

					//save the config, if any error happens here it's a unity issue
					File.WriteAllText(fullConfigPath, JsonUtility.ToJson(config, true));

				}

			} catch (ArgumentException e) {

				//check if config was parsed okay. JsonUtility.FromJson throws ArgumentException on syntax error. It doesn't make any sense, but whatever.
				//In all likelyhood (zero exception documentation for Unity methods), ToJson will throw this as well on a problem, but it will be a Unity issue if so
				print("ERROR: Unable to parse config: " + e.Message);

			} catch (IOException e) {

				//error with either File.WriteAllText/ReadAllText -- user error most likely, permissions, etc.
				print($"ERROR: Unable to access {fullConfigPath}: {e.Message}");

			}

			return config;

		}

		/// <summary>Called when the game is being initialized.</summary>
		public void Awake() {

			//load or create config
			Config = LoadConfig();

			//Create hook manager
			HookManager hookManager = new HookManager();

			//get types (SiloStorage, SiloStorageHooks)
			Type siloStorageType = typeof(SiloStorage);
			Type siloStorageHooksType = typeof(SiloStorageHooks);

			//SiloStorage.MaybeAddIdentifiable(Identifiable, int) prototype
			Type[] one = new Type[] { typeof(Identifiable), typeof(int) };

			//SiloStorageHooks.MaybeAddIdentifiable(SiloStorage, Identifiable, int) prototype
			Type[] oneHook = new Type[] { typeof(SiloStorage), typeof(Identifiable), typeof(int) };

			//SiloStorage.MaybeAddIdentifiable(Identifiable.Id, Identifiable) prototype
			Type[] two = new Type[] { typeof(Identifiable.Id), typeof(Identifiable) };

			//SiloStorage.MaybeAddIdentifiable(SiloStorage, Identifiable.Id, Identifiable) prototype
			Type[] twoHook = new Type[] { typeof(SiloStorage), typeof(Identifiable.Id), typeof(Identifiable) };

			//hook SiloStorage methods
			hookManager.Hook(siloStorageType.GetMethod("MaybeAddIdentifiable", one), siloStorageHooksType.GetMethod("MaybeAddIdentifiable", oneHook));
			hookManager.Hook(siloStorageType.GetMethod("MaybeAddIdentifiable", two), siloStorageHooksType.GetMethod("MaybeAddIdentifiable", twoHook));
			hookManager.Hook(siloStorageType.GetMethod("CanAccept"), siloStorageHooksType.GetMethod("CanAccept"));

			//hook SiloSlotUI methods
			hookManager.Hook(typeof(SiloSlotUI).GetMethod("Update"), typeof(SiloSlotUIHooks).GetMethod("Update"));

		}

		/// <summary>Prints to SlimeRancher_Data\output_log.txt prefixed with timestamp and "PlortSpeculator" (convenience method to help with log grepping).</summary>
		/// <param name="message">String to print.</param>
		public static void print(string message) => MonoBehaviour.print($"[{DateTime.Now}] BiggerSilo: {message}");

	}

}
