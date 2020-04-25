﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Transform damagePoint;
    public Transform energyPoint;
    public Transform dartPoint;
    public LayerMask enemyLayer;

    private GameObject energy;
    private GameObject dart;
    private GameObject bigDust;

    private float scope = 0.2f;
    private bool slowDown = false;
    private bool isSlashing = false;
    private float slowDownTime = 0.3f;
    private PlayerMovement player;
    private Rigidbody2D rb;

    private List<GameObject> damagesEnemies;
    private float pauseTime = 0f;
    private bool isTimeSlow = false;

    private void Start()
    {
        player = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();

        energy = PoolManager.GetInstance().transform.GetChild(0).gameObject;
        dart = PoolManager.GetInstance().transform.GetChild(1).gameObject;
        bigDust = PoolManager.GetInstance().transform.GetChild(2).gameObject;

        damagesEnemies = new List<GameObject>();
    }

    private void Update()
    {
        if(slowDown && Mathf.Abs(rb.velocity.x) >= 0.1f)
        {
            rb.velocity = new Vector2(Mathf.Lerp(0f, rb.velocity.x, (slowDownTime-Time.deltaTime)/0.3f), 0);
        }
        else if(slowDown)
        {
            slowDown = false;
            slowDownTime = 0.3f;
        }

        if (isSlashing)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 0.35f, enemyLayer);
            foreach (Collider2D enemy in enemies)
            {
                if (!damagesEnemies.Contains(enemy.gameObject))
                {
                    pauseTime += 0.01f;
                    if (!isTimeSlow)
                    {
                        isTimeSlow = true;
                        Time.timeScale = 0.1f;
                        StartCoroutine(TimeStart());
                    }
                    damagesEnemies.Add(enemy.gameObject);
                    enemy.GetComponent<EnemyMovement>().getDamage(30, (int)transform.localScale.x);
                    Debug.Log("damage");
                }
            }
        }
    }

    public void Attack()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(damagePoint.position, scope, enemyLayer);
        foreach (Collider2D enemy in enemies)
        {
            int direction = (enemy.transform.position.x > transform.position.x) ? 1 : -1;
            enemy.GetComponent<EnemyMovement>().getDamage(30, direction);
            Debug.Log("damage");
        }
    }

    public void Hadouken()
    {
        player.canMove = true;
        energy.transform.position = energyPoint.position;
        energy.SetActive(true);
    }

    public void Throw()
    {
        dart.transform.position = dartPoint.position;
        dart.SetActive(true);
    }

    public void Slash()
    {
        player.avoidDamage = true;
        rb.velocity = new Vector2(player.transform.localScale.x * 8f, 0);
        isSlashing = true;
        bigDust.SetActive(true);
        bigDust.transform.position = transform.position;
        bigDust.transform.localScale = transform.localScale;
        Camera.main.GetComponent<CameraController>().Shake();
        Camera.main.GetComponent<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));
    }

    public void SlashEnd()
    {
        slowDown = true;
        isSlashing = false;
        damagesEnemies.Clear();
    }

    public void Recover()
    {
        player.avoidDamage = false;
        player.canMove = true;
        player.gameObject.GetComponent<Rigidbody2D>().gravityScale = 1f;
    }

    IEnumerator TimeStart()
    {
        yield return new WaitForSeconds(pauseTime);
        Time.timeScale = 1f;
        isTimeSlow = false;
    }
}