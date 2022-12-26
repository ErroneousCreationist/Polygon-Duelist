using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Rammer : NetworkBehaviour
{
    public Transform top, bottom, rammer;
    public float movetime, cooldown;
    bool locked, isattop;
    NetworkVariable<Vector3> rammerpos = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rammerpos.Value = bottom.position;
    }

    IEnumerator MoveRammer()
    {
        locked = true;
        Vector3 pos = rammerpos.Value;
        Vector3 nextpos = isattop ? bottom.position : top.position;
        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / movetime;
            rammerpos.Value = Vector3.Lerp(pos, nextpos, t);
            yield return null;
        }
        rammerpos.Value = nextpos;
        isattop = !isattop;
        StartCoroutine(LockTime());
    }

    IEnumerator LockTime()
    {
        yield return new WaitForSeconds(cooldown);
        locked = false;
    }

    private void Update()
    {
        rammer.position = rammerpos.Value; //synced rammer position
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }
        if((collision.CompareTag("Player") || collision.CompareTag("LocalPlayer")) && !locked)
        {
            StartCoroutine(MoveRammer());
        }
    }
}
