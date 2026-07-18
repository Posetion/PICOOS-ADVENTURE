using UnityEngine;
using System.Collections.Generic;
using StarterAssets; // Required to access ThirdPersonController

public class QuicksandVolume : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("How much to multiply the player's speed by. (e.g., 0.3 means 30% of normal speed)")]
    [Range(0.1f, 1f)] public float SpeedMultiplier = 0.35f;

    [Tooltip("Jump strength while in quicksand. 1 = normal jump, 0 = no jump.")]
    [Range(0f, 1f)] public float JumpMultiplier = 0.65f;

    // Dictionary to keep track of multiple players (useful for multiplayer) 
    // and store their original configurations.
    private class PlayerSpeedBackup
    {
        public float OriginalMoveSpeed;
        public float OriginalSprintSpeed;
        public float OriginalJumpHeight;       // Added to track original jump
        public float OriginalDoubleJumpHeight; // Added to track original double jump
    }

    private Dictionary<ThirdPersonController, PlayerSpeedBackup> _affectedPlayers = new Dictionary<ThirdPersonController, PlayerSpeedBackup>();

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
            return;

        if (_affectedPlayers.ContainsKey(player))
            return;

        PlayerSpeedBackup backup = new PlayerSpeedBackup
        {
            OriginalMoveSpeed = player.MoveSpeed,
            OriginalSprintSpeed = player.SprintSpeed,
            OriginalJumpHeight = player.JumpHeight,
            OriginalDoubleJumpHeight = player.DoubleJumpHeight
        };

        _affectedPlayers.Add(player, backup);

        player.MoveSpeed *= SpeedMultiplier;
        player.SprintSpeed *= SpeedMultiplier;
        player.JumpHeight = backup.OriginalJumpHeight * JumpMultiplier;
        player.DoubleJumpHeight = backup.OriginalDoubleJumpHeight * JumpMultiplier;
    }

    private void OnTriggerExit(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player != null)
            ReleasePlayer(player);
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
                player.JumpHeight = backup.OriginalJumpHeight;             // Restore jump
                player.DoubleJumpHeight = backup.OriginalDoubleJumpHeight; // Restore double jump
            }
            _affectedPlayers.Remove(player);
        }
    }

    // Safety measure: if the quicksand gets destroyed while player is inside, 
    // restore the player's attributes first.
    private void OnDisable()
    {
        foreach (var kvp in _affectedPlayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.MoveSpeed = kvp.Value.OriginalMoveSpeed;
                kvp.Key.SprintSpeed = kvp.Value.OriginalSprintSpeed;
                kvp.Key.JumpHeight = kvp.Value.OriginalJumpHeight;             // Restore jump
                kvp.Key.DoubleJumpHeight = kvp.Value.OriginalDoubleJumpHeight; // Restore double jump
            }
        }
        _affectedPlayers.Clear();
    }
}