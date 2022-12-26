using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class LocalGamemodeController : NetworkBehaviour
{
    public static LocalGamemodeController instance;
    public GameObject Controller; 
    PlayerMovement m_player;
    public GameModeEnum currentlocalgamemode; //store the current gamemode
    TMP_Text timerText, gameendtext, wintext, roletext;
    CanvasGroup WinScreen;
    CanvasGroup startbuttongroup, endgamebuttongroup;
    Button startgamebutton, endgamebutton;
    float timer;
    [HideInInspector]public GameObject[] Maps;
    [HideInInspector]public int currentmap;

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SetMap(0, true); //set all maps to true
    }

    void Start()
    {
        if (!IsOwner) { return; }
        instance = this;
        m_player = GetComponent<PlayerMovement>();
        WinScreen = GameObject.Find("GameOverScreen").GetComponent<CanvasGroup>();
        timerText = GameObject.Find("TimerText").GetComponent<TMP_Text>();
        gameendtext = GameObject.Find("GameOverText").GetComponent<TMP_Text>();
        wintext = GameObject.Find("WinnerText").GetComponent<TMP_Text>();
        roletext = GameObject.Find("RoleText").GetComponent<TMP_Text>();
        roletext.text = "";
        timerText.text = "";
        WinScreen.alpha = 0;
        Maps = NetworkManagerUI.instance.Maps;
        SetMap(0); //show the lobby
        if(IsOwnedByServer)
        {
            NetworkObject ob = Instantiate(Controller, Vector2.zero, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
            ob.Spawn(true);

            startgamebutton = GameObject.Find("StartGameButton").GetComponent<Button>();
            startgamebutton.onClick.RemoveAllListeners(); //clear listeners first
            startgamebutton.onClick.AddListener(StartMatch_ServerRPC);
            startbuttongroup = GameObject.Find("StartGameButton").GetComponent<CanvasGroup>();

            endgamebutton = GameObject.Find("EndGameButton").GetComponent<Button>();
            endgamebutton.onClick.RemoveAllListeners(); //clear listeners first
            endgamebutton.onClick.AddListener(EndMatch_ServerRPC);
            endgamebuttongroup = GameObject.Find("EndGameButton").GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if(IsOwner && IsOwnedByServer)
        {
            startbuttongroup.alpha = currentlocalgamemode == GameModeEnum.Lobby ? 1 : 0;
            startgamebutton.interactable = currentlocalgamemode == GameModeEnum.Lobby;
            endgamebuttongroup.alpha = currentlocalgamemode != GameModeEnum.Lobby ? 1 : 0;
            endgamebutton.interactable = currentlocalgamemode != GameModeEnum.Lobby;
        }
    }

    public void SetMap(int index, bool allactive = false)
    {
        if (!IsOwner) { return; }
        currentmap = index;
        for (int i = 0; i < Maps.Length; i++)
        {
            Maps[i].SetActive(i == index || allactive);
        }
    }

    [ServerRpc]
    public void StartMatch_ServerRPC()
    {
        timerText.text = "";
        GamemodeManager.instance.StartMatch_ServerRPC(); //starts the game
    }
    [ServerRpc]
    public void EndMatch_ServerRPC()
    {
        timerText.text = "";
        GamemodeManager.instance.EndMatch_ServerRPC(); //starts the game
    }

    public void SetLocalGamemode(GameModeEnum gamemode, bool overrideteam, TeamStatus overridedteam)
    {
        timerText.text = "";
        if(gamemode == GameModeEnum.Lobby) { ModManager.instance.ExecuteMods_OnMatchEnd(); }
        else { ModManager.instance.ExecuteMods_OnMatchStart(); } //run the modmanager stuff
        currentlocalgamemode = gamemode;
        m_player.SetTeam(overrideteam ? overridedteam : TeamByGamemode(gamemode)); //set the players team
        if(overridedteam == TeamStatus.Monster) { m_player.Heal(999); m_player.SetStreak_ServerRPC(14); }//heal new monster player to max and give them their streak abilities
        if(overridedteam == TeamStatus.Seeker) { m_player.StartCoroutine(m_player.HideAndSeek_SeekerWaitTime()); } //lock the seeker in place at te start of the match
        roletext.text = RoleTextPerTeam(overrideteam ? overridedteam : TeamByGamemode(gamemode)); //set the role text
    }

    public TeamStatus TeamByGamemode(GameModeEnum gamemode)
    {
        TeamStatus team = TeamStatus.Noteam;
        if(gamemode == GameModeEnum.Bossfight || gamemode == GameModeEnum.WavesDeathmatch_Coop) { team = TeamStatus.Coop; } //co op gamemodes, everyone is on the Same Team!
        if (gamemode == GameModeEnum.Manhunt) { team = TeamStatus.Hunter; } //hunted role is assigned elsewhere, everyone here is hunter
        if (gamemode == GameModeEnum.Monster) { team = TeamStatus.Antimonster; } //monster role is assigned elsewhere, everyone here is antimonster
        return team;
    }

    public string RoleTextPerTeam(TeamStatus team)
    {
        string returned = "";
        if(team == TeamStatus.Monster) { returned = "Monster - kill everyone else"; }
        if (team == TeamStatus.Antimonster) { returned = "Monster Hunter - kill the monster"; }
        if (team == TeamStatus.Hunted) { returned = "Hunted - current objective: survive"; }
        if (team == TeamStatus.Hunter) { returned = "Hunter - find and kill the hunted"; }
        if (team == TeamStatus.Mate) { returned = "Mate - stop the Outsider"; }
        if (team == TeamStatus.Outsider) { returned = "Outsider - kill all of the Mates"; }
        if (team == TeamStatus.Hider) { returned = "Hider - hide from the Seeker"; }
        if (team == TeamStatus.Seeker) { returned = "Seeker - find and kill every Hider"; }

        return returned;
    }

    public void UpdateTimerText(float time)
    {
        timerText.text = time == -1 ? "" : time + "s";
    }

    public void ShowGameOverScreen(string winnername, ulong winnerid, bool everyonewins, bool teamwins, TeamStatus winningteam)
    {
        WinScreen.alpha = 1; //set all of the game over screen stuff
        if (!teamwins)
        {
            wintext.text = everyonewins ? "YOU WIN!!!" : (winnerid == OwnerClientId ? "YOU WIN!!!" : "YOU LOSE L BOZO"); //if everyone wins then everyone gets the you win message, otherwise show whoever won
        }
        else
        {
            wintext.text = m_player.CurrentTeam.Value == winningteam ? "YOU WIN!!!" : "YOU LOSE L BOZO"; //if a team wins, then check if i'm on the winning team
        }
        gameendtext.text = winnername + " WON THE MATCH";
        StartCoroutine(GameOverScreen());
    }

    IEnumerator GameOverScreen()
    {
        yield return new WaitForSeconds(5);
        WinScreen.alpha = 0;
        roletext.text = ""; //reset the role text
        SetMap(0); //reset the map
    }

    [ServerRpc]
    public void SpawnAsset_ServerRPC(string path, int index, Vector2 pos, float rot, float destroytime = 999)
    {
        NetworkObject netob = Instantiate(HelperFunctions.ReadPrefabFromAssetBundle_Index(path, index), pos, new Quaternion(0, 0, rot, 0)).GetComponent<NetworkObject>();
        netob.Spawn(true);
        Destroy(netob.gameObject, destroytime);
    }
}
