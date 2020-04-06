﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerAnimation anim;

    private bool doubleJump = true;
    private int jumpCount;
    public bool avoidDamage = false;

    private float lastDashTime = -10f;
    private float dashCoolDown;
    private float dashTime = 0.15f;
    private float dashTimeLeft;

    private float lastSkill3Time = -10f;
    private float skill3CoolDown;
    private float lastSkill2Time = -10f;
    private float skill2CoolDown;
    private float lastSkill4Time = -12f;
    private float skill4CoolDown;

    private bool jumpPressed = false;
    private float jumpPressTime;
    private bool dashPressed = false;
    private float dashPressTime;
    private bool glideHeld;
    private bool attackHeld;

    [Header("Movement")]
    public float speed;
    public float dashSpeed;
    public float climbSpeed;
    public float jumpForce;
    public float slideSpeed;
    public float glideGravity;

    [Header("Environment")]
    public LayerMask groundLayer;
    public float footOffset = 0.1f;
    public float footHeight = 0.25f;
    public float handOffset = 0.15f;
    public float headOffset = 0.2f;
    public float groundDistance = 0.2f;

    [Header("State")]
    public float xVelocity;
    public float yVelocity;
    public float airSpeed;
    public bool canMove = true;
    public bool isDoubleJumping;
    public bool isOnGround;
    public bool isSliding;
    public bool isGliding;
    public bool isAttacking;
    public bool isDashing;
    public bool isClimbing;
    public bool isOnLadder;
    public bool isHurting;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<PlayerAnimation>();

        dashCoolDown = GetComponent<Player>().dashCoolDown;
        skill2CoolDown = GetComponent<Player>().skill1CoolDown;
        skill3CoolDown = GetComponent<Player>().skill2CoolDown;
        skill4CoolDown = GetComponent<Player>().skill3CoolDown;

        jumpCount = doubleJump ? 2 : 1;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"));
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Player>().isDead) return;
        GetMoveButton();
        GetSkillButton();

        if (Input.GetMouseButtonDown(1))
        {
            Hurt(1, new Vector2(0, 0));
        }
    }

    private void GetSkillButton()
    {
        if (isDashing || !canMove || isSliding || isGliding || isClimbing) return;
        if (Input.GetButtonDown("Skill3") && Time.time > lastSkill3Time + skill3CoolDown)
        {
            UIManager.GetInstance().ResetSkillTime(1);
            rb.velocity = new Vector2(0, rb.velocity.y);
            canMove = false;
            anim.StartSkill3();
            lastSkill3Time = Time.time;
        }
        if (Input.GetButtonDown("Skill2") && Time.time > lastSkill2Time + skill2CoolDown)
        {
            UIManager.GetInstance().ResetSkillTime(0);
            anim.StartSkill2();
            lastSkill2Time = Time.time;
        }
        if (Input.GetButtonDown("Skill4") && Time.time > lastSkill4Time + skill4CoolDown)
        {
            UIManager.GetInstance().ResetSkillTime(2);
            rb.velocity = new Vector2(0, 0);
            canMove = false;
            anim.StartSkill4();
            lastSkill4Time = Time.time;
            rb.gravityScale = 0f;
        }
    }

    private void GetMoveButton()
    {
        xVelocity = Input.GetAxis("Horizontal");
        yVelocity = Input.GetAxis("Vertical");
        glideHeld = Input.GetButton("Jump");
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            jumpPressTime = Time.time + Time.fixedDeltaTime;
        }
        if (jumpPressed && Time.time >= jumpPressTime) jumpPressed = false;
        if (Input.GetButtonDown("Dash"))
        {
            dashPressed = true;
            dashPressTime = Time.time + Time.fixedDeltaTime;
        }
        if (dashPressed && Time.time >= dashPressTime) dashPressed = false;
        attackHeld = Input.GetMouseButton(0);
    }

    void FixedUpdate()
    {
        if (GetComponent<Player>().isDead) return;
        airSpeed = rb.velocity.y;
        EnvironmentCheck();
        Climb();
        Dash();
        Attack();
        GroundMove();
        AirMove();
    }

    private void Hurt(int damage, Vector2 foece)
    {
        if (avoidDamage) return;
        GetComponent<Player>().SetHealth(-damage);
        isHurting = true;
        anim.Hurt();
        rb.bodyType = RigidbodyType2D.Dynamic;
        canMove = true;
        rb.gravityScale = 1f;
        avoidDamage = true;
        StartCoroutine(Recover());
    }

    private void Climb()
    {
        if (!canMove || isDashing || isHurting) return;
        if(isOnLadder && isOnGround)
        {
            if(!isClimbing && yVelocity != 0)
            {
                isClimbing = true;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
        if (isClimbing)
        {
            rb.velocity = new Vector2(rb.velocity.x, yVelocity * climbSpeed * Time.fixedDeltaTime);
            if (!isOnLadder)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                isClimbing = false;
            }
        }
    }

    private void Dash()
    {
        if (isSliding || isGliding || isClimbing || isHurting) return;
        if (dashPressed && Time.time > lastDashTime + dashCoolDown && canMove)
        {
            UIManager.GetInstance().ResetDashTime();
            PoolManager.GetInstance().GetDustObject(true);
            Camera.main.GetComponent<CameraController>().Shake();
            Camera.main.GetComponent<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));
            lastDashTime = Time.time;
            dashTimeLeft = dashTime;
            float direction = transform.localScale.x;
            isDashing = true;
            dashPressed = false;
            rb.velocity = new Vector2(direction * Time.fixedDeltaTime * dashSpeed, 0);
            rb.gravityScale = 0f;
            avoidDamage = true;
        }
        if (isDashing)
        {
            if(dashTimeLeft <= 0)
            {
                isDashing = false;
                rb.gravityScale = 1f;
                avoidDamage = false;
            }
            dashTimeLeft -= Time.fixedDeltaTime;
            PoolManager.GetInstance().GetShadowObject();
        }
    }

    private void Attack()
    {
        if (isSliding || isDashing || isClimbing || isHurting) return;
        isAttacking = attackHeld;
    }

    private void EnvironmentCheck()
    {
        RaycastHit2D leftCheck = Raycast(new Vector2(-footOffset, -footHeight), Vector2.down, groundDistance, groundLayer);
        RaycastHit2D rightCheck = Raycast(new Vector2(footOffset, -footHeight), Vector2.down, groundDistance, groundLayer);
        isOnGround = (leftCheck || rightCheck);
        if (isOnGround && airSpeed < 0.1f)
        {
            isDoubleJumping = false;
            jumpCount = doubleJump ? 2 : 1;
        }

        float direction = transform.localScale.x;
        RaycastHit2D headCheck = Raycast(new Vector2(direction * handOffset, headOffset),
            Vector2.right * direction, groundDistance, groundLayer);
        RaycastHit2D handCheck = Raycast(new Vector2(direction * handOffset, 0),
            Vector2.right * direction, groundDistance, groundLayer);
        RaycastHit2D footCheck = Raycast(new Vector2(direction * handOffset, -footHeight),
            Vector2.right * direction, groundDistance, groundLayer);
        if (!isOnGround && !isAttacking && !isDashing && !isHurting
            && headCheck && handCheck && footCheck && rb.velocity.y < 0f)
        {
            if (!isSliding)
            {
                StartCoroutine(CreateDust(0.3f));
                jumpCount = 1;
                isDoubleJumping = false;
                if (isGliding)
                {
                    isGliding = false;
                    rb.gravityScale = 1f;
                }
                Vector2 pos = transform.position;
                pos.x += handCheck.distance * direction - 0.05f * direction;
                transform.position = pos;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = new Vector2(0, -slideSpeed);
            }
            isSliding = true;
        }
        else if(isSliding)
        {
            
            isSliding = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void GroundMove()
    {
        if (isSliding || isDashing || !canMove || isHurting) return;
        if (isAttacking && isOnGround)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else rb.velocity = new Vector2(xVelocity * speed * Time.fixedDeltaTime, rb.velocity.y);
        FlipDirection();
    }

    private void FlipDirection()
    {
        float direction = rb.velocity.x == 0f ? 0f : (rb.velocity.x > 0 ? 1f : -1f);
        if (direction != 0)
        {
            transform.localScale = new Vector3(direction, 1, 1);
        }
        else
        {
            direction = xVelocity == 0f ? 0f : (xVelocity > 0 ? 1f : -1f);
            if(direction != 0)
                transform.localScale = new Vector3(direction, 1, 1);
        }
    }

    private void AirMove()
    {
        if (isDashing || isClimbing || isHurting) return;
        if (isSliding)
        {
            if (jumpPressed)
            {
                WallJump();
            }
            return;
        }
        if (jumpPressed && jumpCount > 0)
        {
            if (doubleJump && jumpCount == 1)
            {
                isDoubleJumping = true;
            }
            Jump();
        }
        if (isDoubleJumping && glideHeld && rb.velocity.y < -1f)
        {
            isGliding = true;
            rb.gravityScale = glideGravity;
        }
        else
        {
            isGliding = false;
            if(rb.gravityScale != 0f) rb.gravityScale = 1f;
        }
    }

    private RaycastHit2D Raycast(Vector2 offset, Vector2 direction, float length, LayerMask layer)
    {
        Vector2 pos = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(pos + offset, direction, length, layer);
        Debug.DrawRay(pos + offset, direction * length, hit ? Color.red : Color.green);
        return hit;
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        jumpCount--;
        jumpPressed = false;
    }

    private void WallJump()
    {
        float direction = transform.localScale.x;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = new Vector2(0, 0);
        rb.AddForce(new Vector2(6f * -direction, jumpForce), ForceMode2D.Impulse);
        jumpCount--;
        jumpPressed = false;
        isSliding = false;
        FlipDirection();
        StartCoroutine(DisableMovement(0.15f));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isOnLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isOnLadder = false;
        }
    }

    IEnumerator DisableMovement(float duration)
    {
        canMove = false;
        yield return new WaitForSeconds(duration);
        canMove = true;
    }

    IEnumerator CreateDust(float duration)
    {
        do
        {
            PoolManager.GetInstance().GetDustObject(false);
            yield return new WaitForSeconds(duration);
        }while (isSliding) ;
    }

    IEnumerator Recover()
    {
        yield return new WaitForSeconds(0.5f);
        isHurting = false;
        avoidDamage = false;
    }
}
