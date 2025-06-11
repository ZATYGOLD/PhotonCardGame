using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerSpawner : MonoBehaviourPun
{
    public static PlayerSpawner Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private RectTransform spawnPoint;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (!ValidateElements()) return;
    }

    private bool ValidateElements()
    {
        if (playerPrefab == null)
        { Debug.LogError("PlayerPrefab is not assigned in the Inspector", this); return false; }

        if (spawnPoint == null)
        { Debug.LogError("SpawnPoint is not assigned in the Inspector", this); return false; }

        return true;
    }

    public void SpawnLocalPlayer()
    {
        GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.localPosition, Quaternion.identity);
        playerObject.transform.SetParent(spawnPoint, false);

        //TODO: RPC local to remote's opponent side
    }
}