using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModularTrigger : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent OnEnter, OnExit;
    public bool MakePlayerHeal, MakePlayerEscape, MakePlayerVent;
    public int EscaperID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "LocalPlayer")
        {
            OnEnter.Invoke();
            if (MakePlayerHeal) { other.GetComponent<PlayerMovement>().InHealingArea = true; }
            if (MakePlayerEscape) { other.GetComponent<PlayerMovement>().InPocketDimensionEscaper = true; other.GetComponent<PlayerMovement>().EscaperID = EscaperID; }
            if (MakePlayerVent) { other.GetComponent<PlayerMovement>().InVentRange = true; }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "LocalPlayer")
        {
            OnExit.Invoke();
            if (MakePlayerHeal) { other.GetComponent<PlayerMovement>().InHealingArea = false; }
            if (MakePlayerEscape) { other.GetComponent<PlayerMovement>().InPocketDimensionEscaper = false; other.GetComponent<PlayerMovement>().EscaperID = 0; }
            if (MakePlayerVent) { other.GetComponent<PlayerMovement>().InVentRange = false; }
        }
    }
}
