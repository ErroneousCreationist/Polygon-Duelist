using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DeleteWithoutChildren : NetworkBehaviour
{
    private void Update()
    {
        if (!IsOwner) { return; }
        bool childactive = false;
        foreach (Transform Child in transform)
        {
            if (Child.gameObject.activeSelf) { childactive = true; }
        }
        if (!childactive) { Destroy(gameObject); }
    }
}
