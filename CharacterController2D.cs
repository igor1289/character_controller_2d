using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    /* ToDo:
    */


    //Collision parameters
    protected Rigidbody2D rb2d;
    protected ContactFilter2D contactFilter;

    public float slopeLimit = .65f, minMoveDistance = 0.005f, skinWidth = 0.1f;
    public float gravityModifier = 1f;

    protected const int hitBufferSize = 8;

    //Character areas
    public float footLevel, headLevel;

    public enum Area
    {
        FOOT,
        BODY,
        HEAD
    }

    //Velocity
    public Vector2 velocity;

    //Ground
    public bool grounded;
    public Vector2 groundNormal;

    //DEBUG
    public Vector2 dbg_movement;
    public float speed, jumpSpeed;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useLayerMask = true;
    }

    private void Update()
    {
        float direction = Input.GetAxis("Horizontal");
        bool inputJump = Input.GetButton("Jump");

        if (direction != 0)
            velocity.x = Mathf.Sign(direction) * speed;

        if (inputJump)
            velocity.y = jumpSpeed;

        
    }

    private void FixedUpdate()
    {
        Move();
    }


    public List<Hit> HitTest(Vector2 move)
    {
        RaycastHit2D[] hitBuffer = new RaycastHit2D[hitBufferSize];

        List<Hit> hitList = new List<Hit>();

        int count = rb2d.Cast(move, contactFilter, hitBuffer, move.magnitude + skinWidth);

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D raycastHit = hitBuffer[i];

            Hit hit = new Hit();

            hit.raycastHit = raycastHit;
            hit.gameObject = raycastHit.transform.gameObject;

            //Determine collision area
            float collisionHeight = rb2d.transform.InverseTransformPoint(raycastHit.point - move.normalized * raycastHit.distance).y;

            if (collisionHeight <= footLevel)
            {
                hit.area = Area.FOOT;
            }
            else if (collisionHeight >= headLevel)
            {
                hit.area = Area.HEAD;
            }
            else
            {
                hit.area = Area.BODY; 
            }

            hitList.Add(hit);
        }

        return hitList;
    }

    public List<Hit> Move()
    {
        grounded = false;
        velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;

        Vector2 deltaPosition = velocity * Time.deltaTime;

        //Move character along ground
        Vector2 movementAlongGround = new Vector2(groundNormal.y, -groundNormal.x);
        Vector2 move = movementAlongGround * deltaPosition.x;

        List<Hit> hitList = HitTest(move);

        float distance = move.magnitude;

        if (distance > minMoveDistance)
        {
            foreach (Hit hit in hitList)
            {
                Vector2 currentNormal = hit.raycastHit.normal;

                if (hit.area == Area.FOOT)
                {
                    float slope = Vector2.Dot(currentNormal, -Physics2D.gravity.normalized);

                    if (slope > slopeLimit)
                    {
                        grounded = true;
                        groundNormal = currentNormal;
                    }
                    else
                    {
                        currentNormal = -move.normalized;
                    }
                }
                else if (hit.area == Area.BODY)
                {
                    currentNormal = -move.normalized;
                }
                else if (hit.area == Area.HEAD)
                {
                    float slope = Vector2.Dot(currentNormal, -Physics2D.gravity.normalized);

                    if (slope < -slopeLimit)
                    {
                        currentNormal = -move.normalized;
                    }
                }

                float projection = Vector2.Dot(velocity, currentNormal);

                if (projection < 0)
                {
                    velocity -= projection * currentNormal;
                }

                float modifiedDistance = hit.raycastHit.distance - skinWidth;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
            
            rb2d.transform.position += (Vector3)move.normalized * distance;
        }

        //Move character vertically
        move = Vector2.up * deltaPosition.y;

        hitList = HitTest(move);

        distance = move.magnitude;

        if (distance > minMoveDistance)
        {
            foreach (Hit hit in hitList)
            {
                Vector2 currentNormal = hit.raycastHit.normal;

                if (hit.area == Area.FOOT)
                {
                    float slope = Vector2.Dot(currentNormal, -Physics2D.gravity.normalized);

                    if (slope > slopeLimit)
                    {
                        grounded = true;
                        groundNormal = currentNormal;

                        currentNormal.x = 0;
                    }
                }
                else if (hit.area == Area.HEAD)
                {
                    float slope = Vector2.Dot(currentNormal, -Physics2D.gravity.normalized);

                    if (slope < -slopeLimit)
                    {
                        currentNormal = -move.normalized;
                    }
                }

                float projection = Vector2.Dot(velocity, currentNormal);

                if (projection < 0)
                {
                    velocity -= projection * currentNormal;
                }

                float modifiedDistance = hit.raycastHit.distance - skinWidth;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }

            rb2d.transform.position += (Vector3)move.normalized * distance;
        }

        return null;
    }

    public class Hit
    {
        public RaycastHit2D raycastHit;

        public GameObject gameObject;

        public Area area;
    }
}
