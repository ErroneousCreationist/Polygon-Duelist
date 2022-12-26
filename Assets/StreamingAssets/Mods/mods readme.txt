Modding for Polygon Duelist

DISCLAIMER: Erroneous Creationist is not responsible for any damages caused by mods. The use of these unstable and potentially dangerous third party modifications is at your own risk.

IMPORTANT NOTE: MAKING ANYTHING SYNCED OVER THE NETWORK IS VERY IMPORTANT TO GET RIGHT, OTHERWISE TONS OF ERRORS CAN OCCUR. POLYGON DUELIST IS SERVER AUTHORATIVE, MEANING ENEMIES AND BOSSES AND PROJECTILES ARE OWNED BY SERVER, AND PLAYER MOVEMENT IS HANDLED BY THE SERVER. KEEP THIS IN MIND WHEN DOING ANYTHING OVER THE NETWORK. For more info, you can visit the Unity Netcode for Gameobjects docs.

Notes: Your mod must consist of a folder which is your mod name with '_mod' at the end, e.g "sussycharacter_mod".
Each mod has its 'main.cs' which is its main script. This contains all the functions that are run. Code within can use most unity libraries.
Each mod contains 'modinfo.txt' which contains its basic information such as name and version. Ensure JSON formatting is kept when changing this file.
Both of the above scripts are *MANDATORY*
Additionally, each mod can have ONE assetbundle each that is loaded in on application start. It can contain sprites, prefabs, meshes, materials. These can then be used to instantiate prefabs or replace sprites in the scripts.

There is a HelperFunctions class which contains functions for doing things. It contains the functions below:
AddToMapsList(description[string], ob[GameObject]) //adds a map to the maps list, allowing it to show in the main menu UI. The gameobject must be one in the scene.
ReadAssetBundle(path[string]) //finds and returns the AssetBundle at the path
ReadPrefabFromAssetBundle_Index(path[string], index[int]) //Reads an AssetBundle and returns the prefab at the index from the assetbundle
GetIsOnServer() //Returns if the local client is running the server
SpawnObjectLocally(ob[GameObject], pos[Vector2], rot[float], parent[Transform], destroytime[float]) //Spawn an object locally (not on the network) with the position, rotation and parent values, and adds a destroy timer if destroytime isn't -1. Returns the spawned gameobject
SpawnObjectLocally(path[string], index[int], pos[Vector2], rot[float], parent[Transform], destroytime[float]) //Spawn a prefab asset from an AssetBundle locally (not on the network) with the position, rotation and parent values, and adds a destroy timer if destroytime isn't -1. Returns the spawned gameobject
SpawnAssetOnNetwork(path[string], index[int], pos[Vector2], rot[float], destroytime[float]) //Spawns a prefab asset from an AssetBundle on the network with the position and rotation values, and adds a destroy timer if destroytime isn't -1. Object must have a NetworkObject component and must have been added to the NetworkManager prefab list previously
AddAssetToNetworkManager(ob[GameObject]) //adds a gameobject (prefab asset) to the NetworkManager prefabs list. this is required to have that gameobject be instantiated on the network.
HasInternetAccess() //returns if the application can access the network
GetLocalIPAddress() //returns the local (internal) Ipv4 Address
GetGlobalIpAddress() //returns the global (public) Ipv4 Address (heavy performance wise)
AccessExistingGamePrefab(index[string]) //Accesses an existing game prefab at the string id (all string ids can be found in the file in Mods)
GetPlayersLocally() //returns the transform of every player currently joined

IMPORTANT TYPES
Polygon Duelist uses many different types in its design. The most important include:

PlayerMovement - the player script thats on every player, handles all player actions like movement and attacking
BuildingHealth - the health script for Concave buildings, bosses and enemies
TurretAI - the AI that controls turrets
MineAI - the AI that controls landmines
LocalGamemodeController - controls gamemodes locally
GameModeEnum - the enum that contains every gamemode
TeamStatus - the enum containing every team status in the game
Projectile - controls projectiles
MissileTargetTracker - controls missiles
HideTrigger - controls bushes and building roofs hide behaviour
ModularTrigger - used for healing triggers, pocket dimension escapers and vents. Can trigger any event
Test_DamageTrigger - used to damage players
//boss ais
BossAI_Satan - controls the pentagram boss
bossAI_Fish - controls the fish and charger boss
BossAI_Worm - controls the worm boss
BossAI_StrangeThing - controls the strange thing boss
BossAI_Supercannon - controls the cannon boss

Other important Non-Custom classes:
Unity.Netcode.NetworkManager - the networkmanager that handles the server and client infrastructure (Singleton can be accessed with NetworkManager.Singleton)
Unity.Netcode.NetworkObject - the required component to make an object synced over the network
Unity.Netcode.Components.NetworkTransform - the component that syncs position over the network
Unity.Netcode.Components.NetworkRigidbody2D - the component that ensures smooth rigidbody2d movement over the network