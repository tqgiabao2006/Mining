using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._03.Traffic_System.CurvePath
{
    public class PathCreator:MonoBehaviour
    {
        [HideInInspector] public CurvePath Path;
        
        public Color anchorCol = Color.red;
        public Color controlCol = Color.white;
        public Color segmentCol = Color.black;
        public Color selectedCol = Color.green;
        public float anchorSize = .1f;
        public float controlSize = .075f;
        public float segmentWidth = 4f;
        public bool displayControl = true;
        
        public void CreatePath()
        {
            Path = new CurvePath((Vector2)this.transform.position + Vector2.left, (Vector2)this.transform.position + Vector2.left/2f + Vector2.up,
                (Vector2)this.transform.position + Vector2.right/2f + Vector2.down, (Vector2)this.transform.position + Vector2.right, 0.5f,0.5f, false);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}