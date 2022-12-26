using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NonPooledDynamicObjectSpawner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        Debug.Log("Getting my player. My client id: " + NetworkManager.Singleton.LocalClientId + "|| requesting player spawn: " + FindObjectOfType<NetworkManagerUI>().currentCharacter);
        SendCharacterRequestToServerRPC(FindObjectOfType<NetworkManagerUI>().currentCharacter, NetworkManager.Singleton.LocalClientId);

        //if(NetworkManager.Singleton.LocalClientId == 0) { SendMapChangeRequest_ServerRPC(FindObjectOfType<NetworkManagerUI>().currentMap); }
        //SendMapAcquireRequest_ServerRPC();
    }

    [ServerRpc] //server owns this object but client can request a spawn(RequireOwnership = false)
    public void SendCharacterRequestToServerRPC(int charid, ulong clientid)//gets the clientid and characterid required, and spawns a player for that client id. run on the server
    {
        Debug.Log("Server recieved character request: " + charid);
        NetworkObject ob = Instantiate(FindObjectOfType<NetworkManagerUI>().Characters[charid]).GetComponent<NetworkObject>();
        ob.SpawnAsPlayerObject(clientid, true);
        NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject = ob; //set the player object ong
    }

    //[ServerRpc]
    //public void SendMapAcquireRequest_ServerRPC()
    //{
    //    SendMapAcquireRequestToClientRPC();
    //}

    //[ClientRpc]
    //public void SendMapAcquireRequestToClientRPC()//sync maps with all clients to the hosts map
    //{
    //    if(NetworkManager.Singleton.LocalClientId != 0) { return; }
    //    SendMapChangeRequest_ServerRPC(FindObjectOfType<NetworkManagerUI>().currentMap);
    //}

    //[ServerRpc]
    //public void SendMapChangeRequest_ServerRPC(int mapid)
    //{
    //    SendMapAcquireChangeToClientRPC(mapid);
    //}

    //[ClientRpc]
    //public void SendMapAcquireChangeToClientRPC(int mapid)//sync maps with all clients to the hosts map
    //{
    //    FindObjectOfType<NetworkManagerUI>().SetMap(mapid);

    //}
}