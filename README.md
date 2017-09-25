# SR_BiggerSilo
Slime Rancher Mod for Unity Plugin Manager that increases silo storage size. Adjusts the maximum number of items any in game entity that uses Silo storage components.

**Last Tested with:** Slime Rancher x64 v1.0.1e

# Building
1. Check this repository.
2. Build [Unity Plugin Manager](https://github.com/UnityPluginManager/PluginManager) and place PluginManager.dll in the [Managed](Managed) folder.
3. Copy the following DLLs from the Slime Rancher data folder (IE: F:\Program Files (x86)\Steam\steamapps\common\Slime Rancher\SlimeRancher_Data\Managed) to the [Managed](Managed) folder:
	* Assembly-CSharp.dll
	* UnityEngine.dll
	* UnityEngine.UI.dll
4. Download and place [HookManager.cs](https://raw.githubusercontent.com/wledfor2/PlayHooky/master/HookManager.cs) from [PlayHooky](https://github.com/wledfor2/PlayHooky) into the [Source](Source) folder.
5. Open BiggerSilo.Windows.sln with VS2017 and build Release.

# Installing
1. Install [Unity Plugin Manager](https://github.com/UnityPluginManager/PluginManager) as described in the repository readme.
2. Install BiggerSilo.dll, and [BiggerSilo.json](BiggerSilo.json) in the Unity Plugin Manager plugins folder.
3. Launch your game, and enjoy.

# Configuring
Adjust the value of maxItemSlots in [BiggerSilo.json](BiggerSilo.json) to your liking. This value must be greater than 0 and less than float.MaxValue. It is recommended this value be 3 digits, but it can be any value (large values will display erroneously on the UI).

# Errors
Any errors will be logged to SlimeRancher_Data\output_log.txt