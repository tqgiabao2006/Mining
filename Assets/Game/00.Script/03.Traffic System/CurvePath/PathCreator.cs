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
            Path = new CurvePath(this.transform.position);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}