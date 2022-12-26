using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

//classic is MOST KILLS IN A TIME PERIOD, classic long is that but longer, deathmatch is LAST MAN STANDING, lobby is WAITING FOR MATCH TO START
//bossfight is FREE PLAY BUT FIGHT A RANDOM BOSS, wavesdeathmatch is DEATHMATCH BUT ENEMIES SPAWN, 
public enum GameModeEnum { Lobby = 100, FreePlay = 0, Classic, Classic_Long, Deathmatch, Bossfight, WavesDeathmatch, WavesDeathmatch_Coop, Monster, Manhunt, AmogusSUUUUUS, HideNSeek }

public class GamemodeManager : NetworkBehaviour
{
    public static GamemodeManager instance;
    public GameModeEnum currentGamemode;
    [Header("Bosses")]
    public GameObject[] Bosses;
    [Header("Enemies")]
    public GameObject[] Stage1Enemies;
    public GameObject[] Stage2Enemies, Stage3Enemies;
    public const float Stage2Thresh = 90, Stage3Thresh = 150, BossThresh = 180, BossMaxTime = 330;

    float timer, timer2;
    GameObject currentboss;
    List<GameObject> currentenemies;

    [ServerRpc]
    public void EndMatch_ServerRPC()
    {
        ShowGameEndScreen_ClientRpc("(GAME ENDED) NOBODY", 99999999, false, false, TeamStatus.Noteam); //show the ending screen on all clients
        SetMaps_ClientRPC(0); //reset to lobby
        currentGamemode = GameModeEnum.Lobby;
        SetGamemode_ClientRPC(currentGamemode, false, 999999, TeamStatus.Noteam);
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().SendChatInputToServer_ServerRPC("[MATCH ENDED]");
        ResetPlayers_ClientRPC();
    }

    [ServerRpc]
    public void StartMatch_ServerRPC()
    {
        SetMaps_ClientRPC(NetworkManagerUI.instance.currentMap); //set the maps for all clients
        //LocalGamemodeController.instance.SetMap(FindObjectOfType<NetworkManagerUI>().currentMap); //sets the correct map for my client

        currentGamemode = (GameModeEnum)NetworkManagerUI.instance.currentGamemode;
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().SendChatInputToServer_ServerRPC("[MATCH STARTED]");
        if (currentGamemode == GameModeEnum.FreePlay)
        {
            SetGamemode_ClientRPC(currentGamemode, false, 999999, TeamStatus.Noteam);
        }
        if (currentGamemode == GameModeEnum.Classic)
        {
            timer = 180; //set timer to 3m
            SetGamemode_ClientRPC(currentGamemode, false, 999999, TeamStatus.Noteam);
        }
        else if (currentGamemode == GameModeEnum.Classic_Long)
        {
            timer = 360; //set timer to 6m
            SetGamemode_ClientRPC(currentGamemode, false, 999999, TeamStatus.Noteam);
        }
        else if(currentGamemode == GameModeEnum.Bossfight)
        {
            timer = 10; //10 second startup timer in bossfight mode
            SetGamemode_ClientRPC(currentGamemode, false, 999999, TeamStatus.Noteam);
        }
        else if (currentGamemode == GameModeEnum.WavesDeathmatch || currentGamemode == GameModeEnum.WavesDeathmatch_Coop)
        {
            timer = 0;
            timer2 = 0;
            currentenemies = new List<GameObject>();
            SetGamemode_ClientRPC(currentGamemode, false, 0, TeamStatus.Noteam);
        }
        else if (currentGamemode == GameModeEnum.Manhunt)
        {
            timer = 180; //set timer to 3m
            SetGamemode_ClientRPC(currentGamemode, true, Random.Range(0, NetworkManager.ConnectedClientsIds.Count), TeamStatus.Hunted);
        }
        else if (currentGamemode == GameModeEnum.Monster)
        {
            SetGamemode_ClientRPC(currentGamemode, true, Random.Range(0, NetworkManager.ConnectedClientsIds.Count), TeamStatus.Monster);
        }
        else if (currentGamemode == GameModeEnum.AmogusSUUUUUS)
        {
            SetGamemode_ClientRPC(currentGamemode, true, Random.Range(0, NetworkManager.ConnectedClientsIds.Count), TeamStatus.Outsider);
        }
        else if (currentGamemode == GameModeEnum.HideNSeek)
        {
            timer = 180; //set timer to 3m
            timer2 = 15;
            SetGamemode_ClientRPC(currentGamemode, true, Random.Range(0, NetworkManager.ConnectedClientsIds.Count), TeamStatus.Seeker);
        }
    }

