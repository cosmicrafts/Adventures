using Netick;
using Netick.Unity;
using StinkySteak.Netick.Timer;
using UnityEngine;
using System;
using StinkySteak.N2D.Gameplay.PlayerInput;
using StinkySteak.N2D.Gameplay.Player.Character.Health;
using StinkySteak.N2D.Gameplay.Player.Character.Skills;

public class EnemyAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public float rotationSpeed = 360f; // Speed at which the enemy rotates toward the player

    [Header("Combat Settings")]
    public float attackCooldown = 1f; // Cooldown between attacks
    public float shootingRange = 10f; // Range at which the enemy starts shooting
    private float nextAttackTime = 0f;
    public Transform spriteTransform;

    private Transform player;
    private Rigidbody2D rb;
    private PlayerCharacterHealth healthComponent;

    public override void NetworkStart()
    {
        // Assuming player is the camera's transform for this example
        player = Camera.main.transform;
        rb = GetComponent<Rigidbody2D>();
        healthComponent = GetComponent<PlayerCharacterHealth>();
    }

    private void FixedUpdate()
    {
        if (player != null)
        {
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
                TryAttack();
            }
        }
    }

    private void RotateTowardsPlayer(Vector2 direction)
    {
        if (spriteTransform != null)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 0;
            float smoothedAngle = Mathf.LerpAngle(spriteTransform.eulerAngles.z, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            spriteTransform.rotation = Quaternion.Euler(0, 0, smoothedAngle);
        }
    }

    private void MoveTowardsPlayer(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }

    private void TryAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            // Placeholder for attack mechanism, could be replaced with shooting or melee attack logic later
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    public void TakeDamage(float damage)
    {
        if (healthComponent != null)
        {
            healthComponent.DeductShieldAndHealth(damage, player); // Deducts damage using shield and health
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // if (collision.gameObject.CompareTag("PlayerBullet"))
        // {
        //     // Example: assuming PlayerBullet has a Damage property
        //     if (collision.gameObject.TryGetComponent(out PlayerBullet bullet))
        //     {
        //         TakeDamage(bullet.Damage); // Apply damage from bullet
        //         Destroy(collision.gameObject); // Destroy bullet on impact
        //     }
        // }
    }
}
