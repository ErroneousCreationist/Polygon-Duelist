using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.NetworkInformation;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;

[Serializable]
public class SecureRankData
{
    public string supportedaddress;
    public int rank;
    public float kills;
    public SecureRankData(string address, int newrank, float kills)
    {
        supportedaddress = address;
        rank = newrank;
        this.kills = kills;
    }
    public bool Compare(SecureRankData compared) //comparision function
    {
        bool returned = true;
        if (compared.supportedaddress != supportedaddress) { returned = false; }
        if (compared.rank != rank) { returned = false; }
        if (compared.kills != kills) { returned = false; }
        return returned; 
    }
}

public class RankManager : MonoBehaviour
{//                                                      N   N+  C   C+  I   I+  G   G+  MVP  MVP+ MVP++ MVP+++
    public static readonly int[] RankKillThresholds = { 30, 35, 40, 50, 55, 65, 70, 75, 100, 100, 125,  999999999 }; //you need 765 kills for MVP+++ :) lololololo!LO!L!O
    public static readonly string[] RankNames = { "Normie", "Normie+", "Cool", "Cool+", "Insane", "Insane+", "Gigachad", "Gigachad+", "MVP", "MVP+", "MVP++", "MVP+++" };
    public static readonly string[] ChatRankNames = { "N", "N+", "C", "C+", "I", "I+", "G", "G+", "MVP", "MVP+", "MVP++", "MVP+++" };

    public static readonly byte[] key1 = { 0x11, 0x12, 0x16, 0x15, 0x16, 0x69, 0x42, 0x15, 0x16, 0x15, 0x16, 0x19, 0x14, 0x12, 0x10, 0x15 };
    public static readonly byte[] key2 = { 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x69, 0x15, 0x69, 0x15, 0x16, 0x19, 0x16, 0x15, 0x16, 0x19 }; //2 hardcoded keys

    static string path1 = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/polygon.duelist";
    static string path2 = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/duelist.polygon";

    int currentrank = 0;
    float currentkills = 0;
    [Header("UI")]
    public GameObject rankupObject;
    public TMP_Text rankup_text, ranktext;
    public Image rankimage;
    public Sprite[] rankSprites;
    public Slider killslider, rankslider;

    public static RankManager instance;

    string uniquecomputerid;
    byte[] uniquecomputerIV;

    private void Awake()
    {
        instance = this;

        uniquecomputerid = SystemInfo.deviceUniqueIdentifier;

        uniquecomputerIV = System.Text.Encoding.ASCII.GetBytes(uniquecomputerid.Substring(0, 16)); //get the IV from the first 7 characters of the device identifier

        string p = "";
        foreach (var byt in uniquecomputerIV)
        {
            p += byt.ToString() + " ";
        }
        //Debug.Log(p);
    }

