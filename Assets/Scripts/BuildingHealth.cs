using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BuildingHealth : NetworkBehaviour
{
    public UnityEngine.UI.Slider HB;
    public ulong placedBy;
    public TeamStatus placedbyteam;
    public bool EngineerBuilding, InactiveOnDeath;
    public float Maxhealth, MaterialWorth;
    [HideInInspector]public NetworkVariable<float> currenthealth = new NetworkVariable<float>(), maxHealth = new NetworkVariable<float>();
    [HideInInspector] public NetworkVariable<bool> inpocketdimension = new NetworkVariable<bool>();
    public GameObject dieeffect;
    public bool ResetTeleposOnDie;
    public bool IsBoss;

    [Header("Scale Health (bosses)")]
    public bool ScaleHealth;
    public float BaseHealth, AddedPerPlayer;

    private void Start()
    {
        if (!IsOwner) { return; }
        currenthealth.Value = Maxhealth;
        maxHealth.Value = Maxhealth;
        if (ScaleHealth)
        {
            maxHealth.Value = BaseHealth + (AddedPerPlayer * NetworkManager.Singleton.ConnectedClientsList.Count - 1);
            currenthealth.Value = BaseHealth + (AddedPerPlayer * NetworkManager.Singleton.ConnectedClientsList.Count - 1); //with 1 player, the health will be equal to base health, 2 players base health + addedperplayer, so on
        }
    }

    [ServerRpc]
    void ResetTeleport_ServerRPC(ulong targetid)
    {
        //if(targetid == 0) { GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().teleportpos = Vector2.zero; return; }
        ResetTelepos_ClientRPC(new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { targetid } } });
    }

    [ClientRpc]
    void ResetTelepos_ClientRPC(ClientRpcParams clientparams)
    {
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().teleportpos = Vector2.zero; //reset the telepos
    }

    private void Update()
    {
        if (!HB) { return; }
        HB.value = currenthealth.Value;
        HB.maxValue = maxHealth.Value;
        if (!IsOwner) { return; }
        inpocketdimension.Value = transform.position.y < -30;
        if (inpocketdimension.Value) { TakeDamage_ServerRPC(0.0015f); } //take constant damage in pocket dimension
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendToPocketDimension_ServerRPC()
    {
        transform.position = new Vector2(0, -55);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamage_ServerRPC(float amount) //take damage SERVER RPC, CALLED FROM ANY VERSION OF THE OBJECT, AND THIS OBJECT IS OWNED BY THE SERVER
    {
        if (!IsOwner) { return; }
        currenthealth.Value -= amount;
        if(currenthealth.Value <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (ResetTeleposOnDie) { ResetTeleport_ServerRPC(placedBy); }
        SpawnHitFX_ServerRPC();
        if (InactiveOnDeath)
        {
            gameObject.SetActive(false);
            return;
        }
        DestroyThisGameobject_ServerRPC();
    }

    [ServerRpc]
    void DestroyThisGameobject_ServerRPC() //destroy the object serverside
    {
        Destroy(gameObject);
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC()
    {
        NetworkObject ob = Instantiate(dieeffect, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }
}
