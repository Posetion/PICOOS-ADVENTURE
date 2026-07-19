using UnityEngine;
using System.Collections.Generic;
using StarterAssets; // Required to access ThirdPersonController

public class QuicksandVolume : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("How much to multiply the player's speed by. (e.g., 0.3 means 30% of normal speed)")]
    [Range(0.1f, 1f)] public float SpeedMultiplier = 0.35f;

    // Dictionary to track multiple players and store their original configurations.
    private class PlayerSpeedBackup
    {
        public float OriginalMoveSpeed;
        public float OriginalSprintSpeed;
        public float OriginalJumpHeight;
        public float OriginalDoubleJumpHeight;
    }

    private Dictionary<ThirdPersonController, PlayerSpeedBackup> _affectedPlayers = new Dictionary<ThirdPersonController, PlayerSpeedBackup>();

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
            return;

        if (_affectedPlayers.ContainsKey(player))
            return;

        // Backup the player's current values
        PlayerSpeedBackup backup = new PlayerSpeedBackup
        {
            OriginalMoveSpeed = player.MoveSpeed,
            OriginalSprintSpeed = player.SprintSpeed,
            OriginalJumpHeight = player.JumpHeight,
            OriginalDoubleJumpHeight = player.DoubleJumpHeight
        };

        _affectedPlayers.Add(player, backup);

        // Apply quicksand penalties
        player.MoveSpeed *= SpeedMultiplier;
        player.SprintSpeed *= SpeedMultiplier;

        // Completely disable jumps by turning heights to 0
        player.JumpHeight = 0f;
        player.DoubleJumpHeight = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player != null)
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
                // Restore original values safely when exiting the volume
                player.MoveSpeed = backup.OriginalMoveSpeed;
                player.SprintSpeed = backup.OriginalSprintSpeed;
                player.JumpHeight = backup.OriginalJumpHeight;
                player.DoubleJumpHeight = backup.OriginalDoubleJumpHeight;
            }
            _affectedPlayers.Remove(player);
        }
    }

    // Safety measure: if the quicksand volume gets disabled or destroyed while the player is inside, 
    // restore the player's attributes.
    private void OnDisable()
    {
        foreach (var kvp in _affectedPlayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.MoveSpeed = kvp.Value.OriginalMoveSpeed;
                kvp.Key.SprintSpeed = kvp.Value.OriginalSprintSpeed;
                kvp.Key.JumpHeight = kvp.Value.OriginalJumpHeight;
                kvp.Key.DoubleJumpHeight = kvp.Value.OriginalDoubleJumpHeight;
            }
        }
        _affectedPlayers.Clear();
    }
}