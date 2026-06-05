using UnityEngine;
using System.Collections.Generic;
using StarterAssets; // Required to access ThirdPersonController

public class QuicksandVolume : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("How much to multiply the player's speed by. (e.g., 0.3 means 30% of normal speed)")]
    [Range(0.1f, 1f)] public float SpeedMultiplier = 0.35f;

    // Dictionary to keep track of multiple players (useful for multiplayer) 
    // and store their original configurations.
    private class PlayerSpeedBackup
    {
        public float OriginalMoveSpeed;
        public float OriginalSprintSpeed;
    }

    private Dictionary<ThirdPersonController, PlayerSpeedBackup> _affectedPlayers = new Dictionary<ThirdPersonController, PlayerSpeedBackup>();

    private void OnTriggerEnter(Collider other)
    {
        // Check if the overlapping object has the ThirdPersonController
        if (other.TryGetComponent<ThirdPersonController>(out var player))
        {
            // If they aren't already registered, trap them
            if (!_affectedPlayers.ContainsKey(player))
            {
                // Backup original values
                PlayerSpeedBackup backup = new PlayerSpeedBackup
                {
                    OriginalMoveSpeed = player.MoveSpeed,
                    OriginalSprintSpeed = player.SprintSpeed
                };
                
                _affectedPlayers.Add(player, backup);

                // Apply the slowdown modifier
                player.MoveSpeed *= SpeedMultiplier;
                player.SprintSpeed *= SpeedMultiplier;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<ThirdPersonController>(out var player))
        {
            ReleasePlayer(player);
        }
    }

    private void ReleasePlayer(ThirdPersonController player)
    {
        if (_affectedPlayers.TryGetValue(player, out var backup))
        {
            if (player != null)
            {
                // Restore original values safely
                player.MoveSpeed = backup.OriginalMoveSpeed;
                player.SprintSpeed = backup.OriginalSprintSpeed;
            }
            _affectedPlayers.Remove(player);
        }
    }

    // Safety measure: if the quicksand gets destroyed while player is inside, 
    // restore the player's speed first.
    private void OnDisable()
    {
        foreach (var kvp in _affectedPlayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.MoveSpeed = kvp.Value.OriginalMoveSpeed;
                kvp.Key.SprintSpeed = kvp.Value.OriginalSprintSpeed;
            }
        }
        _affectedPlayers.Clear();
    }
}