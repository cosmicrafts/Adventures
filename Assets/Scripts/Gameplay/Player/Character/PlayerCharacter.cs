using Netick;
using Netick.Unity;
using Cosmicrafts.Gameplay.PlayerManager.Global;
using Cosmicrafts.Gameplay.PlayerManager.LocalPlayer;

namespace Cosmicrafts.Gameplay.Player.Character
{
    [ExecutionOrder(-99)]
    public class PlayerCharacter : NetickBehaviour
    {
        public int InputSourcePlayerId => Entity.InputSourcePlayerId;

        private int _inputSourcePlayerId;
        private bool _isInputSource;

        public override void NetworkStart()
        {
            if (Sandbox.TryGetComponent(out GlobalPlayerManager globalPlayerManager))
                globalPlayerManager.AddPlayerCharacter(InputSourcePlayerId, this);

            _inputSourcePlayerId = InputSourcePlayerId;
            _isInputSource = Object.IsInputSource;

            if (!Object.IsInputSource) return;

            if (Sandbox.TryGetComponent(out LocalPlayerManager localPlayerManager))
                localPlayerManager.CharacterSpawned(this);
        }

        public override void NetworkDestroy()
        {
            if (_isInputSource)
            {
                if (Sandbox.TryGetComponent(out LocalPlayerManager localPlayerManager))
                    localPlayerManager.CharacterDespawned();
            }

            if (Sandbox.TryGetComponent(out GlobalPlayerManager globalPlayerManager))
                globalPlayerManager.RemovePlayerCharacter(_inputSourcePlayerId);
        }
    }
}
