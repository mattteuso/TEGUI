using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab; // Tag: Player
    [SerializeField] private GameObject player2Prefab; // Tag: Player2

    [Header("Spawn Points")]
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    private void Start()
    {
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        if (player1Prefab != null)
        {
            Instantiate(
                player1Prefab,
                player1SpawnPoint != null ? player1SpawnPoint.position : Vector3.zero,
                Quaternion.identity
            );
        }

        if (player2Prefab != null)
        {
            Instantiate(
                player2Prefab,
                player2SpawnPoint != null ? player2SpawnPoint.position : Vector3.right * 2f,
                Quaternion.identity
            );
        }
    }
}
