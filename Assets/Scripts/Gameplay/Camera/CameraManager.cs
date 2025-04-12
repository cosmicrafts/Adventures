using Cinemachine;
using Netick.Unity;
using Cosmicrafts.Gameplay.Player.Character;
using Cosmicrafts.Gameplay.PlayerManager.LocalPlayer;
using Cosmicrafts.Netick;
using UnityEngine;

namespace Cosmicrafts.Gameplay.Cam.Manager
{
    public class CameraManager : NetickBehaviour, INetickSceneLoaded
    {
        [SerializeField] private CinemachineBrain _cinemachineBrain;
        [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;

        private void OnCharacterSpawned(PlayerCharacter playerCharacter)
        {
            _cinemachineVirtualCamera.Follow = playerCharacter.transform;
        }

        public void OnSceneLoaded(NetworkSandbox sandbox)
        {
            AttachBehaviour(sandbox);

            LocalPlayerManager localPlayerManager = sandbox.GetComponent<LocalPlayerManager>();
            localPlayerManager.OnCharacterSpawned += OnCharacterSpawned;

            if (localPlayerManager.TryGetCharacter(out PlayerCharacter character))
            {
                OnCharacterSpawned(character);
            }
        }
        private void AttachBehaviour(NetworkSandbox sandbox)
        {
#if UNITY_EDITOR
            sandbox.AttachBehaviour(this);
#endif
        }
#if UNITY_EDITOR

        public override void NetworkRender()
        {
            _cinemachineVirtualCamera.enabled = Sandbox.IsVisible;
        }
#endif
    }
}