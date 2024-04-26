﻿using Unity.Netcode;
using UnityEngine;

namespace GiantSpecimens.src;
internal class Utils : NetworkBehaviour
{
    static int seed = 0;
    static System.Random random = new System.Random(seed + 85);
    internal static Utils Instance { get; set; }

    void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = true)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position) {
        if (StartOfRound.Instance == null)
        {
            return;
        }
        if (random == null)
        {
            seed = StartOfRound.Instance.randomMapSeed;
            random = new System.Random(seed + 85);
        }

        if(itemName.Length == 0)
        {
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out var item);
        if (item == null)
        {
            return;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up, Quaternion.identity);
        int value = random.Next(minValue: item.minValue, maxValue: item.maxValue);
        var scanNode = go.gameObject.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        go.GetComponent<NetworkObject>().Spawn(false);
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value) {
        go.TryGet(out NetworkObject netObj);
        if(netObj != null)
        {
            var scanNode = netObj.GetComponentInChildren<ScanNodeProperties>();
            scanNode.scrapValue = value;
            scanNode.subText = $"Value: ${value}";
        }
    }
}