using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    Rigidbody2D rb;
    public float Damage, ProjectileRadius;
    public bool IncreaseDamageInFlight;
    public GameObject spawneddamagearea, hitfx;
    public ulong ShotBy;
    public TeamStatus shotbyteam;
    public bool ShotByInvis, IsEnemyProj;
    public LayerMask mask;
    float dmginctimer;
    public const float SniperProjectileIncreaseWhileInvisMult = 3f;
    //public Collider2D ignoredCollider;
    bool hit;
    Vector2 startpos;

    private void Start()
    {
        //rb = GetComponent<Rigidbody2D>();
        hit = false;
        //if (IsOwner) { invismult.Value = GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().PublicInvisBoolean; gameObject.name = Local_NameSetter; OnDoDamageFN.AddListener(GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().OnDoDamage); AddVelocity(((Vector2)transform.position - (Vector2)GameObject.Find("LocalPlayer").transform.position).normalized * ProjectileSpeed * (invismult.Value ? SniperProjectileIncreaseWhileInvisMult : 1)); Debug.Log(invismult); } //works because the projectile is spawned with ownership to the player who shot it
        if (IsOwner) { startpos = transform.position; }
    }

    private void Update()
    {
        //does this for every instance of the projectile because Damage isn't synced

        if (!IsOwner) { return; }
        if (hit) { return; }
        if (IncreaseDamageInFlight) //increase damage in flight only has to run on the owner
        {
            Damage = Vector2.Distance(transform.position, startpos) / 3f; //at 15m, it will do 5dmg
        }
        Collider2D coll = Physics2D.OverlapCircle(transform.position, ProjectileRadius, mask); //check for any player in radius
        if (coll && coll.GetComponent<PlayerMovement>())
        {
            if(coll.GetComponent<PlayerMovement>().OwnerClientId == ShotBy && !IsEnemyProj) { return; }
            if (!IsEnemyProj && shotbyteam != TeamStatus.Noteam && shotbyteam != TeamStatus.Mate && shotbyteam == coll.GetComponent<PlayerMovement>().CurrentTeam.Value) { return; } //if i'm on a team and my team matches the hit team, don't do anything
            if(shotbyteam == TeamStatus.Mate && coll.GetComponent<PlayerMovement>().CurrentTeam.Value != TeamStatus.Outsider) { DoDamageToTarget_ServerRPC(ShotBy, ShotBy, 999); return; } //nuke myself if i'm playing amogus and the target isnt a mate
            hit = true;
            if (spawneddamagearea) { SpawnDamageArea_ServerRPC(transform.position, ShotBy, shotbyteam); }
            SpawnHitFX_ServerRPC();
            if(!IsEnemyProj && !coll.GetComponent<PlayerMovement>().isdead.Value && coll.GetComponent<PlayerMovement>().currenthp.Value - Damage <= 0) { SendKillToDamager_ServerRPC(ShotBy, false); }
            DoDamageToTarget_ServerRPC(ShotBy, coll.GetComponent<PlayerMovement>().OwnerClientId, Damage);
            DestroyThisGameobjecy_ServerRPC();
        } //send serverrpc to damage player, and destroy projectile
        else if (coll && coll.tag == "Wall")
        {
            hit = true;
            if (spawneddamagearea) { SpawnDamageArea_ServerRPC(transform.position, ShotBy, shotbyteam); }
            SpawnHitFX_ServerRPC();
            DestroyThisGameobjecy_ServerRPC();
        }
        else if(coll && coll.GetComponent<BuildingHealth>()) //when a building (made by concave) is hit, then damage it using its server RPC
        {
            if(coll.GetComponent<BuildingHealth>().EngineerBuilding)
            {
                if(coll.GetComponent<BuildingHealth>().placedBy == ShotBy) { return; }
                if (!IsEnemyProj && shotbyteam != TeamStatus.Noteam && shotbyteam == coll.GetComponent<BuildingHealth>().placedbyteam) { return; } //if i'm on a team and my team matches the hit buildings team, don't do anything
                hit = true;
                if (spawneddamagearea) { SpawnDamageArea_ServerRPC(transform.position, ShotBy, shotbyteam); }
                coll.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(Damage / 2);
                SpawnHitFX_ServerRPC();
                DestroyThisGameobjecy_ServerRPC();
            }
            else
            {
                if (IsEnemyProj) { return; } //enemy projectiles ignore enemies
                hit = true;
                if (spawneddamagearea) { SpawnDamageArea_ServerRPC(transform.position, ShotBy, shotbyteam); }
                if (coll.GetComponent<BuildingHealth>().IsBoss) //give normal kill on boss kill
                {
                    if (coll.GetComponent<BuildingHealth>().currenthealth.Value - Damage <= 0) { SendKillToDamager_ServerRPC(ShotBy, false); } //give self a kill if I deserve one
                }
                else
                {
                    if (coll.GetComponent<BuildingHealth>().currenthealth.Value - Damage <= 0) { SendKillToDamager_ServerRPC(ShotBy, true); } //give self a kill if I deserve one
                }
                coll.GetComponent<BuildingHealth>().TakeDamage_ServerRPC(Damage);
                SpawnHitFX_ServerRPC();
                DestroyThisGameobjecy_ServerRPC();
            }
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
        //if (!IsEnemyProj && didkill) { Debug.Log(damager); SendKillToDamager_ServerRPC(damager); } //if kill is detected, then send the kill request serverrpc (ignored if its not a player projectile)
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

    public void AddVelocity(Vector2 velocity)
    {
        rb.velocity = velocity;
    }

    [ServerRpc] //server side damage area spawning
    void SpawnDamageArea_ServerRPC(Vector2 pos, ulong id, TeamStatus team)
    {
        NetworkObject ob = Instantiate(spawneddamagearea, pos, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        ob.GetComponent<Test_DamageTrigger>().whoShot = id;
        ob.GetComponent<Test_DamageTrigger>().myteam = team;
        Destroy(ob.gameObject, 10);
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC()
    {
        NetworkObject ob = Instantiate(hitfx, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if(collision.collider.GetComponent<PlayerMovement>())
    //    {
    //        bool didkill;
    //        collision.collider.GetComponent<PlayerMovement>().TakeDamage(Damage, out didkill);
    //        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().StartCoroutine(frameWaiter(collision.collider)); //this object is getting destroyed, run coroutine on smth else
    //    }
    //    StartCoroutine(destroywaiter());
    //}

    //IEnumerator destroywaiter()
    //{
    //    yield return new WaitForSeconds(0.4f);
    //    DestroyThisGameobjecy_ServerRPC(); //destroy later to ensure hit 
    //}

    [ServerRpc(RequireOwnership = false)]
    void DestroyThisGameobjecy_ServerRPC()
    {
        Destroy(gameObject);
    }
}
