using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Cinemachine;

public class PlayerSwitchManager : MonoBehaviour
{
    [Header("Referências")]
    public List<PlayerInput> playerInputs;
    public List<CinemachineCamera> virtualCameras;

    public string gameplayMap = "Player";
    public string disabledMap = "Disabled";

    private int currentIndex = 0;

    private void Start()
    {
        // Garante que todos mantenham o device pareado
        foreach (var p in playerInputs)
        {
            if (p == null) continue;
            p.SwitchCurrentActionMap(disabledMap);
        }

        UpdateActivePlayer();
    }

    public void OnSwitchCharacter(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        currentIndex = (currentIndex + 1) % playerInputs.Count;
        UpdateActivePlayer();
    }

    private void UpdateActivePlayer()
    {
        for (int i = 0; i < playerInputs.Count; i++)
        {
            if (playerInputs[i] == null) continue;

            bool isActive = (i == currentIndex);

            // troca Action Map, NÃO desliga PlayerInput
            playerInputs[i].SwitchCurrentActionMap(
                isActive ? gameplayMap : disabledMap
            );

            // CÂMERA
            if (virtualCameras.Count > i && virtualCameras[i] != null)
            {
                virtualCameras[i].Priority = isActive ? 20 : 10;

                if (isActive)
                {
                    virtualCameras[i].Follow = playerInputs[i].transform;
                    virtualCameras[i].LookAt = playerInputs[i].transform;
                }
            }
        }
    }
}
