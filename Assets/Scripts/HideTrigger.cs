using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideTrigger : MonoBehaviour
{
    //public Animator anim;
    public SpriteRenderer rend;
    bool istransitioning;
    public float transspeed = 0.05f, lowerValue = 0.1f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "LocalPlayer")
        {
            //anim.SetTrigger("hide");
            istransitioning = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "LocalPlayer")
        {
            //anim.SetTrigger("hide");
            istransitioning = false;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (istransitioning == true)
        {
            rend.color = new Color(rend.color.r, rend.color.g, rend.color.b, Mathf.MoveTowards(rend.color.a, lowerValue, transspeed));
        }
        else
        {
            rend.color = new Color(rend.color.r, rend.color.g, rend.color.b, Mathf.MoveTowards(rend.color.a, 1, transspeed));
        }
            

    }
}
