using System.Collections;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Observer;
using Game._00.Script.Camera;
using UnityEngine;

public class UI_Grid : MonoBehaviour, IObserver
{

    private Material _material;
    
    private SpriteRenderer _spriteRenderer;
    
    private CameraZoom _cameraZoom;
    
    private static readonly int Pivot = Shader.PropertyToID("_Pivot");
    
    private static readonly int Size = Shader.PropertyToID("_Size");
    
    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        _material = _spriteRenderer.material;
        
        _cameraZoom = CameraZoom.Instance;
    }

    private void Update()
    {
        _material.SetVector(Pivot, new  Vector4(_cameraZoom.Zone.BotLeftPivot.x, _cameraZoom.Zone.BotLeftPivot.y, 0,0));
        _material.SetVector(Size, new Vector4(_cameraZoom.Zone.Size.x, _cameraZoom.Zone.Size.y, 0,0));
    }

    public void OnNotified(object data, string flag)
    {
        if (flag == NotificationFlags.PLACING)
        {
            _spriteRenderer.enabled = true;
        }

        if (flag == NotificationFlags.NOT_PLACING)
        {
            _spriteRenderer.enabled = false;
        }
    }
}
