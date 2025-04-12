using Netick.Unity;
using Netick;
using Cosmicrafts.Gameplay.PlayerManager.Global;
using Cosmicrafts.Gameplay.PlayerManager.LocalPlayer;
using Cosmicrafts.Launcher.Prototype;
using System;

namespace Cosmicrafts.Gameplay.Player.Session
{
    [ExecutionOrder(-100)]
    public class PlayerSession : NetworkBehaviour
    {
        [Networked] private NetworkString32 _nickname { get; set; }

        public NetworkString32 Nickname => _nickname;

        public event Action OnNicknameChanged;

        [OnChanged(nameof(_nickname))]
        private void OnChangedNickname(OnChangedData onChangedData)
        {
            OnNicknameChanged?.Invoke();
        }

        [Rpc(RpcPeers.InputSource, RpcPeers.Owner, true)]
        public void RPC_Respawn()
        {
            Sandbox.GetComponent<MatchManager>().SpawnPlayerCharacter(InputSource);
        }

        public void SetNickname(NetworkString32 nickname)
            => _nickname = nickname;

        [Rpc(RpcPeers.InputSource, RpcPeers.Owner, true)]
        public void RPC_SetNickname(NetworkString32 nickname)
            => SetNickname(nickname);

        public override void NetworkStart()
        {
            if (Sandbox.TryGetComponent(out GlobalPlayerManager globalPlayerManager))
                globalPlayerManager.AddPlayerSession(InputSourcePlayerId, this);

            if (!Object.IsInputSource) return;

            if (Sandbox.TryGetComponent(out LocalPlayerManager localPlayerManager))
                localPlayerManager.SessionSpawned(this);
        }
    }
}