    public string FetchMacId()
    {
        string macAddresses = "";

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                macAddresses += nic.GetPhysicalAddress().ToString();
                break;
            }
        }
        return macAddresses;
    }

    public void SaveNewRankData() //save the rank data to a secure location with a secure 
    {
        SecureRankData data = new SecureRankData(uniquecomputerid, 0, 0);
        string jsonconverted = JsonUtility.ToJson(data); //convert to JSON
        //BinaryFormatter formatter = new BinaryFormatter();
        //Debug.Log(jsonconverted);

        //save first file
        FileStream stream = new FileStream(path1, FileMode.Create);
        Aes aes1 = AesManaged.Create();
        aes1.Key = key1;
        aes1.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer1 = aes1.CreateEncryptor(aes1.Key, aes1.IV);
        CryptoStream cryptoStream1 = new CryptoStream(stream, cryptographer1, CryptoStreamMode.Write);
        StreamWriter writer1 = new StreamWriter(cryptoStream1);
        writer1.Write(jsonconverted);
        writer1.Close();
        cryptoStream1.Close();
        stream.Close();

        //save first file
        FileStream stream2 = new FileStream(path2, FileMode.Create);
        Aes aes2 = AesManaged.Create();
        aes2.Key = key2;
        aes2.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer2 = aes2.CreateEncryptor(aes2.Key, aes2.IV);
        CryptoStream cryptoStream2 = new CryptoStream(stream2, cryptographer2, CryptoStreamMode.Write);
        StreamWriter writer2 = new StreamWriter(cryptoStream2);
        writer2.Write(jsonconverted);
        writer2.Close();
        cryptoStream2.Close();
        stream2.Close();
    }

    public void SaveRankData() //save the rank data to a secure location with a secure 
    {
        SecureRankData data = new SecureRankData(uniquecomputerid, currentrank, currentkills);
        string jsonconverted = JsonUtility.ToJson(data); //convert to JSON
        //BinaryFormatter formatter = new BinaryFormatter();
        //Debug.Log(jsonconverted);

        //save first file
        FileStream stream = new FileStream(path1, FileMode.Create);
        Aes aes1 = AesManaged.Create();
        aes1.Key = key1;
        aes1.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer1 = aes1.CreateEncryptor(aes1.Key, aes1.IV);
        CryptoStream cryptoStream1 = new CryptoStream(stream, cryptographer1, CryptoStreamMode.Write);
        StreamWriter writer1 = new StreamWriter(cryptoStream1);
        writer1.Write(jsonconverted);
        writer1.Close();
        cryptoStream1.Close();
        stream.Close();

        //save first file
        FileStream stream2 = new FileStream(path2, FileMode.Create);
        Aes aes2 = AesManaged.Create();
        aes2.Key = key2;
        aes2.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer2 = aes2.CreateEncryptor(aes2.Key, aes2.IV);
        CryptoStream cryptoStream2 = new CryptoStream(stream2, cryptographer2, CryptoStreamMode.Write);
        StreamWriter writer2 = new StreamWriter(cryptoStream2);
        writer2.Write(jsonconverted);
        writer2.Close();
        cryptoStream2.Close();
        stream2.Close();
    }

    public SecureRankData LoadRankData()
    {
        //load first file
        FileStream stream = new FileStream(path1, FileMode.Open);
        Aes aes1 = AesManaged.Create();
        aes1.Key = key1;
        aes1.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer1 = aes1.CreateDecryptor(aes1.Key, aes1.IV);
        CryptoStream cryptoStream1 = new CryptoStream(stream, cryptographer1, CryptoStreamMode.Read);
        StreamReader reader1 = new StreamReader(cryptoStream1);
        string jsonplaintext1 = reader1.ReadToEnd();
        reader1.Close();
        cryptoStream1.Close();
        stream.Close();
        //Debug.Log(jsonplaintext1);
        if (JsonUtility.FromJson<SecureRankData>(jsonplaintext1) == null) { Debug.LogError("Error reading Rank Data File, Decrypt failure"); return new SecureRankData(uniquecomputerid, 0, 0); }
        SecureRankData data1 = JsonUtility.FromJson<SecureRankData>(jsonplaintext1); //decrypt and read the data from the file

        //load second file
        FileStream stream2 = new FileStream(path2, FileMode.Open);
        Aes aes2 = AesManaged.Create();
        aes2.Key = key2;
        aes2.IV = uniquecomputerIV; //wtf is IV lol
        ICryptoTransform cryptographer2 = aes2.CreateDecryptor(aes2.Key, aes2.IV);
        CryptoStream cryptoStream2 = new CryptoStream(stream2, cryptographer2, CryptoStreamMode.Read);
        StreamReader reader2 = new StreamReader(cryptoStream2);
        string jsonplaintext2 = reader2.ReadToEnd();
        reader2.Close();
        cryptoStream2.Close();
        stream2.Close();
        //Debug.Log(jsonplaintext2);
        if (JsonUtility.FromJson<SecureRankData>(jsonplaintext2) == null) { Debug.LogError("Error reading Rank Data File, Decrypt failure"); return new SecureRankData(uniquecomputerid, 0, 0); }
        SecureRankData data2 = JsonUtility.FromJson<SecureRankData>(jsonplaintext2); //decrypt and read the data from the file

        //string path = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/polygon.duelist";
        //if (!File.Exists(path)) { StopAllCoroutines(); StartCoroutine(ShowRankupText("Rank Cleared due to Missing Rank File!")); Debug.LogError("No Rank File Found at LocalApplicationData"); return new SecureRankData(FetchMacId(), 0, 0); } //fail if path not found
        //BinaryFormatter formatter = new BinaryFormatter();
        //FileStream stream = new FileStream(path, FileMode.Open);
        //SecureRankData data1 = formatter.Deserialize(stream) as SecureRankData;
        //stream.Close();

        //string path2 = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/duelist.polygon";
        //if (!File.Exists(path2)) { StopAllCoroutines(); StartCoroutine(ShowRankupText("Rank Cleared due to Missing Rank File!")); Debug.LogError("No Rank File Found at LocalApplicationData"); return new SecureRankData(FetchMacId(), 0, 0); } //fail if path not found
        //BinaryFormatter formatter2 = new BinaryFormatter();
        //FileStream stream2 = new FileStream(path2, FileMode.Open);
        //SecureRankData data2 = formatter.Deserialize(stream2) as SecureRankData;
        //stream2.Close();

        if (!data1.Compare(data2)) { StopAllCoroutines(); StartCoroutine(ShowRankupText("Rank Cleared due to Mismatch!")); Debug.LogError("Rank files do NOT match! Clearing Rank Files!"); return new SecureRankData(uniquecomputerid, 0, 0); }
        if(data1.supportedaddress != uniquecomputerid) { StopAllCoroutines(); StartCoroutine(ShowRankupText("Rank Cleared due to Incorrect MAC Address!")); Debug.LogError("Looks like your MAC address doesn't match the Rank File! Clearing!"); return new SecureRankData(uniquecomputerid, 0, 0); }
        return data1;
    }

    void Start()
    {
        //check missing save files and create them if so
        if(!File.Exists(path1) || !File.Exists(path2))
        {
            SaveNewRankData();
            //Debug.Log(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + " LocalApplicationData folder");
        }

        SecureRankData data = LoadRankData();
        currentkills = data.kills;
        currentrank = data.rank;
        //Debug.Log(currentkills + " kills || " + currentrank + " rank");
    }

    private void Update()
    {
        rankimage.sprite = rankSprites[currentrank];
        ranktext.text = RankNames[currentrank];
        killslider.value = currentrank == RankKillThresholds.Length - 1 ? 0 : currentkills / RankKillThresholds[currentrank]; //if you are at max rank then dont show the kills lol
        rankslider.value = currentrank == RankKillThresholds.Length - 1 ? 1 : (float)currentrank / (float)(RankKillThresholds.Length - 1); //do the ui stuff
    }

    private void OnApplicationQuit()
    {
        SaveRankData(); //save rank data on quit
    }

    public void AddKill()
    {
        currentkills += 1;
        if(currentkills >= RankKillThresholds[currentrank])
        {
            currentrank += 1;
            currentkills = 0;
            StopAllCoroutines();
            StartCoroutine(ShowRankupText("Rank up! " + ChatRankNames[currentrank]));
        }
    }
    public void AddQuarterKill()
    {
        currentkills += 0.25f;
        if (currentkills >= RankKillThresholds[currentrank])
        {
            currentrank += 1;
            currentkills = 0;
            StopAllCoroutines();
            StartCoroutine(ShowRankupText("Rank up! " + ChatRankNames[currentrank]));
        }
    }

    IEnumerator ShowRankupText(string text)
    {
        rankupObject.SetActive(true);
        rankup_text.text = text;
        yield return new WaitForSeconds(2);
        rankup_text.text = "";
        rankupObject.SetActive(false);
    }
}
