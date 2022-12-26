using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

[System.Serializable]
public class ModInfoDataClass
{
    public string modname;
    public string version;
    public bool onlylocal;
}

public class ModManager : MonoBehaviour
{
    [System.Serializable]
    public struct GamePrefabDictionary
    {
        public string key;
        public GameObject value;
    }
    public GamePrefabDictionary[] GamePrefabs;
    public static ModManager instance;
    private IEnumerable<I_ModFunctions> mods;
    public GameObject modlistob_def, modUI, moduiButton;
    static readonly KeyCode[] _keyCodes =
            System.Enum.GetValues(typeof(KeyCode))
                .Cast<KeyCode>()
                .Where(k => k < KeyCode.Mouse0)
                .ToArray();
    //static readonly KeyCode[] _keyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    bool hasmods = false;

    public static Dictionary<string, GameObject> StaticGamePrefabs;
    List<int> currentHeldKeys, currentPressedKeys;

    private void Awake()
    {
        instance = this;
        StaticGamePrefabs = new Dictionary<string, GameObject>();
        foreach(GamePrefabDictionary pair in GamePrefabs)
        {
            StaticGamePrefabs.Add(pair.key, pair.value);
        }
        if (!File.Exists(Application.streamingAssetsPath + "/Mods/existingprefabslist.txt")) { CreateDocumentationFile(); }
    }

    public static IEnumerable<int> GetCurrentKeysPressed()
    {
        if (Input.anyKeyDown)
        {
            for (int i = 0; i < _keyCodes.Length; i++)
                if (Input.GetKey(_keyCodes[i]))
                    yield return (int)_keyCodes[i];
        }
    }

    public static IEnumerable<int> GetCurrentKeysHeld()
    {
        if (Input.anyKey)
        {
            for (int i = 0; i < _keyCodes.Length; i++)
                if (Input.GetKey(_keyCodes[i]))
                    yield return (int)_keyCodes[i];
        }
    }

    void CreateDocumentationFile()
    {
        string contents = "";
        foreach (var pair in StaticGamePrefabs)
        {
            contents += "KEY: " + pair.Key + " - PREFAB NAME: " + pair.Value.name + "\n";
        }
        File.WriteAllText(Application.streamingAssetsPath + "/Mods/existingprefabslist.txt", contents);
    }

    public void SetModUI(bool state)
    {
        modUI.SetActive(state);
        moduiButton.SetActive(!state);
    }

    private void Start()
    {
        hasmods = false;
        LoadMods();
        ExecuteMods_OnAppStart();
    }

    private void Update()
    {
        //currentPressedKeys = GetCurrentKeysPressed().ToList();

        //if(GetCurrentKeysPressed().Count() > 0)
        //{
        //    Debug.Log("hello??!!");
        //    try
        //    {
        //        var keytask = Task.Run(async () => {
        //            try
        //            {
        //                await Task.Run(() =>
        //                {
        //                    foreach (var mod in mods)
        //                    {
        //                        mod.OnAnyKeyDown(currentPressedKeys);
        //                    }
        //                });
        //            }
        //            catch
        //            {
        //                Debug.LogError("Keypress inner task failed");
        //            }

        //        });
        //    }
        //    catch
        //    {
        //        Debug.LogError("Keypress outer task failed");
        //    }
        //}
        if (!hasmods) { return; }

        var task = Task.Run(async () => { //start a new async loop for the update
            while (true)
            {
                if (!Application.IsPlaying(gameObject) || !hasmods) { break; }
                try
                {
                    await Task.Run(() => {
                        foreach (var mod in mods)
                        {
                            mod.OnBackgroundUpdate();
                        }
                    });
                }
                catch
                {
                    Debug.LogError("Something Went Wrong Backgroundupdate");
                }

            }
        });
    }

    public void OpenModsFolder()
    {
        TextEditor te = new TextEditor();
        te.text = Application.streamingAssetsPath + "/Mods";
        te.SelectAll();
        te.Copy();
    }

