using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BossAI_Worm : NetworkBehaviour
{
    public float MoveSpeed, RotSpeed, ViewRange, MinDist, MaxDist;
    public Transform connected;
    public Vector2 offset;

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
            if (Vector3.Distance(enemy.transform.position, origin) <= LowestDist) //ignore invisible players
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
        iscurrentlyAttacking = IsEnemyInRange(ViewRange, transform.position, ref currentObjectTarget);
        if (iscurrentlyAttacking)
        {
            if (currentObjectTarget.GetComponent<PlayerMovement>()) { currentPlayerTarget = currentObjectTarget.GetComponent<PlayerMovement>(); attackingPlayer = true; }
            else { attackingPlayer = false; }
        }
        else { currentPostarget = Vector2.zero; }
    }

    private void Update()
    {
        if (!IsOwner) { return; }
        if(!connected || !connected.gameObject.activeSelf)
        {
            currentattacktimer -= Time.deltaTime;

            if (iscurrentlyAttacking)
            {
                if (currentattacktimer <= 0) { currentattacktimer = 1; Retarget(); }
                if (attackingPlayer)
                {
                    if (!currentPlayerTarget || currentPlayerTarget.isdead.Value || currentPlayerTarget.PublicInvisBoolean || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > ViewRange) { Retarget(); return; }
                }
                else
                {
                    if (!currentObjectTarget || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > ViewRange) { Retarget(); return; }
                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector3.forward, (currentObjectTarget.position - transform.position).normalized), RotSpeed * Time.deltaTime);
                transform.position += transform.up * (MoveSpeed * Time.deltaTime);
            }
            else
            {
                if (currentattacktimer <= 0) { currentattacktimer = 1; Retarget(); }
            }
        }
        else
        {
            //no pathfinding ai required because it just moves toward parent segment
            Vector2 lastPosition = connected.transform.position;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, ((Vector2)connected.position - (Vector2)transform.position).normalized);
            Vector2 target = lastPosition - (Vector2)(transform.up * MinDist);
            float dist = (target - (Vector2)transform.position).magnitude;
            transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Lerp(0.0f, MinDist, dist / MaxDist));
        }
    }
}
