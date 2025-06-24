using System.Collections.Generic;
using UnityEngine;

public class EnemyMortor : BaseEnemy
{
    public float targetingRange = 2f;
    public float rotateSpeed = 150f;
    public float fireRate = 2f;
    [SerializeField]
    private List<Transform> _projectileSpawnPoint;
    [SerializeField]
    private GameObject _projectilePrefab;

    private float _fireRateCooldown;
    private int _currentCannonFiring; // max of 4 (element 3 of spawn points)


    protected override void Awake()
    {
        base.Awake();
        _fireRateCooldown = fireRate;
    }
    
    private void Update()
    {
        if (canTarget && Health.IsAlive() && _player && _player.Health.IsAlive())
        {
            if (InFiringRange())
            {
                _fireRateCooldown += Time.deltaTime;

                transform.rotation = Quaternion.Euler(
                    transform.eulerAngles.x, 
                    transform.eulerAngles.y + rotateSpeed * Time.deltaTime,
                    transform.eulerAngles.z);

                HandleFire();
            }
        }
    }

    private bool InFiringRange()
    {
        return Vector3.Distance(transform.position, _player.transform.position) <= targetingRange;
    }

    private void HandleFire()
    {
        if (_fireRateCooldown >= fireRate)
        {
            Fire();
            _fireRateCooldown = 0f;
        }
    }

    private void Fire()
    {
        var spawnPoint = _projectileSpawnPoint[_currentCannonFiring];
        Instantiate(_projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        
        _currentCannonFiring++;
        if (_currentCannonFiring >= _projectileSpawnPoint.Count)
        {
            _currentCannonFiring = 0;
        }
    }
}