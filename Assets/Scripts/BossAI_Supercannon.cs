using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BossAI_Supercannon : NetworkBehaviour
{
    public GameObject projectile;
    public float movespeed, maxSpeed, rotSpeed, projectilespeed, attackspeed, viewRange, circleRange, shootdistance = 1.5f;
    public Rigidbody2D rb;

    PlayerMovement currentPlayerTarget;
    Transform currentObjectTarget;
    bool iscurrentlyAttacking, attackingPlayer;
    Vector2 currentPostarget;
    float currentattacktimer;

    private void Start()
    {
        if (!IsOwner) { return; }
        currentPlayerTarget = null;
    }

    private bool IsEnemyInRange(float RangeThresh, Vector3 origin, ref Transform closesttarget)
    {
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
        bool found = false;
        closesttarget = null;
        float LowestDist = RangeThresh;
        foreach (var enemy in players)
        {
            if (Vector3.Distance(enemy.transform.position, origin) <= LowestDist) //ignore invisible players NEVERMIND
            {
                LowestDist = Vector3.Distance(enemy.transform.position, origin);
                found = true;
                closesttarget = enemy.transform;
            }
        }
        if (found) { return found; }

        bool targetfound = false;
        float lowesttargetdist = RangeThresh;
        foreach (var target in Targeted.ENEMY_TARGETS)
        {
            if (Vector3.Distance(target.transform.position, origin) <= lowesttargetdist) //ignore invisible players
            {
                lowesttargetdist = Vector3.Distance(target.transform.position, origin);
                targetfound = true;
                closesttarget = target.transform;
            }
        }
        return targetfound; //if a non-player target is spotted and no players are detected, attack the non-player
    }

    void Retarget()
    {
        iscurrentlyAttacking = IsEnemyInRange(viewRange, transform.position, ref currentObjectTarget);
        if (iscurrentlyAttacking)
        {
            currentPostarget = Random.insideUnitCircle.normalized * circleRange;
            if (currentObjectTarget.GetComponent<PlayerMovement>()) { currentPlayerTarget = currentObjectTarget.GetComponent<PlayerMovement>(); attackingPlayer = true; }
            else { attackingPlayer = false; }
        }
        else { currentPostarget = Vector2.zero; }
    }

    void Attack()
    {
        Fire_ServerRPC();
    }

    [ServerRpc]
    private void Fire_ServerRPC()
    {
        NetworkObject ob = Instantiate(projectile, transform.position + transform.up * shootdistance, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        ob.GetComponent<Projectile>().ShotBy = 999;
        ob.GetComponent<Projectile>().IsEnemyProj = true;
        ob.transform.up = transform.up;
        ob.GetComponent<Rigidbody2D>().velocity = transform.up.normalized * projectilespeed;
        Destroy(ob.gameObject, 30);
    }

    private void Update()
    {
        if (!IsOwner) { return; }
        currentattacktimer -= Time.deltaTime;
        if(currentattacktimer <= 0 && !iscurrentlyAttacking) { currentattacktimer = attackspeed; Retarget(); }
        else if(currentattacktimer <= 0 && iscurrentlyAttacking) { currentattacktimer = attackspeed; Attack(); }

        if(iscurrentlyAttacking)
        {
            if(attackingPlayer)
            {
                if (!currentPlayerTarget || currentPlayerTarget.isdead.Value || currentPlayerTarget.PublicInvisBoolean || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > viewRange) { Retarget(); return; }
            }
            else
            {
                if (!currentObjectTarget || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > viewRange) { Retarget(); return; }
            }
            Vector2 direction = (((Vector2)currentObjectTarget.position + currentPostarget) - (Vector2)transform.position).normalized;
            Vector2 direction2 = ((Vector2)currentObjectTarget.position - (Vector2)transform.position).normalized;
            float amount = Vector3.Cross(direction2, transform.up).z;
            rb.angularVelocity = -amount * rotSpeed;
            rb.AddForce(direction * movespeed);
            rb.velocity = Vector2.ClampMagnitude(rb.velocity, movespeed);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }    
    }
}
