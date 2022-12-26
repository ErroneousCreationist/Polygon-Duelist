using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Test_DamageTrigger : NetworkBehaviour
{
    public TeamStatus myteam;
    public bool UseCircleOverlap;
    public GameObject hitfx; 
    public float damageAmount, radius;
    public bool OnEnter, EnemyTrigger, HitEnemies;
    public float staycooldown;
    public ulong whoShot;
    float currentcooldown;
    public bool UseAreaOverlap;
    public Transform topleft, bottomright;
    public bool debug;
    bool begun;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        begun = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        begun = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOwner) { return; }
        if (UseCircleOverlap || UseAreaOverlap) { return; }
        if (OnEnter) { return; }
        if (currentcooldown <= 0 && other.GetComponent<PlayerMovement>() && !other.GetComponent<PlayerMovement>().isdead.Value)
        {
            if (!other.GetComponent<PlayerMovement>().isdead.Value && other.GetComponent<PlayerMovement>().currenthp.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
            DoDamageToTarget_ServerRPC(OwnerClientId, other.GetComponent<PlayerMovement>().OwnerClientId, damageAmount);
            currentcooldown = staycooldown;
        }
        else if (currentcooldown <= 0 && other.GetComponent<BuildingHealth>())
        {
            other.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount / 2);
            currentcooldown = staycooldown;
        }
    }

    private void Update()
    {
        if (!IsOwner || !begun) { return; } //only run after this has been spawned appropriately and everything
        if(UseAreaOverlap)
        {
            TryAreaOverlap();
        }
        else if(UseCircleOverlap)
        {
            TryCircleOverlap();
        }
    }

    void TryAreaOverlap()
    {
        if (!IsOwner) { return; }
        if (currentcooldown > 0) { currentcooldown -= Time.deltaTime; }
        if (currentcooldown <= 0)
        {
            if (debug) { Debug.Log("tried area overlap"); }

            //get local
            Vector3 frontboxAWorld = transform.TransformPoint(new Vector3(topleft.position.x, topleft.position.y, 0f));
            Vector3 frontboxBWorld = transform.TransformPoint(new Vector3(bottomright.position.x, bottomright.position.y, 0f));
            // Change them to Vector2 so that Physics2D.OverlapArea will take them as parameters
            Vector2 frontboxAFinal = new Vector2(frontboxAWorld.x, frontboxAWorld.y);
            Vector2 frontboxBFinal = new Vector2(frontboxBWorld.x, frontboxBWorld.y);

            currentcooldown = staycooldown;
            Collider2D[] colls = Physics2D.OverlapAreaAll(frontboxAFinal, frontboxBWorld);
            foreach (var hit in colls)
            {
                if (hit.gameObject == gameObject) { continue; }
                if (hit.GetComponent<PlayerMovement>())
                {
                    if (!EnemyTrigger && hit.GetComponent<PlayerMovement>().OwnerClientId == whoShot) { continue; } //dont kill whoever shot me
                    if (!EnemyTrigger && myteam != TeamStatus.Noteam && myteam == hit.GetComponent<PlayerMovement>().CurrentTeam.Value) { continue; }//no teamkilling
                    SpawnHitFX_ServerRPC(hit.transform.position);
                    if (!EnemyTrigger && !hit.GetComponent<PlayerMovement>().isdead.Value && hit.GetComponent<PlayerMovement>().currenthp.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
                    DoDamageToTarget_ServerRPC(whoShot, hit.GetComponent<PlayerMovement>().OwnerClientId, damageAmount); //damage player target
                }
                else if (hit.GetComponent<BuildingHealth>())
                {
                    if (!hit.GetComponent<BuildingHealth>().EngineerBuilding)
                    {
                        if (!HitEnemies) { continue; }//enemy damage triggers ignore enemies
                        SpawnHitFX_ServerRPC(hit.transform.position);
                        if (hit.GetComponent<BuildingHealth>().IsBoss) //give normal kill on boss kill
                        {
                            if (hit.GetComponent<BuildingHealth>().currenthealth.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
                        }
                        else
                        {
                            if (hit.GetComponent<BuildingHealth>().currenthealth.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, true); } //give self a kill if I deserve one
                        }
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount); //damage enemy target
                    }
                    else
                    {
                        if (myteam != TeamStatus.Noteam && hit.GetComponent<BuildingHealth>().placedbyteam == myteam) { continue; } //dont teamkill engineer buildings
                        SpawnHitFX_ServerRPC(hit.transform.position);
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount); //damage building target
                    }
                }
            }
        }
    }

    void TryCircleOverlap()
    {
        if (!IsOwner) { return; }
        if (currentcooldown > 0) { currentcooldown -= Time.deltaTime; }
        if (currentcooldown <= 0)
        {
            currentcooldown = staycooldown;
            Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in colls)
            {
                if (hit.gameObject == gameObject) { continue; }
                if (hit.GetComponent<PlayerMovement>())
                {
                    if (!EnemyTrigger && hit.GetComponent<PlayerMovement>().OwnerClientId == whoShot) { continue; } //dont kill whoever shot me
                    if (!EnemyTrigger && myteam != TeamStatus.Noteam && myteam == hit.GetComponent<PlayerMovement>().CurrentTeam.Value) { continue; }//no teamkilling
                    SpawnHitFX_ServerRPC(hit.transform.position);
                    if (!EnemyTrigger && !hit.GetComponent<PlayerMovement>().isdead.Value && hit.GetComponent<PlayerMovement>().currenthp.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
                    DoDamageToTarget_ServerRPC(whoShot, hit.GetComponent<PlayerMovement>().OwnerClientId, damageAmount); //damage player target
                }
                else if (hit.GetComponent<BuildingHealth>())
                {
                    if (!hit.GetComponent<BuildingHealth>().EngineerBuilding)
                    {
                        if (EnemyTrigger) { continue; }//enemy damage triggers ignore enemies
                        SpawnHitFX_ServerRPC(hit.transform.position);
                        if (hit.GetComponent<BuildingHealth>().IsBoss) //give normal kill on boss kill
                        {
                            if (hit.GetComponent<BuildingHealth>().currenthealth.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
                        }
                        else
                        {
                            if (hit.GetComponent<BuildingHealth>().currenthealth.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, true); } //give self a kill if I deserve one
                        }
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount); //damage enemy target
                    }
                    else
                    {
                        if (myteam != TeamStatus.Noteam && hit.GetComponent<BuildingHealth>().placedbyteam == myteam) { continue; } //dont teamkill engineer buildings
                        SpawnHitFX_ServerRPC(hit.transform.position);
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount); //damage building target
                    }
                }
            }
        }
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC(Vector2 position)
    {
        NetworkObject ob = Instantiate(hitfx, position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) { return; }
        if (!OnEnter) { return; }
        if (UseCircleOverlap || UseAreaOverlap) { return; }
        if (other.GetComponent<PlayerMovement>() && !other.GetComponent<PlayerMovement>().isdead.Value)
        {
            if (!EnemyTrigger && !other.GetComponent<PlayerMovement>().isdead.Value && other.GetComponent<PlayerMovement>().currenthp.Value - damageAmount <= 0) { SendKillToDamager_ServerRPC(whoShot, false); } //give self a kill if I deserve one
            DoDamageToTarget_ServerRPC(whoShot, other.GetComponent<PlayerMovement>().OwnerClientId, damageAmount);
        }
        else if (other.GetComponent<BuildingHealth>())
        {
            other.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(damageAmount / 2);
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
        if (!GameObject.Find("LocalPlayer") || !begun) { return; } //stop errors maybe
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().TakeDamage(amount, out bool didkill); //damage the player target
        //if (!EnemyTrigger && didkill) { Debug.Log(whoShot); SendKillToDamager_ServerRPC(damager, false); } //if kill is detected, then send the kill request serverrpc
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
}
