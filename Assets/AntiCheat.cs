using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.IO;

public class AntiCheat : MonoBehaviour
{
    public GameObject piracyScreen, spookyJumpscare, kickScreen;
    public TMP_Text ipText;
    public bool ForceAntiPiracyScreen;
    public const float velmaglimit = 45f, hplimit = 75;
    PlayerMovement localplayer;

    private void Start()
    {
        spookyJumpscare.SetActive(false);
        piracyScreen.SetActive(false);
        if ((!Application.isEditor && (!Application.genuine || Application.identifier != "com.ErroneousCreations.PolygonDuelist")) || ForceAntiPiracyScreen) { StartCoroutine(AntiPiracyScreenCoroutine()); }
    }

    public static string GetLocalIPAddress()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable) { throw new System.Exception("No Internet Reachability to get IP Address"); }
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

    public static string GetGlobalIPAddress() //get external ip address USING A FUCKING WEBSITE I HATE THIS NOT GONNA LIE
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

    IEnumerator AntiPiracyScreenCoroutine() //if the application has been reverse engineered or something then kick the player lol
    {
        piracyScreen.SetActive(true);
        yield return new WaitForSeconds(9.75f);
        ipText.text = GetLocalIPAddress(); //spooky ip address leak
        spookyJumpscare.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        Application.Quit();
    }
    IEnumerator KickedCheaterCoroutine() //show the kicked cheater prompt
    {
        kickScreen.SetActive(true);
        yield return new WaitForSeconds(5f);
        kickScreen.SetActive(false);
    }

    private void Update()
    {
        if (NetworkManager.Singleton.IsClient && !localplayer && GameObject.Find("LocalPlayer")) { localplayer = GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>(); } //always find the localplayer (when it should exist)
        if(NetworkManager.Singleton.IsClient && localplayer && IsCheating())
        {
            localplayer.Disconnect(); //kick the player
            StopAllCoroutines();
            StartCoroutine(KickedCheaterCoroutine());
        }
    }

    bool IsCheating()
    {
        bool cheater = false;

        //checks
        if (localplayer.rb.velocity.magnitude > velmaglimit) { cheater = true; } //if too fast, then kick
        if (localplayer.rb.angularVelocity > 0.01f) { cheater = true; } //no turning wtf
        if (!localplayer.InPocketDimension && Mathf.Abs(localplayer.transform.position.x) > 29.5f && Mathf.Abs(localplayer.transform.position.y) > 29.5f){ cheater = true; } //if ur not in pocket dimension and ur outside of the map, kick
        if (localplayer.MaxHP + localplayer.currentmaxhpaddition.Value > hplimit) { cheater = true; } //if stupid max hp, kick
        //if (!localplayer.isdead.Value && localplayer.currenthp.Value <= 0) { cheater = true; } //if ur not dead but have less than 0 hp, wtf are you and kick
        //if (localplayer.isdead.Value && (localplayer.currentaltattackcooldown > 0 || localplayer.currentAttackCooldown > 0)) { cheater = true; } //if ur somehow attacking while dead, kick
        return cheater;
    }
}
