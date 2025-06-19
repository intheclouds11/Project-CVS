using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField]
    private float _lifetime = 2f;

    private void Start()
    {
        var particles = GetComponent<ParticleSystem>();
        if (particles)
        {
            Destroy(gameObject, particles.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(gameObject, _lifetime);
        }
    }
}