    [ClientRpc]
    public void SetGamemode_ClientRPC(GameModeEnum id, bool overrideteam, int overridedclientid, TeamStatus overridedteam) //if it has the selected client id, then override its role to something else
    {
        if(overrideteam && FindObjectOfType<PlayerMovement>().OwnerClientId == NetworkManager.ConnectedClientsIds[overridedclientid])
        {
            LocalGamemodeController.instance.SetLocalGamemode(id, true, overridedteam);
        }
        else
        {
            LocalGamemodeController.instance.SetLocalGamemode(id, false, TeamStatus.Noteam);
        }
    }

    [ClientRpc]
    public void ShowGameEndScreen_ClientRpc(string winner, ulong winningclient, bool everyonewins = false, bool teamwins = false, TeamStatus winningteam = TeamStatus.Noteam)
    {
        LocalGamemodeController.instance.ShowGameOverScreen(winner, winningclient, everyonewins, teamwins, winningteam);
    }

    private void Awake()
    {
        instance = this;
        currentGamemode = GameModeEnum.Lobby; //set to lobby on start
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        if(currentGamemode == GameModeEnum.Classic || currentGamemode == GameModeEnum.Classic_Long) { ClassicUpdate(); }
        if(currentGamemode == GameModeEnum.Deathmatch) { DeathmatchUpdate(); }
        if(currentGamemode == GameModeEnum.Bossfight) { BossMatchUpdate(); }
        if(currentGamemode == GameModeEnum.WavesDeathmatch) { WavesDeathmatchUpdate(); }
        if (currentGamemode == GameModeEnum.WavesDeathmatch_Coop) { WavesDeathmatchSinglePlayerUpdate(); }
        if (currentGamemode == GameModeEnum.Monster) { MonsterUpdate(); }
        if (currentGamemode == GameModeEnum.Manhunt) { ManhuntUpdate(); }
        if (currentGamemode == GameModeEnum.AmogusSUUUUUS) { AmogusUpdate(); }
        if (currentGamemode == GameModeEnum.HideNSeek) { HideNSeekUpdate(); }

    }

    private void HideNSeekUpdate()
    {
        if(timer2 <= 0)
        {
            timer -= Time.deltaTime;
            UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers

            if (timer <= 0) //hunted wins if they survive throughout the match
            {
                ShowGameEndScreen_ClientRpc("HIDERS", 99999999, false, true, TeamStatus.Hider); //show the ending screen on all clients
                SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
                currentGamemode = GameModeEnum.Lobby;
                ResetPlayers_ClientRPC();
                return;
            }

            bool atleast1hideralive = false;
            string seekername = "";

            foreach (var item in NetworkManager.Singleton.ConnectedClients)
            {
                if (item.Value.PlayerObject.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Hider && !item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value)
                {
                    atleast1hideralive = true;
                    continue;
                }
                if (item.Value.PlayerObject.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Seeker) { seekername = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString(); }
            }
            if (!atleast1hideralive) //if monster killed everyone
            {
                ShowGameEndScreen_ClientRpc(seekername + " (SEEKER)", 99999999, false, true, TeamStatus.Seeker); //show the ending screen on all clients
                SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
                currentGamemode = GameModeEnum.Lobby;
                ResetPlayers_ClientRPC();
                return;
            }
        }
        else
        {
            timer2 -= Time.deltaTime;
            UpdateClientTimers_ClientRPC((float)System.Math.Round(timer2, 2)); //update client timers
        }
    }

