using System.Collections.Generic;
using UnityEngine;

namespace Game._00.Script._00._Manager
{
    public class ObjectPooling : MonoBehaviour
    {
        Dictionary<GameObject, List<GameObject>> _pool = new Dictionary<GameObject, List<GameObject>>();
    
        public virtual GameObject GetObj(GameObject prefabs)
        { 
            List<GameObject> listObj = new List<GameObject>();
            if (_pool.ContainsKey(prefabs))
            {
                listObj = _pool[prefabs];
            }
            else
            {
                _pool.Add(prefabs, listObj);
            }
       
            foreach(GameObject g in listObj)
            {
                if(g.activeSelf)
                    continue;
                return g;
            }
            GameObject g2 = Instantiate(prefabs, this.transform.position, Quaternion.identity);
            listObj.Add(g2);
            return g2;
        }
    }
}