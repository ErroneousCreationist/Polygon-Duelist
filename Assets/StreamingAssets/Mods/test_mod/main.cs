using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Test_ModMain : I_ModFunctions
{
    public string modname { get { return "Test Mod"; } }


    public void OnAppStart()
    {
        HelperFunctions.AddAssetToNetworkManager(HelperFunctions.ReadPrefabFromAssetBundle_Index("/Mods/test_mod/testmodassetbundle", 1));
        GameObject.Find("MapSelectTitle").GetComponent<TMP_Text>().color = Color.red;
        HelperFunctions.SpawnAssetLocally("/Mods/test_mod/testmodassetbundle", 0, new Vector2(0, 200), 0, GameObject.Find("Canvas").transform);
        Debug.Log(HelperFunctions.ReadPrefabFromAssetBundle_Index("/Mods/test_mod/testmodassetbundle", 0).name);
    }

    public void OnBackgroundUpdate()
    {
        
    }

    public void OnGameJoined()
    {

    }

    public void OnGameLeft()
    {

    }

    public void OnLocalPlayerDie()
    {
        HelperFunctions.SpawnAssetOnNetwork("/Mods/test_mod/testmodassetbundle", 1, GameObject.Find("LocalPlayer").transform.position, 0, 1);
    }

    public void OnLocalPlayerRespawn()
    {

    }

    public void OnMatchStarted()
    {

    }

    public void OnMatchEnded()
    {

    }

}