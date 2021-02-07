using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region // インスペクターで設定する
    [Header("速度")] public float speed;
    [Header("ジャンプ速度")] public float jumpSpeed;
    [Header("ジャンプできる高さ")] public float jumpHeight;
    [Header("ジャンプ制限時間")] public float jumpLimitTime;
    [Header("踏みつけ判定の高さの割合")] public float stepOnRate;
    [Header("重力")] public float gravity;
    [Header("設置判定")] public GroundCheck ground;
    [Header("頭をぶつけた判定")] public GroundCheck head;
    [Header("ダッシュの速さ表現")] public AnimationCurve dashCurve;
    [Header("ジャンプの速さ表現")] public AnimationCurve jumoCurve;
    #endregion

    #region // プライベート変数
    private Animator anim = null;
    private Rigidbody2D rb = null;
    private CapsuleCollider2D capcol = null;
    private bool isGround = false;
    private bool isHead = false;
    private bool isJump = false;
    private bool isOtherJump = false;
    private bool isRun = false;
    private bool isDown = false;
    private float jumpPos = 0.0f;
    private float otherJumpHeight = 0.0f;
    private float jumpTime = 0.0f;
    private float dashTime = 0.0f;
    private float beforeKey = 0.0f;
    private string enemyTag = "Enemy";
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // コンポーネントのインスタンスを捕まえる
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        capcol = GetComponent<CapsuleCollider2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isDown) {
            // 設置判定を得る
            isGround = ground.IsGround();
            isHead = head.IsGround();

            // 各種座標軸の速度を求める
            float xSpeed = GetXSpeed();
            float ySpeed = GetYSpeed();
            
            // アニメーションを適用
            SetAnimation();
            
            // 移動速度を設定
            rb.velocity = new Vector2(xSpeed, ySpeed);
        } else {
            rb.velocity = new Vector2(0, -gravity);
        }
    }

    /// <summary>
    /// Y成分で必要な計算をし、速度を返す
    /// </summary>
    /// <returns>Y軸の速さ</returns>
    private float GetYSpeed()
    {
        float verticalKey = Input.GetAxis("Vertical");
        float ySpeed = -gravity;
        if (isOtherJump) {
            // 現在の高さが飛べる高さより下か
            bool canHeight = jumpPos + otherJumpHeight > transform.position.y;
            // ジャンプ時間が長くなりすぎていないか
            bool canTime = jumpLimitTime > jumpTime;
            if (canHeight && canTime && !isHead) {
                ySpeed = jumpSpeed;
                jumpTime += Time.deltaTime;
            } else {
                isOtherJump = false;
                jumpTime = 0.0f;
            }
        } else if (isGround) {
            if (verticalKey > 0) {
                ySpeed = jumpSpeed;
                jumpPos = transform.position.y;  // ジャンプした位置を記録する
                isJump = true;
                jumpTime = 0.0f;
            } else {
                isJump = false;
            }
        } else if (isJump) {
            // 上方向キーを押しているか
            bool pushUpKey = verticalKey > 0;
            // 現在の高さが飛べる高さより下か
            bool canHeight = jumpPos + jumpHeight > transform.position.y;
            // ジャンプ時間が長くなりすぎていないか
            bool canTime = jumpLimitTime > jumpTime;
            if (pushUpKey && canHeight && canTime && !isHead) {
                ySpeed = jumpSpeed;
                jumpTime += Time.deltaTime;
            } else {
                isJump = false;
                jumpTime = 0.0f;
            }
        }

        // アニメーションカーブを速度に適用
        if (isJump || isOtherJump) {
            ySpeed *= jumoCurve.Evaluate(jumpTime);
        }
        return ySpeed;
    }

    /// <summary>
    /// X成分で必要な計算をし、速度を返す
    /// </summary>
    /// <returns>X軸の速さ</returns>
    private float GetXSpeed()
    {
        float horizontalKey = Input.GetAxis("Horizontal");
        float xSpeed = 0.0f;

        if (horizontalKey > 0) {
            transform.localScale = new Vector3(1, 1, 1);
            isRun = true;
            dashTime += Time.deltaTime;
            xSpeed = speed;
        } else if (horizontalKey < 0){
            transform.localScale = new Vector3(-1, 1, 1);
            isRun = true;
            dashTime += Time.deltaTime;
            xSpeed = -speed;
        } else {
            isRun = false;
            dashTime = 0.0f;
        }

        // 前回の入力からダッシュの反転を判断して速度を変える
        if (horizontalKey > 0 && beforeKey < 0) {
            dashTime = 0.0f;
        } else if (horizontalKey < 0 && beforeKey >0) {
            dashTime = 0.0f;
        }
        beforeKey = horizontalKey;

        // アニメーションカーブを速度に適用
        xSpeed *= dashCurve.Evaluate(dashTime);
        return xSpeed;
    }

    /// <summary>
    /// アニメーションを設定する
    /// </summary>
    /// <returns>void</returns>
    private void SetAnimation()
    {
        anim.SetBool("run", isRun);
        anim.SetBool("jump", isJump || isOtherJump);
        anim.SetBool("ground", isGround);
    }

    #region // 接触判定
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == enemyTag) {
            // 踏みつけ判定になる高さ
            float stepOnHeight = (capcol.size.y * (stepOnRate / 100f));
            // 踏みつけ判定のワールド座標
            float judgePos = transform.position.y - (capcol.size.y / 2f) + stepOnHeight;
            foreach (ContactPoint2D p in collision.contacts)
            {
                if (p.point.y < judgePos) {
                    // もう一度跳ねる
                    ObjectCollision o = collision.gameObject.GetComponent<ObjectCollision>();
                    if (o != null) {
                        otherJumpHeight = o.boundHeight;
                        o.playerStepOn = true;
                        jumpPos = transform.position.y;
                        isOtherJump = true;
                        isJump = false;
                        jumpTime = 0.0f;
                    } else {
                        Debug.Log("ObjectCollisionがついてないよ");
                    }
                } else {
                    // ダウンする
                    anim.Play("player_down");
                    isDown = true;
                    break;
                }
            }
        }
    }
    #endregion
}
