
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1.5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
