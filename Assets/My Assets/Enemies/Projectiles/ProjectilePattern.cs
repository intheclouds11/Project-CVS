using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewProjectilePattern", menuName = "Boss/Projectile Pattern")]
public class ProjectilePattern : ScriptableObject
{
    public string PatternName;

    [Header("Projectile Settings")]
    public GameObject ProjectilePrefab;
    public bool AimAtPlayer;
    public float StartDelay = 3f;
    public float EndDelay = 3f;
    public float FireRate = 0.5f;
    public float Speed = 10f;
    public float FireCount = 10f;
    
    [Header("FX")]
    public AudioClip ChargeSFX;
    public float ChargeVolume = 1f;
    public AudioClip SpawnSFX;
    public float SpawnVolume = 1f;
    public AudioClip CooldownSFX;
    public float CooldownVolume = 1f;

    
    public GameObject Spawn(MultiProjectilePool pool, Transform spawnPoint, Vector3 dirToPlayer)
    {
        var projObj = pool.Get(ProjectilePrefab.name);

        projObj.transform.position = spawnPoint.position;
        projObj.transform.rotation = spawnPoint.rotation;

        var proj = projObj.GetComponent<Projectile>();
        proj.Init(pool, ProjectilePrefab.name);
        projObj.SetActive(true);
        
        var direction = AimAtPlayer ? dirToPlayer : spawnPoint.forward;
        proj.Rb.linearVelocity = direction * Speed;

        var pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySound(proj.transform, SpawnSFX, true, false, SpawnVolume, pitch);
        
        return projObj;
    }
}