    private void AmogusUpdate()
    {
        bool impalive = false;
        bool atleast1matealive = false;
        string impname = "";

        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if (item.Value.PlayerObject.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Outsider)
            {
                impalive = !item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value;
                impname = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString();
            }
            else if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value)
            {
                atleast1matealive = true;
            }
        }
        if (impalive && !atleast1matealive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc(impname + " (THE OUTSIDER)", 99999999, false, true, TeamStatus.Outsider); //show the ending screen on all clients
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ResetPlayers_ClientRPC();
            return;
        }
        if (!impalive && atleast1matealive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc("(" + impname + "WAS THE OUTSIDER), MATES", 99999999, false, true, TeamStatus.Mate); //show the ending screen on all clients
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ResetPlayers_ClientRPC();
            return;
        }
    }

    private void ManhuntUpdate()
    {
        timer -= Time.deltaTime;
        UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers

        if(timer <= 0) //hunted wins if they survive throughout the match
        {
            ShowGameEndScreen_ClientRpc("HUNTED", 99999999, false, true, TeamStatus.Hunted); //show the ending screen on all clients
            ResetPlayers_ClientRPC();
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            return;
        }

        bool huntedalive = false;
        bool atleast1hunteralive = false;

        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if (item.Value.PlayerObject.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Hunted)
            {
                huntedalive = !item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value;
            }
            else if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value)
            {
                atleast1hunteralive = true;
            }
        }
        if (huntedalive && !atleast1hunteralive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc("HUNTED", 99999999, false, true, TeamStatus.Hunted); //show the ending screen on all clients
            ResetPlayers_ClientRPC();
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            return;
        }
        if (!huntedalive && atleast1hunteralive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc("HUNTERS", 99999999, false, true, TeamStatus.Hunter); //show the ending screen on all clients
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ResetPlayers_ClientRPC();
            return;
        }
    }

    private void MonsterUpdate()
    {
        bool monsteralive = false;
        bool atleast1hunteralive = false;
        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if(item.Value.PlayerObject.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Monster)
            {
                monsteralive = !item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value;
            }
            else if(!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value)
            {
                atleast1hunteralive = true;
            }
        }
        if (monsteralive && !atleast1hunteralive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc("MONSTER", 99999999, false, true, TeamStatus.Monster); //show the ending screen on all clients
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ResetPlayers_ClientRPC();
            return;
        }
        if (!monsteralive && atleast1hunteralive) //if monster killed everyone
        {
            ShowGameEndScreen_ClientRpc("MONSTER HUNTERS", 99999999, false, true, TeamStatus.Antimonster); //show the ending screen on all clients
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ResetPlayers_ClientRPC();
            return;
        }
    }

    void WavesDeathmatchSinglePlayerUpdate() //just deathmatch but the game ends when there is 0 players left
    {
        int aliveamount = 0;
        ulong alive = 0;
        string winnername = "";
        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value) { aliveamount += 1; alive = item.Key; winnername = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString(); }
        }
        if (aliveamount <= 0) //if somehow everyone died by the end??!
        {
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc("ENEMIES", 99999999, false); //show the ending screen on all clients
            for (int i = 0; i < currentenemies.Count; i++)
            {
                Destroy(currentenemies[i]); //destroy all enemies
            }
            Destroy(currentboss);
            ResetPlayers_ClientRPC();
            return;
        }

        //if there are players alive, then spawn enemies
        timer += Time.deltaTime;
        UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers
        timer2 -= Time.deltaTime;
        if (timer2 <= 0)
        {
            if (timer > BossThresh)
            {
                if (Random.value < ((timer / BossMaxTime) * (timer / BossMaxTime) * (timer / BossMaxTime) * (timer / BossMaxTime)) && currentboss == null)
                {
                    Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                    NetworkObject ob = Instantiate(Bosses[Random.Range(0, Bosses.Length)], target + Random.insideUnitCircle.normalized * 9, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                    ob.Spawn(true); //spawn on network
                    currentenemies.Add(ob.gameObject); //add to current enemies list
                    currentboss = ob.gameObject;
                }
            } //immediately after bosses are eligible, there will be a 8% chance. at the max time, it will be a 100% chance of course. there can only be one boss at a time though
            timer2 = 7.5f;
            if (timer > Stage2Thresh)
            {
                if (timer > Stage3Thresh)
                {
                    for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count * 1.5f); i++) //for all the players in the server
                    {
                        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                        NetworkObject ob = Instantiate(Stage3Enemies[Random.Range(0, Stage3Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                        ob.Spawn(true); //spawn on network
                        currentenemies.Add(ob.gameObject); //add to current enemies list
                    }
                }
                else
                {
                    for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count); i++) //for 75% of the players in the server
                    {
                        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                        NetworkObject ob = Instantiate(Stage2Enemies[Random.Range(0, Stage2Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                        ob.Spawn(true); //spawn on network
                        currentenemies.Add(ob.gameObject); //add to current enemies list
                    }
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count * 0.75f); i++) //for 60% of the players in the server
                {
                    Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                    NetworkObject ob = Instantiate(Stage1Enemies[Random.Range(0, Stage1Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                    ob.Spawn(true); //spawn on network
                    currentenemies.Add(ob.gameObject); //add to current enemies list
                }
            }
        }
    }

    void WavesDeathmatchUpdate()
    {
        int aliveamount = 0;
        ulong alive = 0;
        string winnername = "";
        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value) { aliveamount += 1; alive = item.Key; winnername = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString(); }
        }
        if (aliveamount == 1) //if theres one person standing
        {
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc(winnername, alive); //show the ending screen on all clients
            for (int i = 0; i < currentenemies.Count; i++)
            {
                Destroy(currentenemies[i]); //destroy all enemies
            }
            Destroy(currentboss);
            ResetPlayers_ClientRPC();
            return;
        }
        else if (aliveamount <= 0) //if somehow everyone died by the end??!
        {
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc("NOBODY", 9999999); //show the ending screen on all clients
            for (int i = 0; i < currentenemies.Count; i++)
            {
                Destroy(currentenemies[i]); //destroy all enemies
            }
            ResetPlayers_ClientRPC();
            return;
        } 

        //if there are players alive, then spawn enemies
        timer += Time.deltaTime;
        UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers
        timer2 -= Time.deltaTime;
        if (timer2 <= 0)
        {
            if(timer > BossThresh)
            {
                if (Random.value < ((timer / BossMaxTime) * (timer / BossMaxTime) * (timer / BossMaxTime) * (timer / BossMaxTime)) && currentboss == null)
                {
                    Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                    NetworkObject ob = Instantiate(Bosses[Random.Range(0, Bosses.Length)], target + Random.insideUnitCircle.normalized * 9, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                    ob.Spawn(true); //spawn on network
                    currentenemies.Add(ob.gameObject); //add to current enemies list
                    currentboss = ob.gameObject;
                }
            } //immediately after bosses are eligible, there will be a 8% chance. at the max time, it will be a 100% chance of course. only 1 boss at a time
            timer2 = 7.5f;
            if (timer > Stage2Thresh)
            {
                if (timer > Stage3Thresh)
                {
                    for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count * 1.5f); i++) //for all the players in the server
                    {
                        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                        NetworkObject ob = Instantiate(Stage3Enemies[Random.Range(0, Stage3Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                        ob.Spawn(true); //spawn on network
                        currentenemies.Add(ob.gameObject); //add to current enemies list
                    }
                }
                else
                {
                    for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count); i++) //for 75% of the players in the server
                    {
                        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                        NetworkObject ob = Instantiate(Stage2Enemies[Random.Range(0, Stage2Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                        ob.Spawn(true); //spawn on network
                        currentenemies.Add(ob.gameObject); //add to current enemies list
                    }
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Round(NetworkManager.Singleton.ConnectedClientsList.Count * 0.75f); i++) //for 60% of the players in the server
                {
                    Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position; //get random player as target
                    NetworkObject ob = Instantiate(Stage1Enemies[Random.Range(0, Stage1Enemies.Length)], target + Random.insideUnitCircle.normalized * 5, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the gamemode controller
                    ob.Spawn(true); //spawn on network
                    currentenemies.Add(ob.gameObject); //add to current enemies list
                }
            }
        }
    }

    void BossMatchUpdate()
    {
        if(timer > -1) //start of round timer
        {
            timer -= Time.deltaTime;
            UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers
            if (timer <= 0)
            {
                timer = -2;
                NetworkObject ob = Instantiate(Bosses[Random.Range(0, Bosses.Length)], Vector2.zero, Quaternion.identity).GetComponent<NetworkObject>(); //spawn the boss
                ob.Spawn(true);
                currentboss = ob.gameObject; //spawn the boss and get a reference to it
                UpdateClientTimers_ClientRPC(-1); //update client timers to show nothing
            }
        }
        else //normal update
        {
            if (!currentboss)
            {
                SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
                currentGamemode = GameModeEnum.Lobby;
                ShowGameEndScreen_ClientRpc("Players", 999999, true); //show the ending screen on all clients that they won against the boss
                ResetPlayers_ClientRPC();
            }
            else
            {
                int aliveamount = 0;
                foreach (var item in NetworkManager.Singleton.ConnectedClients)
                {
                    if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value) { aliveamount += 1; }
                }
                if (aliveamount <= 0)
                {
                    Destroy(currentboss); //destroy the boss of course
                    SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
                    currentGamemode = GameModeEnum.Lobby;
                    ShowGameEndScreen_ClientRpc("Boss", 999999); //show the ending screen on all clients
                    ResetPlayers_ClientRPC();
                }
            }
        }
    }

    void DeathmatchUpdate()
    {
        int aliveamount = 0;
        ulong alive = 0;
        string winnername = "";
        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            if (!item.Value.PlayerObject.GetComponent<PlayerMovement>().isdead.Value) { aliveamount += 1; alive = item.Key; winnername = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString(); }
        }
        if(aliveamount == 1) //if theres one person standing
        {
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc(winnername, alive); //show the ending screen on all clients
            ResetPlayers_ClientRPC();
        }
        else if (aliveamount <= 0) //if somehow everyone died by the end??!
        {
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc("NOBODY", 9999999); //show the ending screen on all clients
            ResetPlayers_ClientRPC();
        }
    }

    void ClassicUpdate()
    {
        timer -= Time.deltaTime;
        UpdateClientTimers_ClientRPC((float)System.Math.Round(timer, 2)); //update client timers
        if(timer <= 0)
        {
            string playername = "";
            int highestkills = -1;
            ulong winningclient = 0;
            foreach (var item in NetworkManager.Singleton.ConnectedClients)
            {
                if(item.Value.PlayerObject.GetComponent<PlayerMovement>().currentKills.Value > highestkills)
                {
                    highestkills = item.Value.PlayerObject.GetComponent<PlayerMovement>().currentKills.Value;
                    playername = item.Value.PlayerObject.GetComponent<PlayerMovement>().playername.Value.ToString(); //get the thing and playername
                    winningclient = item.Key;
                }
                else if (item.Value.PlayerObject.GetComponent<PlayerMovement>().currentKills.Value == highestkills) //if a draw, nobody wins bc funny
                {
                    playername = "DRAW"; 
                    winningclient = 99999999; //nobody wins in a draw
                }
            }
            SetGamemode_ClientRPC(GameModeEnum.Lobby, false, 99999, TeamStatus.Noteam); //set the gamemode back to lobby mode
            ResetPlayers_ClientRPC();
            currentGamemode = GameModeEnum.Lobby;
            ShowGameEndScreen_ClientRpc(playername, winningclient); //show the ending screen on all clients
        }
    }

    [ClientRpc]
    void ResetPlayers_ClientRPC()
    {
        PlayerMovement pm = GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>(); //fine the local player
        pm.TakeDamage(999, out bool boolean, false, true); //kill to reset everything (ignoring lobby protection)
        pm.StartCoroutine(resetTimer(pm)); //reset after a short time
    }

    IEnumerator resetTimer(PlayerMovement pm)
    {
        yield return new WaitForSeconds(0.5f);
        pm.Respawn(); //respawn them
        pm.SetKills_ServerRPC(0);
        pm.SetStreak_ServerRPC(0); //remove kills and streak and deaths
        pm.SetDeaths(0);
        LocalGamemodeController.instance.UpdateTimerText(-1); //hide the timer text
    }

    [ClientRpc]
    void UpdateClientTimers_ClientRPC(float time)
    {
        LocalGamemodeController.instance.UpdateTimerText(time);
    }

    [ClientRpc]
    void SetMaps_ClientRPC(int map)
    {
        LocalGamemodeController.instance.SetMap(map); //sets the correct map for all clients
    }
}
