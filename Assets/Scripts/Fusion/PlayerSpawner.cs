using Fusion;
using UnityEngine;
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("Prefabs dos Jogadores")]
    public GameObject Player1Prefab; 
    public GameObject Player2Prefab;
    // Contador de jogadores que se juntaram (compartilhado com todo mundo '-')
    private static int playerCount = 0;
    public void PlayerJoined(PlayerRef player)
    {
        if (player != Runner.LocalPlayer)
            return;
 
        Vector3 spawnPos = new Vector3(playerCount * 2, 1, 0);
        // Escolhe o prefab com base no número de jogadores
        GameObject prefabToSpawn = playerCount == 0 ? Player1Prefab : Player2Prefab;
        // Spawna o objeto 
        

        Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
        // Incrementador
        playerCount++;
    }
}