    void LoadMods()
    {
        var wrapper = new CompilerWrapper();
        string modlog = "MODS LOG \n";

        // load text files and run them
        foreach (var modfile in Directory.GetDirectories(Application.streamingAssetsPath + "/Mods", "*_mod"))
        {
            hasmods = true;
            GameObject ob = Instantiate(modlistob_def, modlistob_def.transform.parent);
            ob.SetActive(true);
            string modlistobtext = "";
            if (!File.Exists(modfile + "/main.cs")) { ob.GetComponentInChildren<TMPro.TMP_Text>().text = "!MOD MAIN FILE NOT FOUND!"; continue; }
            bool success = wrapper.Execute(modfile + "/main.cs");
            Debug.Log($"Loaded and Executed Mod Main file {Path.GetFileName(modfile)}, errors: {wrapper.ErrorsCount}, result: {wrapper.GetReport()} ");
            modlog += "LOADED MOD " + modfile + " WITH " + wrapper.ErrorsCount + " ERRORS. REPORT: '" + wrapper.GetReport() + "' SUCCESSFUL:" + success + " \n";
            if (wrapper.ErrorsCount > 0) { modlistobtext = "!LOAD FAILED: ERRORS!"; }
            else
            {
                ModInfoDataClass modinfo = new ModInfoDataClass();
                try { modinfo = JsonUtility.FromJson<ModInfoDataClass>(File.ReadAllText(modfile + "/modinfo.txt")); }
                catch { ob.GetComponentInChildren<TMPro.TMP_Text>().text = "!MOD INFO FILE NOT FOUND!"; continue; }
                modlistobtext = modinfo.modname + " - " + modinfo.version + " - " + (modinfo.onlylocal ? "Local Mod" : "Networked Mod");
            }
            ob.GetComponentInChildren<TMPro.TMP_Text>().text = modlistobtext;

        }
        modlog += "MODS LOADED AND INSTANCED";
        File.WriteAllText(Application.streamingAssetsPath + "/Mods/modlog.txt", modlog);
        mods = wrapper.CreateInstancesOf<I_ModFunctions>();
        Debug.Log("MODS LOADED AND INSTANCED");

    }

    void ExecuteMods_OnAppStart()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnAppStart function of " + mod.modname);
            mod.OnAppStart(); //run the mod's onappstart function
        }
    }

    //private async Task ExecuteMods_OnBackgroundUpdateAsync()
    //{
    //    Debug.Log("what");
    //    foreach (var mod in mods)
    //    {
    //        await Task.Run(() =>
    //        {
    //            //Debug.Log(currentPressedKeys.Count() + " " + currentHeldKeys.Count());
    //            mod.OnBackgroundUpdate(currentPressedKeys, currentHeldKeys);
    //        });
    //    }
    //}

    public void ExecuteMods_OnGameStart()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnGameJoined function of " + mod.modname);
            mod.OnGameJoined(); //run the mod's onappstart function
        }
    }

    public void ExecuteMods_OnGameLeft()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnGameLeft function of " + mod.modname);
            mod.OnGameLeft(); //run the mod's onappstart function
        }
    }

    public void ExecuteMods_OnLocalPlayerDie()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnLocalPlayerDie function of " + mod.modname);
            mod.OnLocalPlayerDie(); //run the mod's onappstart function
        }
    }

    public void ExecuteMods_OnLocalPlayerRespawn()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
           // Debug.Log("Executed Mod OnLocalPlayerRespawn function of " + mod.modname);
            mod.OnLocalPlayerRespawn(); //run the mod's onappstart function
        }
    }

    public void ExecuteMods_OnMatchStart()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnMatchStart function of " + mod.modname);
            mod.OnMatchStarted(); //run the mod's onappstart function
        }
    }

    public void ExecuteMods_OnMatchEnd()
    {
        if (!hasmods) { return; }
        foreach (var mod in mods)
        {
            //Debug.Log("Executed Mod OnMatchEnd function of " + mod.modname);
            mod.OnMatchEnded(); //run the mod's onappstart function
        }
    }
}
