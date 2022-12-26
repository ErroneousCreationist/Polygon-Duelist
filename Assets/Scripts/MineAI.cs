using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MineAI : NetworkBehaviour
{
    public bool active = true;
    public float Damage, Radius, MineRadius;
    public GameObject explodeFx;
    public LayerMask lm;
    public ulong whoPlaced;
    public TeamStatus myteam;

    PlayerMovement myplayer;

    private void Start()
    {
        if (!IsOwner) { return; }
        myplayer = NetworkManager.Singleton.ConnectedClients[whoPlaced].PlayerObject.GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!IsOwner) { return; }
        if (!myplayer) { DestroyThisGameobjecy_ServerRPC(); }
        if (myplayer.isdead.Value) { DestroyThisGameobjecy_ServerRPC(); }
        if (!active) { return; }
        Collider2D collision = Physics2D.OverlapCircle(transform.position, MineRadius);
        if((collision.GetComponent<PlayerMovement>() && collision.GetComponent<PlayerMovement>().OwnerClientId != whoPlaced && !(myteam != TeamStatus.Noteam && myteam == collision.GetComponent<PlayerMovement>().CurrentTeam.Value)) || (collision.GetComponent<BuildingHealth>() && !collision.GetComponent<BuildingHealth>().EngineerBuilding)) //detect any enemy or player that steps on it
        {
            Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, Radius);
            foreach (var hit in colls)
            {
                if (hit.GetComponent<PlayerMovement>())
                {
                    if (!hit.GetComponent<PlayerMovement>().isdead.Value && hit.GetComponent<PlayerMovement>().currenthp.Value - Damage <= 0) { SendKillToDamager_ServerRPC(whoPlaced, false); } //give self a kill if I deserve one
                    DoDamageToTarget_ServerRPC(whoPlaced, hit.GetComponent<PlayerMovement>().OwnerClientId, Damage); //damage player target
                }
                else if (hit.GetComponent<BuildingHealth>() && !hit.GetComponent<BuildingHealth>().EngineerBuilding)
                {
                    if (hit.GetComponent<BuildingHealth>().IsBoss) //give normal kill on boss kill
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - Damage <= 0) { SendKillToDamager_ServerRPC(whoPlaced, false); } //give self a kill if I deserve one
                    }
                    else
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - Damage <= 0) { SendKillToDamager_ServerRPC(whoPlaced, true); } //give self a kill if I deserve one
                    }
                    hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(Damage); //damage enemy target
                }
            }
            SpawnHitFX_ServerRPC();
            DestroyThisGameobjecy_ServerRPC();
        }
    }

    [ServerRpc]
    void DoDamageToTarget_ServerRPC(ulong damager, ulong targetid, float amount)
    {
        DoDamageToTarget_ClientRPC(damager, amount, new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { targetid } } }); //send a clientrpc to a specific client to damage it
    }

    [ClientRpc]
    void DoDamageToTarget_ClientRPC(ulong damager, float amount, ClientRpcParams clientparams)
    {
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().TakeDamage(amount, out bool didkill); //damage the player target
        //if (didkill) { Debug.Log(damager); SendKillToDamager_ServerRPC(damager); } //if kill is detected, then send the kill request serverrpc
    }

    [ServerRpc]
    void SendKillToDamager_ServerRPC(ulong targetid, bool quarter)
    {
        if (targetid == 0) { if (quarter) { GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().AddQuarterKill(); } else { GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().AddKill(); } return; } //if the target is the host, then give kill to the host
        SendKillToDamager_ClientRPC(quarter, new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { targetid } } }); //send serverrpc to request the server to send the kill to the damager
    }

    [ClientRpc]
    void SendKillToDamager_ClientRPC(bool quarter, ClientRpcParams clientparams)
    {
        if (quarter) { GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().AddQuarterKill(); }
        else
        {
            GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().AddKill();
        }
    }

    [ServerRpc]
    void DestroyThisGameobjecy_ServerRPC()
    {
        Destroy(gameObject);
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC()
    {
        NetworkObject ob = Instantiate(explodeFx, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }
}
