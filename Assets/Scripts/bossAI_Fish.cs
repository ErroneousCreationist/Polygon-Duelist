using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class bossAI_Fish : NetworkBehaviour
{
    public float MoveSpeed, RotSpeed, DashSpeed, DashCooldownTime, ViewRange;
    public Rigidbody2D rb;
    public bool TeleportToPlayer;
    public float TeleportDistance, projectileSpeed, DashTime;
    public GameObject TeleportFX, projectile; 
    PlayerMovement currentPlayerTarget;
    Transform currentObjectTarget;
    bool iscurrentlyAttacking, attackingPlayer, runningDashAttack;
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

    void Attack(Vector2 direction)
    {
        if (TeleportToPlayer && Random.value < 0.4f) { StartCoroutine(TeleportAttack()); return; }
        rb.AddForce(direction * DashSpeed);
    }

    [ServerRpc]
    private void Fire_ServerRPC(Vector2 direction, Vector2 position)
    {
        NetworkObject ob = Instantiate(projectile, position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        ob.GetComponent<Projectile>().ShotBy = 999;
        ob.GetComponent<Projectile>().IsEnemyProj = true;
        ob.transform.up = direction;
        ob.GetComponent<Rigidbody2D>().velocity = direction.normalized * projectileSpeed;
        Destroy(ob.gameObject, 30);
    }

    [ServerRpc]
    private void SpawnFX_ServerRPC(Vector2 position, float dietime)
    {
        NetworkObject ob = Instantiate(TeleportFX, position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, dietime);
    }

    IEnumerator TeleportAttack()
    {
        runningDashAttack = true;
        gameObject.layer = LayerMask.NameToLayer("SCP106"); //become phase thru guy
        SpawnFX_ServerRPC(transform.position, 1); //spawn the effect
        transform.position = new Vector2(0, 100); //vanish somewhere offscreen
        yield return new WaitForSeconds(0.666f); //wait a bit before reappearing
        transform.position = GetDashPos(out Vector2 target); //teleport around a player
        transform.up = (target - (Vector2)transform.position).normalized;
        SpawnFX_ServerRPC(transform.position, 1); //spawn the effect for appearing
        yield return new WaitForSeconds(0.15f); //wait a bit before dashing

        float angle = 45; // Fire off 3 projectiles for funny
        Vector2 direction = transform.up;
        for (int i = 0; i < 3; i++)
        {
            Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
            Fire_ServerRPC(pos, (Vector2)transform.position + pos); 
            angle -= 45;
        }

        rb.AddForce((target - (Vector2)transform.position).normalized * DashSpeed);
        yield return new WaitForSeconds(DashTime); //wait for the dash to be complete
        runningDashAttack = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy"); //obey physics again
        Retarget();
    }

    Vector2 GetDashPos(out Vector2 pos)
    {
        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position;
        pos = target; //set the target of the dash
        return target + Random.insideUnitCircle * TeleportDistance;
    }

    private void Update()
    {
        if (!IsOwner) { return; }
        if (runningDashAttack) { return; }
        currentattacktimer -= Time.deltaTime;
        if (currentattacktimer <= 0 && !iscurrentlyAttacking) { currentattacktimer = 1; Retarget(); }
        else if (currentattacktimer <= 0 && iscurrentlyAttacking) { currentattacktimer = DashCooldownTime; Attack(((Vector2)currentObjectTarget.position - (Vector2)transform.position).normalized); }

        if (iscurrentlyAttacking)
        {
            if (attackingPlayer)
            {
                if (!currentPlayerTarget || currentPlayerTarget.isdead.Value || currentPlayerTarget.PublicInvisBoolean || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > ViewRange) { Retarget(); return; }
            }
            else
            {
                if (!currentObjectTarget || Vector2.Distance(transform.position, currentPlayerTarget.transform.position) > ViewRange) { Retarget(); return; }
            }
            Vector2 direction2 = ((Vector2)currentObjectTarget.position - (Vector2)transform.position).normalized;
            float amount = Vector3.Cross(direction2, transform.up).z;
            rb.angularVelocity = -amount * RotSpeed;
            rb.AddForce(transform.up * MoveSpeed);
            rb.velocity = Vector2.ClampMagnitude(rb.velocity, MoveSpeed);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
}
