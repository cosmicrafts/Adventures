using Netick;
using Netick.Unity;
using Cosmicrafts.Finder;
using Cosmicrafts.Gameplay.Player.Character;
using Cosmicrafts.Gameplay.Player.Session;
using Cosmicrafts.Gameplay.PlayerManager.Global;
using Cosmicrafts.Gameplay.Spawnpoints;
using Cosmicrafts.Netick;
using System.Collections.Generic;
using UnityEngine;
using NetworkPlayer = Netick.NetworkPlayer;

namespace Cosmicrafts.Launcher.Prototype
{
    public class MatchManager : NetworkEventsListener
    {
        [SerializeField] private NetworkObject _playerSessionPrefab;
        [SerializeField] private NetworkObject _playerCharacterPrefab;
        private SpawnPoints _spawnpoints;

        [System.Obsolete("Use NewMethodName instead")]
        public override void OnSceneLoaded(NetworkSandbox sandbox)
        {
            List<INetickSceneLoaded> listeners = sandbox.FindObjectsOfType<INetickSceneLoaded>();

            foreach (INetickSceneLoaded listener in listeners)
                listener.OnSceneLoaded(sandbox);

            if (!sandbox.IsServer) return;

            _spawnpoints = Object.FindFirstObjectByType<SpawnPoints>();

            if (_spawnpoints == null)
            {
                Sandbox.LogError("Spawn points not found in the scene.");
                return;
            }

            SpawnPlayerSession(sandbox.LocalPlayer);
            SpawnPlayerCharacter(sandbox.LocalPlayer);
        }

        private void SpawnPlayerSession(NetworkPlayer networkPlayer)
        {
            NetworkObject obj = Sandbox.NetworkInstantiate(_playerSessionPrefab.gameObject, Vector3.zero, Quaternion.identity, networkPlayer);

            if (obj.TryGetComponent(out PlayerSession session))
            {
                session.SetNickname($"Bot{Random.Range(100, 999)}");
            }
        }

        public void SpawnPlayerCharacter(NetworkPlayer player)
        {
            bool isPlayerExist = Sandbox.GetComponent<GlobalPlayerManager>().IsCharacterExist(player.PlayerId);

            if (isPlayerExist) return;

            Vector3 nextPosition = _spawnpoints.GetNext().position;

            Sandbox.NetworkInstantiate(_playerCharacterPrefab.gameObject, nextPosition, Quaternion.identity, player);
        }

        public override void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
        {
            SpawnPlayerSession(client);
            SpawnPlayerCharacter(client);
        }

        public override void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason transportDisconnectReason)
        {
            DespawnPlayerCharacter(client);
            DespawnPlayerSession(client);
        }

        private void DespawnPlayerSession(NetworkPlayer networkConnection)
        {
            GlobalPlayerManager playerManager = Sandbox.GetComponent<GlobalPlayerManager>();

            if (playerManager.TryGetSession(networkConnection.PlayerId, out PlayerSession session))
                Sandbox.Destroy(session.Object);
        }

        private void DespawnPlayerCharacter(NetworkPlayer networkConnection)
        {
            GlobalPlayerManager playerManager = Sandbox.GetComponent<GlobalPlayerManager>();

            if (playerManager.TryGetCharacter(networkConnection.PlayerId, out PlayerCharacter character))
                Sandbox.Destroy(character.Object);
        }
    }
}
