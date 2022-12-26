using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System;
using System.Linq;

public class NetworkManagerUI : MonoBehaviour
{
    public Button HostButton, JoinButton, EndHostButton, EndClientButton;
    public Image fill, backdrop;
    public GameObject HostButtons, ClientButtons, MenuButtons, Chat, ConnectionLoadingScreen, ConnectionTimeoutObject, InvalidIPObject;
    public TMP_Text connectiontimeoutText;
    public Slider charSelectSlider, mapSelectSlider, gamemodeSelectSlider;
    public UnityTransport transport;
    public GameObject nowifiIcon; 
    [Header("Characters")]
    public GameObject[] Characters;
    [TextArea]public string[] CharacterNames;
    public uint[] HashList;
    public Sprite[] characterSprites;
    public TMP_Text CharacterSelectText;
    public int currentCharacter;
    public Image characterDisplay;
    [Header("Maps")]
    public GameObject[] Maps;
    [TextArea]public string[] MapNames;
    public TMP_Text MapSelectText;
    public int currentMap;
    [Header("Maps")]
    [TextArea] public string[] GamemodeNames;
    public TMP_Text GamemodeSelectText;
    public int currentGamemode;
    bool inmenu;
    string currentip;

    public static NetworkManagerUI instance;

    public void CopyPublicIPToClipboard()
    {
        TextEditor te = new TextEditor();
        te.text = AntiCheat.GetGlobalIPAddress();
        te.SelectAll();
        te.Copy();
    }

    public void CopyPrivateIPToClipboard()
    {
        TextEditor te = new TextEditor();
        te.text = AntiCheat.GetLocalIPAddress();
        te.SelectAll();
        te.Copy();
    }

