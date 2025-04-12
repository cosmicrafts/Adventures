using UnityEngine;

public class Rotator : MonoBehaviour
{
    private void Start()
    {
        float randomZRotation = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0, 0, randomZRotation);
    }
}
