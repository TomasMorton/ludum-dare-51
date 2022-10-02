using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Unity References")] public Animator animator;

        public PlayerInput playerInput;
        private PlayerWeapon _weapon;
        public AudioClip attackSound;

        /*
     * Player health.
     */
        [Header("Health")] [Tooltip("Starting player health.")] [SerializeField]
        protected int healthInitial = 5;

        [Tooltip("How much player health increases per upgrade level.")] [SerializeField]
        protected int healthGrowthPerLevel = 1;

        [Tooltip("How many times the player can upgrade health.")] [SerializeField]
        protected int healthMaxLevel = 5;

        protected int healthLevel = 0;

        // Use me for calculations.
        protected int healthMax;
        protected int healthActual;

        /*
     * Player attack damage.
     */
        [Header("Attack Damage")]
        [Tooltip("How much damage the player deals to enemies per swing attack.")]
        [SerializeField]
        protected float attackDamageInitial = 1.0F;

        [Tooltip("By how much the player's attack damage increases per level.")] [SerializeField]
        protected float attackDamageGrowthPerLevel = 0.2F;

        [Tooltip("How many times the player can upgrade attack damage.")] [SerializeField]
        protected int attackDamageMaxLevel = 5;

        protected int attackDamageLevel = 0;

        // Use me for calculations.
        protected float attackDamageActual;

        /*
     * Player attack speed.
     */
        [Header("Attack Speed")]
        [Tooltip("How many times per second that the player can attack with their weapon.")]
        [SerializeField]
        protected float attackSpeedInitial = 2.0F;

        [Tooltip("By how much the player's attack speed increases per level.")] [SerializeField]
        protected float attackSpeedGrowthPerLevel = 0.667F;

        [Tooltip("How many times the player can upgrade attack speed.")] [SerializeField]
        protected int attackSpeedMaxLevel = 5;

        protected int attackSpeedLevel = 0;

        // Use me for calculations.
        protected float attackSpeedActual;

        // Used to calculate how long it has been since the last attack.
        protected float timeOfLastAttack = 0.0F;

        /*
     * Player attack range.
     */
        [Header("Attack Range")]
        [Tooltip("How far in game units that the player can reach enemies with their weapon.")]
        [SerializeField]
        protected float attackRangeInitial = 100.0F;

        [Tooltip("By how much the player's attack range increases per level.")] [SerializeField]
        protected float attackRangeGrowthPerLevel = 12.0F;

        [Tooltip("How many times the player can upgrade attack range.")] [SerializeField]
        protected int attackRangeMaxLevel = 5;

        protected int attackRangeLevel = 0;

        // Use me for calculations.
        protected float attackRangeActual;

        private bool _playing = true;

        /*
         * Upgrade Costs.
         */
        [Header("Upgrade Costs")]
        [SerializeField][Tooltip("How much currency it costs to upgrade from level 0 to level 1")]
        public int firstUpgradeCost = 3;
        [SerializeField][Tooltip("How much currency it costs to upgrade from level 1 to level 2")]
        public int secondUpgradeCost = 7;
        [SerializeField][Tooltip("How much currency it costs to upgrade from level 2 to level 3")]
        public int thirdUpgradeCost = 12;
        [SerializeField][Tooltip("How much currency it costs to upgrade from level 3 to level 4")]
        public int fourthUpgradeCost = 18;
        [SerializeField][Tooltip("How much currency it costs to upgrade from level 4 to level 5")]
        public int fifthUpgradeCost = 25;


        // True if the player is trying to attack.
        protected bool attacking = false;

        // True if the player can't attack because they have recently attacked.
        protected bool attackOnCooldown = false;

        //Direction of the attack
        protected Vector2 playerAttackDirection = Vector2.zero;

        public enum FacingDirection
        {
            Up,
            Down,
            Left,
            Right,
        }

        private FacingDirection facingDirection = FacingDirection.Up;

        public FacingDirection GetFacingDirection()
        {
            return facingDirection;
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (!animator) GetComponent<Animator>();
            _weapon = gameObject.GetComponentInChildren<PlayerWeapon>();
            if (!playerInput) playerInput = GetComponent<PlayerInput>();

            RecalculateStats();
        }

        // Update is called once per frame
        private void Update()
        {
            // Check if the player can be moved.
            if (Controllers.GameController.IsPlayerInputEnabled)
            {
                playerAttackDirection = Vector2.zero;

                if (Controllers.InputController.isMobile) //Mobile Controls
                {
                    //Get Input for playerAttack joystick
                    playerAttackDirection = playerInput.actions["Attack"].ReadValue<Vector2>();

                    //Only if stick is in use
                    //Player is attacking
                    if (playerAttackDirection != Vector2.zero)
                    {
                        //Face direction and Attack!
                        animator.SetFloat("Horizontal", playerAttackDirection.x);
                        animator.SetFloat("Vertical", playerAttackDirection.y);

                        //Horizontal
                        if (playerAttackDirection.x < 0) facingDirection = FacingDirection.Left;
                        else facingDirection = FacingDirection.Right;

                        //Vertical
                        if (playerAttackDirection.y < 0) facingDirection = FacingDirection.Down;
                        else facingDirection = FacingDirection.Up;

                        attacking = true;
                        // facingDirection = CalculateFacingDirection(playerAttackDirection);
                    }
                }
                // Keyboard controls
                else
                {
                    // Attack is pressed
                    if (playerInput.actions["Attack"].IsPressed())
                    {
                        // Attack in direction of the mouse
                        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        playerAttackDirection = (mousePosition - (Vector2) transform.position).normalized;

                        //Attack!
                        attacking = true;
                    }
                }

                // Update the animator.
                animator.SetBool("Attacking", attacking);
                if (attacking)
                {
                    animator.SetFloat("Horizontal", playerAttackDirection.x);
                    animator.SetFloat("Vertical", playerAttackDirection.y);
                }
            }
        }

        private void FixedUpdate()
        {
            // Only update if the game is in play.
            if (!_playing) return;

            // If the player is trying to attack, and the attack isn't on cooldown, initiate an attack.
            if (attacking && !attackOnCooldown)
            {
                Attack();
            }
        }

        // Declare an attack.
        private void Attack()
        {
            attacking = false;
            attackOnCooldown = true;
            _weapon.DoAttack(playerAttackDirection);
            StartCoroutine(ResetAttackCooldown());
        }

        // This function resets the attack cooldown after the cooldown period ends.
        IEnumerator ResetAttackCooldown()
        {
            yield return new WaitForSeconds(1 / attackSpeedActual);
            attackOnCooldown = false;
        }

        private static FacingDirection CalculateFacingDirection(Vector2 direction)
        {
            // If the absolute value of X is larger than Y.
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // If X is positive, the facing direction is Right. Otherwise it is Left.
                return (direction.x > 0F ? FacingDirection.Right : FacingDirection.Left);
            }
            // else, the absolute value of Y is larger.
            else
            {
                // If Y is positive, the facing direction is Up. Otherwise it is Down.
                return (direction.y > 0F ? FacingDirection.Up : FacingDirection.Down);
            }
        }

        private void RecalculateStats()
        {
            healthMax = healthInitial + (healthLevel * healthGrowthPerLevel);
            healthActual = healthMax;
            attackDamageActual = attackDamageInitial + (attackDamageLevel * attackDamageGrowthPerLevel);
            attackSpeedActual = attackSpeedInitial + (attackSpeedLevel * attackSpeedGrowthPerLevel);
            attackRangeActual = attackRangeInitial + (attackRangeLevel * attackRangeGrowthPerLevel);
            animator.ResetTrigger("Dead");
            _weapon.RecalculateStats();
        }

        public int GetPlayerHealth()
        {
            return healthActual;
        }

        public void HealPlayer(int healing)
        {
            if (healthActual < healthMax)
            {
                healthActual += healing;
                // Prevent over-healing.
                if (healthActual >= healthMax)
                    healthActual = healthMax;
            }
        }

        public void DamagePlayer(int damage)
        {
            healthActual -= damage;
            // TODO Give visual indication?
            // TODO Update HUD?
            if (healthActual <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            // Make sure that health doesn't go negative.
            healthActual = 0;
            // Stop the game. TODO Hook into the controllers later.
            _playing = false;
            // Trigger the death animation for the player.
            animator.SetTrigger("Dead");
        }

        public float GetAttackDamage()
        {
            return attackDamageActual;
        }

        public float GetAttackSpeed()
        {
            return attackSpeedActual;
        }

        public float GetAttackRange()
        {
            return attackRangeActual;
        }

        public int GetHealthLevel()
        {
            return healthLevel;
        }

        public void IncreaseHealthLevel()
        {
            if (healthLevel < healthMaxLevel)
                healthLevel++;
            RecalculateStats();
        }

        public int GetAttackDamageLevel()
        {
            return attackDamageLevel;
        }

        public void IncreaseAttackDamageLevel()
        {
            if (attackDamageLevel < attackDamageMaxLevel)
                attackDamageLevel++;
            RecalculateStats();
        }

        public int GetAttackSpeedLevel()
        {
            return attackSpeedLevel;
        }

        public void IncreaseAttackSpeedLevel()
        {
            if (attackSpeedLevel < attackSpeedMaxLevel)
                attackSpeedLevel++;
            RecalculateStats();
        }

        public int GetAttackRangeLevel()
        {
            return attackRangeLevel;
        }

        public void IncreaseAttackRange()
        {
            if (attackRangeLevel < attackRangeMaxLevel)
                attackRangeLevel++;
            RecalculateStats();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, attackRangeActual);
        }
    }
}
