using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GifAnimation : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 15f;

    private void OnEnable()
    {
        StartCoroutine(framesCoroutine());
    }

    private void OnDisable()
    {
        StopCoroutine(framesCoroutine());
    }

    private IEnumerator framesCoroutine()
    {
        int index = 0;

        while (true)
        {
            image.sprite = frames[index];

            index++;

            if (index >= frames.Length)
                index = 0;

            yield return new WaitForSeconds(1f / frameRate);
        }
    }
}