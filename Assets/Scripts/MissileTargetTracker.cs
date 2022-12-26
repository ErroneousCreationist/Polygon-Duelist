using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MissileTargetTracker : NetworkBehaviour
{
    public Projectile myProj;
    public Transform constanttarget;
    PlayerMovement currentplayertarget;
    public float trackSpeed, speed, RetargetRange, LookRange;
    public GameObject explosion;
    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!IsOwner) { return; }
        if (!IsEnemyInRange(RetargetRange, transform.position, ref constanttarget)) { Explode(); } //get target, if none then explode
    }

    //private bool IsEnemyInRange(float RangeThresh, Vector3 origin, ref Transform closestEnemy)
    //{
    //    PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
    //    bool found = false;
    //    closestEnemy = null;
    //    float LowestDist = RangeThresh;
    //    foreach (var enemy in players)
    //    {
    //        if (Vector3.Distance(enemy.transform.position, origin) <= LowestDist && !enemy.IgnoreMissileTarget && !enemy.PublicInvisBoolean) //ignore hexagons
    //        {
    //            LowestDist = Vector3.Distance(enemy.transform.position, origin);
    //            found = true;
    //            closestEnemy = enemy.transform;
    //            
    //        }
    //    }
    //    return found;
    //}

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
            if (Vector3.Distance(enemy.transform.position, origin) <= LowestDist && !enemy.isdead.Value && enemy.CurrentTeam.Value != myProj.shotbyteam && enemy.OwnerClientId != myProj.ShotBy) //ignore players of the same team as who shot it, and ofc who shot it, and ignore dead bodies suuuus
            {
                LowestDist = Vector3.Distance(enemy.transform.position, origin);
                found = true;
                closesttarget = enemy.transform;
                currentplayertarget = enemy;
            }
        }
        return found;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }
        if (!constanttarget) { return; }
        if (currentplayertarget && (currentplayertarget.PublicInvisBoolean || currentplayertarget.isdead.Value)) { if (!IsEnemyInRange(LookRange, transform.position, ref constanttarget)) { Explode(); return; } } //if the target goes invisible, retarget or self destruct
        if(Vector2.Distance(transform.position, constanttarget.position) > RetargetRange) { if (!IsEnemyInRange(LookRange, transform.position, ref constanttarget)) { Explode(); return; } } //try to retarget if target is out of range, otherwise explode if none are found
        Vector2 currenttarget = constanttarget.position;
        Vector2 direction = (currenttarget - rb.position).normalized;
        float amount = Vector3.Cross(direction, transform.up).z;
        rb.angularVelocity = -amount * trackSpeed;
        rb.velocity += (Vector2)transform.up * speed;
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, speed);
        //when hit is handled by projectile script
    }

    void Explode()
    {
        DestroyThisGameobjecy_ServerRPC();
        SpawnHitFX_ServerRPC();
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC()
    {
        NetworkObject ob = Instantiate(explosion, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }

    [ServerRpc]
    void DestroyThisGameobjecy_ServerRPC()
    {
        Destroy(gameObject);
    }
}