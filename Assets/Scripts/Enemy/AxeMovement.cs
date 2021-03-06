﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxeMovement : EnemyMovement
{
    //控制怪物行动范围
    public Transform startPos;
    public Transform endPos;

    private float startx;
    private float endx;

    private bool attackType; //true攻击,false防御
    //private bool walking;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        anim.SetBool("jump", false);
        anim.SetBool("dead", false);
        anim.SetBool("walk", false);
        startx = startPos.position.x;
        endx = endPos.position.x;
        maxBlood = 200;
        blood = maxBlood;
        walkspeed = 100f;
        waittime = 1f;
        length = 0.31f;
        faceright = true;
        attackType = true;
        //length = 0.375f;
        //walking = true;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isdead) return;
        base.Update();

        Attack();
        //testAttack();
      
    }
   
    void FixedUpdate()
    {
        if (isdead) return;
        Move();
    }
    void Move()
    {
        if (isdead)
        {
            return;
        }
        if (attacking)
        {
            //Debug.Log("Attacking");
            rb.velocity = new Vector2(0, rb.velocity.y);
            //Debug.Log(rb.velocity);
            return;
        }
        
        
        //到起点和终点的距离
        float edis = endx - transform.position.x;
        float sdis = transform.position.x - startx;
        float hdis = Mathf.Abs(player.transform.position.y - transform.position.y);
        if (faceright && edis > 0)
        {
            //面向右，未到达终点
            if (player.transform.position.x > startx && player.transform.position.x < transform.position.x)
            {
                //玩家在身后，转身
                if (hdis < 0.5 && !block) 
                {
                    Flip();
                }
            }
            else
            {
                anim.SetBool("walk", true);
                rb.velocity = new Vector2(walkspeed * Time.deltaTime, rb.velocity.y);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, length + 0.05f, groundLayer.value);
                if (hit)
                {
                    block = true;
                    Flip();
                    rb.velocity = new Vector2(-1 * walkspeed * Time.deltaTime, rb.velocity.y);
                }
                else
                {
                    block = false;
                }
            }
        }
        else if (faceright && edis <= 0)
        {
            //面向右，到达终点
            anim.SetBool("walk", false);
            rb.velocity = new Vector2(0, 0);
            if (waittime > 0)
            {
                waittime -= Time.deltaTime;
            }
            else
            {
                waittime = 1;
                Flip();
            }
        }
        else if (!faceright && sdis > 0)
        {
            if (player.transform.position.x > transform.position.x && player.transform.position.x < endx)
            {
                if (!attacking && hdis < 0.5 && !block) 
                {
                    Flip();
                }
            }
            else
            {
                anim.SetBool("walk", true);
                rb.velocity = new Vector2(-1 * walkspeed * Time.deltaTime, rb.velocity.y);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.left, length + 0.05f, groundLayer.value);
                if (hit)
                {
                    block = true;
                    Flip();
                    rb.velocity = new Vector2(walkspeed * Time.deltaTime, rb.velocity.y);
                }
                else
                {
                    block = false;
                }
            }
        }
        else
        {
            anim.SetBool("walk", false);
            rb.velocity = new Vector2(0, 0);
            if (waittime > 0)
            {
                waittime -= Time.deltaTime;
            }
            else
            {
                waittime = 1;
                Flip();
            }
        }
    }

    void Attack()
    {
        if (!isdead && ground && !hurt)
        {
            //根据和玩家的距离进行攻击
            float distance = player.transform.position.x - transform.position.x;
            //Debug.Log("player: " + player.transform.position.x + ", enemy: " + transform.position.x);
            float heightdis = Mathf.Abs(player.transform.position.y - transform.position.y);
            if (faceright && distance > 0 && distance < 0.65 && heightdis < 0.5 || !faceright && distance < 0 && distance > -0.65 && heightdis < 0.5)
            {
                if (!attacking)
                {
                    attacking = true;
                    anim.SetBool("walk", false);
                    if (attackType)
                    {
                        anim.SetBool("attack", true);
                    }
                    else
                    {
                        //shield = true;
                        Invoke("doShield", 1.4f);
                        anim.SetBool("shield", true);
                        Invoke("finishShield", 3f);
                    }
                    attackType = !attackType;
                }

            }
            if (shield && (faceright && distance > 0 || !faceright && distance < 0))
            {
                // Debug.Log("shield: " + shield + ", faceright: " + faceright + ", distance: " + distance);
                canHurt = false;
            }
            else
            {
                // Debug.Log("shield: " + shield + ", faceright: " + faceright + ", distance: " + distance);
                canHurt = true;
            }
            //Debug.Log(canHurt);
        }
    }

    private void doShield()
    {
        shield = true;
    }
    public void finishAttack()
    {
        anim.SetBool("attack", false);
        attacking = false;
    }

    public void finishShield()
    {
        if (shield)
        {
            shield = false;
            anim.SetBool("shield", false);
            attacking = false;
        }
        
    }

    public override void getDamage(float damage, int direction)
    {
        base.getDamage(damage, direction);
        if (!isdead && canHurt)
        {
            CancelInvoke("doShield");
            CancelInvoke("finishShield");
        }
    }
    protected override void Drop()
    {
        int silverNum = Random.Range(3, 8);
        if (silverNum >= 5)
        {
            silverNum = silverNum - 5;
            GameObject itemObject = Instantiate(goldPrefab);
            itemObject.GetComponent<Item>().Emit(transform.position + Vector3.up * 0.2f);
        }

        for (int i = 1; i <= silverNum; i++)
        {
            GameObject itemObject = Instantiate(silverPrefab);
            itemObject.GetComponent<Item>().Emit(transform.position + Vector3.up * 0.2f);
            //item.
            //itemList.Add(Instantiate())
        }

        float heartDrop = Random.Range(0, 1);
        if (heartDrop <= 0.3)
        {
            GameObject itemObject = Instantiate(silverPrefab);
            itemObject.GetComponent<Item>().Emit(transform.position + Vector3.up * 0.2f);
        }
    }
}
       