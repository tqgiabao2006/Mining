using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private List<ZoomByLevel> zoomData;

    void Start()
    {
        _camera = GetComponent<Camera>();
        // StartCoroutine(ZoomOut(0));
    }

    /// <summary>
    /// Called only once when the game level changes.
    /// Level is 0-indexed.
    /// </summary>
    public IEnumerator ZoomOut(int level)
    {
        if (level >= zoomData.Count)
        {
            Debug.LogError($"This camera doesn't contain zoom data for level {level}", this);
            yield break;
        }

        float targetSize = zoomData[level].endSize;
        float duration = zoomData[level].time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            _camera.orthographicSize = Mathf.SmoothStep(zoomData[level].startSize, targetSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _camera.orthographicSize = targetSize; // Ensure it reaches exact target size at the end
    }
}


[Serializable]
// Max size is 50, min size is 15
public struct ZoomByLevel
{
    public float time;

    [Range(15, 50)] public float startSize;
    [Range(15, 50)] public float endSize;
}