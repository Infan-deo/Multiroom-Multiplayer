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
    public GameObject boomGameObject;

    public void OnSetBoom(bool state)
    {
        
        Debug.Log("Sd4");
        boomGameObject.SetActive(state);
    }
    
    public IEnumerator<float> _ExplodeBoom()
    {
        boomGameObject.SetActive(false);
        yield return Timing.WaitForSeconds(2.0f);
    }
}