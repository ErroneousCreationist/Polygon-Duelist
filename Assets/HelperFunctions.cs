using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;

public interface I_ModFunctions
{
    string modname { get; }
    void OnAppStart();
    void OnBackgroundUpdate();
    void OnGameJoined();
    void OnGameLeft();
    void OnLocalPlayerDie();
    void OnLocalPlayerRespawn();
    void OnMatchStarted();
    void OnMatchEnded();
    //void OnAnyKeyDown(List<int> keys);
}

//public class ModBase : I_ModFunctions
//{
//    public string modname { get { return "Mod name here"; } }

//    public void OnAppStart()
//    {

//    }

//    public void OnBackgroundUpdate()
//    {

//    }

//    public void OnGameJoined()
//    {

//    }
//}

public static class HelperFunctions
{
    /// <summary>
    /// Adds to an array, for example the character names list
    /// </summary>
    /// <param name="Org">The original array</param>
    /// <param name="New_Value">The new value that will be added</param>
    /// <returns>Returns the array after the value has been added</returns>
    public static T[] AddtoArray<T>(this T[] Org, T New_Value) 
    {
        T[] New = new T[Org.Length + 1];
        Org.CopyTo(New, 0);
        New[Org.Length] = New_Value;
        return New;
    }

    /// <summary>
    /// Adds a new player object to the game
    /// </summary>
    /// <param name="description">The description of the player in the menu</param>
    /// <param name="charsprite">The characters sprite as seen in the menu</param>
    /// <param name="charobject">The characters gameobject</param>
    public static void AddToCharacterList(string description, Sprite charsprite, GameObject charobject)
    {
        NetworkManagerUI.instance.CharacterNames = AddtoArray<string>(NetworkManagerUI.instance.CharacterNames, description); //add name to character names
        NetworkManagerUI.instance.characterSprites = AddtoArray<Sprite>(NetworkManagerUI.instance.characterSprites, charsprite); //add name to character names
        NetworkManagerUI.instance.Characters = AddtoArray<GameObject>(NetworkManagerUI.instance.Characters, charobject); //add name to character names
        NetworkManager.Singleton.AddNetworkPrefab(charobject);
    }

    /// <summary>
    /// Adds a new map to the game
    /// </summary>
    /// <param name="description">The description of the map in the menu</param>
    /// <param name="mapobject">The gameobject itself</param>
    public static void AddToMapsList(string description, GameObject mapobject)
    {
        NetworkManagerUI.instance.MapNames = AddtoArray<string>(NetworkManagerUI.instance.MapNames, description); //add name to character names
        NetworkManagerUI.instance.Maps = AddtoArray<GameObject>(NetworkManagerUI.instance.Maps, mapobject); //add name to character names
    }

    public static List<Transform> GetPlayersLocally()
    {
        List<Transform> returned = new List<Transform>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            returned.Add(client.PlayerObject.transform);
        }
        return returned;
    }

    public static AssetBundle ReadAssetBundle(string path)
    {
        var bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + path);
        if (bundle == null)
        {
            throw new System.Exception("Failed To Load AssetBundle");
        }
        return bundle;
    }

    public static GameObject ReadPrefabFromAssetBundle_Index(string path, int index)
    {
        var bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + path);
        if (bundle == null)
        {
            throw new System.Exception("Failed To Load AssetBundle");
        }
        GameObject ob = bundle.LoadAsset<GameObject>(bundle.GetAllAssetNames()[index]);
        bundle.Unload(false);
        return ob;
    }

    public static bool GetIsOnServer()
    {
        return NetworkManager.Singleton.IsServer;
    }

    public static GameObject SpawnObjectLocally(GameObject ob, Vector2 pos, float rot, Transform parent, float destroytime = -1)
    {
        GameObject obb = Object.Instantiate(ob, pos, new Quaternion(0, 0, rot, 0), parent);
        if (destroytime != -1) { Object.Destroy(obb, destroytime); }
        return obb;
    }

    public static void AddAssetToNetworkManager(GameObject ob)
    {
        NetworkManager.Singleton.AddNetworkPrefab(ob);
    }

    public static GameObject SpawnAssetLocally(string path, int index, Vector2 pos, float rot, Transform parent, float destroytime = -1)
    {
        GameObject obb = Object.Instantiate(ReadPrefabFromAssetBundle_Index(path, index), pos, new Quaternion(0, 0, rot, 0), parent);
        if (destroytime != -1) { Object.Destroy(obb, destroytime); }
        return obb;
    }

    public static void SpawnAssetOnNetwork(string path, int index, Vector2 pos, float rot, float destroytime = 9999)
    {
        LocalGamemodeController.instance.SpawnAsset_ServerRPC(path, index, pos, rot, destroytime == -1 ? Mathf.Infinity : destroytime);
    }

    public static bool HasInternetAccess()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    public static string GetLocalIPAddress()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable) { throw new System.Exception("No Internet Reachability to get IP Address"); }
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    public static string GetGlobalIPAddress() //get external ip address USING A WEBSITE I HATE THIS NOT GONNA LIE
    {
        if (Application.internetReachability == NetworkReachability.NotReachable) { throw new System.Exception("No Internet Reachability to get IP Address"); }

        var url = "https://api.ipify.org/"; //thank you this website

        WebRequest request = WebRequest.Create(url);
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        Stream dataStream = response.GetResponseStream();

        using StreamReader reader = new StreamReader(dataStream);

        var ip = reader.ReadToEnd();
        reader.Close();
        dataStream.Close();

        return ip;
    }

    public static GameObject AccessExistingGamePrefab(string id)
    {
        return ModManager.StaticGamePrefabs[id];
    }
}
