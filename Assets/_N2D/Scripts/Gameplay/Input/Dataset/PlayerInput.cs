using Netick;
using UnityEngine;

namespace StinkySteak.N2D.Gameplay.PlayerInput
{
    public struct PlayerCharacterInput : INetworkInput
    {
        public float HorizontalMove;
        public float VerticalMove;    // Add vertical movement support
        public bool Jump;
        public bool IsFiring;
        public float LookDegree;
        public bool ActivateRegenerativeShield;
        public Vector2 TargetPosition;
    }
}
