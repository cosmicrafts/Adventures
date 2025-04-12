using Netick;
using Cosmicrafts.Gameplay.Player.Character;
using Cosmicrafts.Gameplay.Player.Session;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Cosmicrafts.Gameplay.PlayerManager.LocalPlayer
{
    public class LocalPlayerManager : MonoBehaviour
    {
        private PlayerSession _session;
        private PlayerCharacter _character;
        public PlayerSession Session => _session;
        public PlayerCharacter Character => _character;


        public event Action<PlayerCharacter> OnCharacterSpawned;
        public event Action OnCharacterDespawned;
        public event Action<PlayerSession> OnSessionSpawned;

        public bool TryGetCharacter(out PlayerCharacter character)
        {
            character = _character;
            return _character != null;
        }

        public void CharacterSpawned(PlayerCharacter playerCharacter)
        {
            _character = playerCharacter;
            OnCharacterSpawned?.Invoke(playerCharacter);
        }
        public void CharacterDespawned()
        {
            OnCharacterDespawned?.Invoke();
        }
        public void SessionSpawned(PlayerSession playerSession)
        {
            _session = playerSession;
            OnSessionSpawned?.Invoke(playerSession);
        }
    }
}