    private void Awake()
    {
        instance = this;

        HostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(HashList[currentCharacter]); //get the character id as the payload
            NetworkManager.Singleton.StartHost(); } );
        //HostButton.onClick.AddListener(DisableMenuButtons);

        JoinButton.onClick.AddListener(() => {
            if(Application.internetReachability == NetworkReachability.NotReachable) { StopAllCoroutines(); StartCoroutine(ConnectionFailedScreenCoroutine("No Network Connection")); return; }
            if (!ValidateIPv4(currentip)) { StopAllCoroutines(); StartCoroutine(ConnectionFailedScreenCoroutine("Invalid IP Address")); return; }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes((uint)currentCharacter); //get the character id as the payload
            StartCoroutine(AttemptConnection());
        });
        //JoinButton.onClick.AddListener(DisableMenuButtons);

        EndHostButton.onClick.AddListener(() => { NetworkManager.Singleton.Shutdown(); });
        //EndHostButton.onClick.AddListener(EnableMenuButtons);

        EndClientButton.onClick.AddListener(() => { NetworkManager.Singleton.Shutdown(); });
        //EndClientButton.onClick.AddListener(EnableMenuButtons);

        //NetworkManager.Singleton.OnClientConnectedCallback += Conn_DisableMenu;
        //NetworkManager.Singleton.OnClientDisconnectCallback += Conn_EnableMenu;

        inmenu = true;
        //NetworkManager.Singleton.OnClientConnectedCallback += SendMapRequester;
        NetworkManager.Singleton.OnTransportFailure += EnableMenuButtons;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        //NetworkManager.Singleton.OnClientConnectedCallback += SendCharacterRequester;

        currentCharacter = 1; //defaults to square
        currentGamemode = 0;
        currentMap = 1;
        CharacterSelectText.text = CharacterNames[1];
        GamemodeSelectText.text = GamemodeNames[0];
        MapSelectText.text = MapNames[1];
    }

    IEnumerator AttemptConnection()
    {
        ConnectionLoadingScreen.SetActive(true);
        yield return new WaitForSeconds(3);

        if (NetworkManager.Singleton.IsConnectedClient == false)
        {
            ConnectionLoadingScreen.SetActive(false);
            NetworkManager.Singleton.Shutdown();
            StopAllCoroutines();
            StartCoroutine(ConnectionFailedScreenCoroutine("Connection Timed Out"));
        }
        else
        {
            ConnectionLoadingScreen.SetActive(false);
        }
    }

    IEnumerator ConnectionFailedScreenCoroutine(string text)
    {
        ConnectionTimeoutObject.SetActive(true);
        connectiontimeoutText.text = text;
        yield return new WaitForSeconds(3);
        connectiontimeoutText.text = "";
        ConnectionTimeoutObject.SetActive(false);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // Your approval logic determines the following values
        response.Approved = true; //players can join in the middle of a match (but they become spectators, that logic is below)
        response.CreatePlayerObject = true;

        //string bytes = "";
        //foreach (var bit in connectionData)
        //{
        //    bytes += bit.ToString() + " | "; funny experiment with bytes thats all
        //}
        //Debug.Log(bytes);                                         //check the gamemode and if its lobby mode, set the hash to 100 (spectator character). otherwise, use the connection data
        uint hash = GamemodeManager.instance ? (GamemodeManager.instance.currentGamemode != GameModeEnum.Lobby ? 100 : System.BitConverter.ToUInt32(connectionData, 0)) : System.BitConverter.ToUInt32(connectionData, 0);
        response.PlayerPrefabHash = hash; //set the spawned character to the current character of the connecter (ongod)
        
        response.Position = Vector2.zero; //get a random position

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }

    //void SendMapRequester(ulong connid)//connid is unused
    //{
    //    Debug.Log("Map request serverrpc being sent");
    //    SendMapRequestServerRPC(FindObjectOfType<NetworkManagerUI>().currentMap);
    //}

    //[ServerRpc]
    //void SendMapRequestServerRPC(int mapid)//connid is unused
    //{
    //    Debug.Log("Server recieved map request: " + mapid);
    //    SendMapSetRequestClientRPC(mapid);
    //}

    //[ClientRpc]
    //void SendMapSetRequestClientRPC(int mapid)
    //{
    //    Debug.Log("Client recieved map request: " + mapid);
    //    FindObjectOfType<NetworkManagerUI>().SetMap(mapid);
    //}

    //manually tell the server to spawn a player character for the joining connid (awful ik)



    //void SendCharacterRequester(ulong id)//called by the OnClientConnected callback
    //{
    //    Debug.Log("Character request clientRPC being sent");
    //    GetRequiredCharacterClientRPC(id, new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { id } } }); //send a request to the joining client to get their character preference (awful i know)
    //}



    public void SetPlayerName(string name)
    {
        PlayerPrefs.SetString("PlayerName", name);
    }
    public void SetMap(float id) //set the map 
    {
        MapSelectText.text = MapNames[(int)id];
        currentMap = (int)id;
    }
    public void SetGamemode(float id) //set the gamemode
    {
        currentGamemode = (int)id;
        GamemodeSelectText.text = GamemodeNames[(int)id];
    }
    public void SetCharacter(float id)
    {
        //NetworkManager.Singleton.NetworkConfig.PlayerPrefab = Characters[(int)id];
        CharacterSelectText.text = CharacterNames[(int)id];
        currentCharacter = (int)id;
        characterDisplay.sprite = characterSprites[(int)id];
    }
    public void SetConnectionAddress(string ip)
    {
        if(ValidateIPv4(ip)) { transport.ConnectionData.Address = ip; currentip = ip; InvalidIPObject.SetActive(false); }
        else { transport.ConnectionData.Address = "127.0.0.1"; currentip = ip; InvalidIPObject.SetActive(true); }
    }
    public bool ValidateIPv4(string ipString)
    {
        if (String.IsNullOrEmpty(ipString))
        {
            return false;
        }

        string[] splitValues = ipString.Split('.');
        if (splitValues.Length != 4)
        {
            return false;
        }

        byte tempForParsing;

        return splitValues.All(r => byte.TryParse(r, out tempForParsing));
    }
    public void SetPort(string port)
    {
        ushort shortport; //lol it rhymes!!!
        if (ushort.TryParse(port, out shortport)) { transport.ConnectionData.Port = shortport; }
        else { transport.ConnectionData.Address = "7777"; } //default is 7777
    }
    public void SetCol(float value)
    {
        PlayerPrefs.SetFloat("PlayerCol", value);
    }
    private void Update()
    {
        nowifiIcon.SetActive(Application.internetReachability == NetworkReachability.NotReachable); //show or hide the networkreachability icon

        if (!inmenu && !NetworkManager.Singleton.IsClient) { inmenu = true; } 
        if (inmenu && NetworkManager.Singleton.IsClient) { inmenu = false; }
        if (!inmenu)
        {
            if (NetworkManager.Singleton.IsHost) { HostButtons.SetActive(true); ClientButtons.SetActive(false); }
            else { HostButtons.SetActive(false); ClientButtons.SetActive(true); }
            Chat.SetActive(true);
            MenuButtons.SetActive(false);
        }
        else
        {
            mapSelectSlider.maxValue = Maps.Length - 1;
            gamemodeSelectSlider.maxValue = GamemodeNames.Length - 1;
            charSelectSlider.maxValue = CharacterNames.Length - 1;
            MenuButtons.SetActive(true);
            HostButtons.SetActive(false); ClientButtons.SetActive(false);
            fill.color = Color.HSVToRGB(PlayerPrefs.GetFloat("PlayerCol"), 1, 1);
            backdrop.color = Color.HSVToRGB(PlayerPrefs.GetFloat("PlayerCol"), 1, 1);
            Chat.SetActive(false);
            characterDisplay.color = Color.HSVToRGB(PlayerPrefs.GetFloat("PlayerCol"), 1, 1);
        }
    }
    void DisableMenuButtons()
    {
        MenuButtons.SetActive(false);
        inmenu = false;
    }
    void EnableMenuButtons()
    {
        MenuButtons.SetActive(true);
        inmenu = true;
    }

    void Conn_DisableMenu(ulong id)
    {
        MenuButtons.SetActive(false);
        inmenu = false;
    }
    void Conn_EnableMenu(ulong id)
    {
        MenuButtons.SetActive(true);
        inmenu = true;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
