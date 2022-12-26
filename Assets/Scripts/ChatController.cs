using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProfanityFilterText
{
    public string[] filteredwords = { "shit", "SHIT", "ShIt", "sHiT", "fuck", "FUCK", "FuCk", "fUcK", "Nigga", "nigga", "NIGGA", "nigger", "NIGGER", "Nigger", "sex", "Sex", "SEX", "cunt", "CUNT", "Cunt", "Brian", "brian", "BRIAN" };
    public string[] replacedwords = { "ship", "SHIP", "ShIp", "sHiP", "muck", "MUCK", "MuCk", "MUcK", "Nigerian Prince", "nigerian prince", "NIGERIAN PRINCE", "nigerian prince", "NIGERIAN PRINCE", "Nigerian Prince", "six", "Six", "SIX", "count", "COUNT", "Count", "Brain", "brain", "BRAIN" };
}

public class ChatController : MonoBehaviour
{
    public List<TMP_Text> chatTexts;
    public List<string> CurrentTexts;
    public int MaxMessageLength;
    public static ChatController instance;

    private void Awake()
    {
        instance = this;
        if(!System.IO.File.Exists(Application.streamingAssetsPath + "/profanityfilter.txt"))
        {
            CreateNewProfanityFilter();
        }
    }

    public void CreateNewProfanityFilter()
    {
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/profanityfilter.txt", JsonUtility.ToJson(new ProfanityFilterText()));
    }

    private void Update()
    {
        if (CurrentTexts.Count > MaxMessageLength)
        {
            CurrentTexts.RemoveAt(MaxMessageLength);
        }
        for (int i = 0; i < chatTexts.Count; i++)
        {
            if (i < CurrentTexts.Count)
            {
                chatTexts[i].text = CurrentTexts[i];
            }
            else
            {
                chatTexts[i].text = "";
            }
        }
    }

    public static string ProfanityFilter(string filtered)
    {
        string returnedstring = filtered;
        ProfanityFilterText readprofanityfilter = JsonUtility.FromJson<ProfanityFilterText>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/profanityfilter.txt"));
        for (int i = 0; i < readprofanityfilter.filteredwords.Length; i++)
        {
            returnedstring = returnedstring.Replace(readprofanityfilter.filteredwords[i], readprofanityfilter.replacedwords[i]);
        }
        return returnedstring;
    }

    public void AddMessage(string newmessage = "INSERT MESSAGE HERE")
    {
        string profanityfiltered = System.IO.File.Exists(Application.streamingAssetsPath + "/profanityfilter.txt") ? ProfanityFilter(newmessage) : newmessage; //profanity filter

        CurrentTexts.Insert(0, profanityfiltered);
    }
}
