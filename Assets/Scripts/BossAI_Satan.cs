using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BossAI_Satan : NetworkBehaviour
{
    public float MoveSpeed, RotSpeed, DashSpeed, DashCooldownTime, ViewRange, projectileSpeed;
    public Test_DamageTrigger SpinDamageTrigger;
    public GameObject projectile;
    public Transform[] ShootPoints;
    public Rigidbody2D rb;

    PlayerMovement currentPlayerTarget;
    Transform currentObjectTarget;
    bool iscurrentlyAttacking, attackingPlayer, freewhileattack;
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

    void RunAttack(Vector2 direction)
    {
        float value = Random.value;
        if(value < 0.4f) { currentattacktimer = DashCooldownTime; StartCoroutine(DashAttack(direction)); }
        else if(value < 0.8f) { currentattacktimer = DashCooldownTime / 2; StartCoroutine(FireFromShooters()); }
        else { currentattacktimer = DashCooldownTime; StartCoroutine(EncirclePlayers()); }
    }

    IEnumerator DashAttack(Vector2 direction)
    {
        freewhileattack = true;
        rb.AddForce(direction * DashSpeed);
        rb.AddTorque(Random.Range(-20f, 20f));
        SpinDamageTrigger.enabled = true;
        foreach (var shooter in ShootPoints)
        {
            Fire_ServerRPC(shooter.up, shooter.position);
        }
        yield return new WaitForSeconds(3);
        SpinDamageTrigger.enabled = false;
        freewhileattack = false;
    }

    IEnumerator EncirclePlayers() //fire in a spiral
    {
        Vector2 target = NetworkManager.Singleton.ConnectedClientsList[Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.transform.position;
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = target + (Random.insideUnitCircle.normalized * 3f);
            Fire_ServerRPC(target - pos, pos);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator FireFromShooters() //fire in a spiral
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (var shooter in ShootPoints)
            {
                Fire_ServerRPC(shooter.up, shooter.position);
                yield return new WaitForSeconds(0.05f);
            }
        }
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

    private void Update()
    {
        if (!IsOwner) { return; }
        currentattacktimer -= Time.deltaTime;
        if (currentattacktimer <= 0 && !iscurrentlyAttacking) { currentattacktimer = 1; Retarget(); }
        else if (currentattacktimer <= 0 && iscurrentlyAttacking) { RunAttack(((Vector2)currentObjectTarget.position - (Vector2)transform.position).normalized); }

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
            if(!freewhileattack) //don't move boss with AI while attacking
            {
                rb.angularVelocity = -amount * RotSpeed; //make sure its always turning so it looks more shaky
                rb.AddForce(transform.up * MoveSpeed);
                rb.velocity = Vector2.ClampMagnitude(rb.velocity, MoveSpeed);
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
}
