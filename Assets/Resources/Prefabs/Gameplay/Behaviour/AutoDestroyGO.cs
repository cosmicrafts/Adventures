using UnityEngine;

namespace Cosmicrafts.Gameplay.Behaviour
{
    public class AutoDestroyGO : MonoBehaviour
    {
        [SerializeField] private float _lifetime;

        private void OnEnable()
        {
            Destroy(gameObject, _lifetime);
        }
    }
}
