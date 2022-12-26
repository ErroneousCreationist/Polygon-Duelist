using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Reflection;

public class TurretAI : NetworkBehaviour
{
    public ulong WhoPlaced;
    public TeamStatus placedbyteam;
    public GameObject projectile;
    public Transform turret;
    public float speed, fireCooldown, range;
    NetworkVariable<Vector3> turretrot = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    float currentfirecooldown;
    PlayerMovement myplayer;

    private void Start()
    {
        if (!IsOwner) { return; }
        myplayer = NetworkManager.Singleton.ConnectedClients[WhoPlaced].PlayerObject.GetComponent<PlayerMovement>();
        turretrot.Value = Vector3.up;
    }

    private bool IsEnemyInRange(float RangeThresh, Vector3 origin, ref Transform closesttarget)
    {
        bool targetfound = false;
        float lowesttargetdist = RangeThresh;
        foreach (var target in Targeted.TURRET_TARGETS)
        {
            if (Vector3.Distance(target.transform.position, origin) <= lowesttargetdist) //ignore invisible players
            {
                lowesttargetdist = Vector3.Distance(target.transform.position, origin);
                targetfound = true;
                closesttarget = target.transform;
            }
        }
        if (targetfound) { return targetfound; } //if a non-player target is spotted, then return that as priority over a player

        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
        bool found = false;
        closesttarget = null;
        float LowestDist = RangeThresh;
        foreach (var enemy in players)
        {
            if (Vector3.Distance(enemy.transform.position, origin) <= LowestDist && enemy.OwnerClientId != WhoPlaced && !enemy.PublicInvisBoolean && !(placedbyteam != TeamStatus.Noteam && placedbyteam == enemy.CurrentTeam.Value)) //ignore invisible players and players on my own team
            {
                LowestDist = Vector3.Distance(enemy.transform.position, origin);
                found = true;
                closesttarget = enemy.transform;
            }
        }
        return found;
    }

    private void Update()
    {
        turret.up = turretrot.Value; //set the turretrot synced
        if (!IsOwner) { return; }
        if (!myplayer) { DestroyThisGameobject_ServerRPC(); }
        if (myplayer.isdead.Value) { DestroyThisGameobject_ServerRPC(); }
        currentfirecooldown -= Time.deltaTime;
        if(currentfirecooldown <= 0)
        {
            currentfirecooldown = fireCooldown;
            Transform closesttarget = null;
            IsEnemyInRange(range, transform.position, ref closesttarget);
            if(closesttarget)
            {
                turretrot.Value = ((Vector2)closesttarget.transform.position - (Vector2)transform.position).normalized; //set turret up to nearest uncloaked target
                Fire_ServerRPC(); //fire projectile
            }
        }
    }

    [ServerRpc]
    private void Fire_ServerRPC()
    {
        NetworkObject ob = Instantiate(projectile, turret.position + turret.up * 0.9f, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        ob.GetComponent<Projectile>().ShotBy = WhoPlaced;
        ob.GetComponent<Projectile>().ShotByInvis = false;
        ob.transform.up = turret.up;
        ob.GetComponent<Rigidbody2D>().velocity = turret.up.normalized * speed;
        Destroy(ob.gameObject, 30);
    }


    [ServerRpc]
    void DestroyThisGameobject_ServerRPC() //destroy the object serverside
    {
        Destroy(gameObject);
    }

}
