using UnityEngine;

public class EnemyTurret : BaseEnemy
{
    public float targetingRange = 2f;
    public float rotateSpeed = 3f;
    public float fireRate = 2f;
    public Transform turretTop;
    [SerializeField]
    private Transform _projectileSpawnPoint;
    [SerializeField]
    private GameObject _projectilePrefab;

    private float _lastFireTime;
    

    private void Update()
    {
        if (canTarget && Health.IsAlive() && _player && _player.Health.IsAlive())
        {
            if (InFiringRange())
            {
                var direction = _player.transform.position - transform.position;
                var directionYaw = new Vector3(direction.x, 0f, direction.z);
                var lookRotation = Quaternion.LookRotation(directionYaw);
                turretTop.rotation = Quaternion.RotateTowards(turretTop.rotation, lookRotation, rotateSpeed);

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
        if (Time.time >= _lastFireTime + fireRate)
        {
            Fire();
            _lastFireTime = Time.time;
        }
    }

    private void Fire()
    {
        Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
    }
}