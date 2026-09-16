using System.Collections.Generic;
using MEC;
using UnityEngine;

public class ManageBoom : MonoBehaviour
{
    public bool BoomEnabled
    {
        get => boomEnabled;
        set
        {
            boomEnabled = value;
            OnSetBoom(value);
        }
    }

    private bool boomEnabled;
    public Animator animator;
    public GameObject boomGameObject;

    public void Start()
    {
        boomGameObject.SetActive(false);
    }

    public void OnSetBoom(bool state)
    {
        boomGameObject.SetActive(state);
    }

    public IEnumerator<float> _ExplodeBoom()
    {
        animator.SetTrigger("0");
        yield return Timing.WaitForSeconds(1.0f);
    }
}