using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 2f;
    public float rotationSpeed = 360f; // Speed at which the enemy rotates toward the player
    public int maxHealth = 10; // Enemy health
    public float attackCooldown = 1f; // Cooldown between attacks
    public GameObject bulletPrefab; // Bullet prefab for shooting
    public Transform shootPoint; // Where the bullets are shot from
    public float bulletSpeed = 10f; // Speed of the bullet
    public float shootingRange = 10f; // Range at which the enemy starts shooting

    private Transform player;
    private Rigidbody2D rb;
    private float nextAttackTime = 0f;
    private int currentHealth;

    private void Start()
    {
        player = Camera.main.transform; // Assume player is the camera target
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (player != null)
        {
            // Get direction to the player
            Vector2 direction = (player.position - transform.position).normalized;

            // Rotate smoothly towards the player
            RotateTowardsPlayer(direction);

            // Move towards the player
            MoveTowardsPlayer(direction);

            // Prevent physics-based rotation from affecting the enemy
            rb.angularVelocity = 0f;

            // Check distance and attack player if close enough
            if (Vector2.Distance(transform.position, player.position) <= shootingRange)
            {
                TryShoot();
            }
        }
    }

    // Rotate the enemy towards the player
    private void RotateTowardsPlayer(Vector2 direction)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float smoothedAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothedAngle);
    }

    // Move the enemy towards the player
    private void MoveTowardsPlayer(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }

    // Shooting mechanism
    private void TryShoot()
    {
        if (Time.time >= nextAttackTime)
        {
            Shoot();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void Shoot()
    {
      //   // Instantiate the bullet and shoot it
      //   GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
      //   Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
      //   bulletRb.linearVelocity = shootPoint.up * bulletSpeed;

      //   // Destroy the bullet after a while to prevent memory leaks
      //   Destroy(bullet, 3f); // Destroy after 3 seconds
    }

    // Enemy takes damage when hit
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Destroy the enemy
    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
      //   // Check for player bullets
      //   if (collision.gameObject.CompareTag("Bullet"))
      //   {
      //       Bullet bullet = collision.gameObject.GetComponent<Bullet>();
      //       if (bullet != null)
      //       {
      //           TakeDamage(bullet.damage); // Apply damage from bullet
      //           Destroy(collision.gameObject); // Destroy the bullet on impact
      //       }
      //   }
    }
}
