using System;
using System.Collections.Generic;
using Game._00.Script._00.Manager.Custom_Editor;
using Game._00.Script._03.Traffic_System.Building;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._03.Traffic_System.Building
{

    [CreateAssetMenu(fileName = "Building",menuName = "Sprite Collection/Building")]
    public class BuildingSpriteCollection : ScriptableObject
    {
        [SerializeField] protected List<BuildingSprite> sprites;
        protected Dictionary<BuildingDirection, Sprite > _spriteDict;
        private void OnEnable()
        {
            _spriteDict = new Dictionary<BuildingDirection, Sprite>();
            
            foreach (BuildingSprite sprite in sprites)
            {
                _spriteDict.Add(sprite.direction, sprite.sprite);
            }
            
        }

        /// <summary>
        /// Get Sprite base on direction, only has 4: Top, Down, Right, Left
        /// </summary>
        /// <param name="direction"></param>
        public Sprite GetBuildingSprite(BuildingDirection direction, ParkingLotSize size)
        {
             //This only have horizontal && veritcal
             if (size == ParkingLotSize._1x1)
             {
                 if (direction ==BuildingDirection.Up || direction == BuildingDirection.Down)
                 {
                     if (_spriteDict.ContainsKey(BuildingDirection.Up))
                     {
                         return _spriteDict[BuildingDirection.Up];
                     }
                     return _spriteDict[BuildingDirection.Down];
                 }

                 if (direction == BuildingDirection.Left || direction ==BuildingDirection.Right)
                 {
                     if (_spriteDict.ContainsKey(BuildingDirection.Left))
                     {
                         return _spriteDict[BuildingDirection.Left];
                     }
                     return _spriteDict[BuildingDirection.Right];    
                 }
             }
             if (_spriteDict.ContainsKey(direction))
             {
                 return _spriteDict[direction];
             }
            
             DebugUtility.LogError("Sprite Dict not found, direction has to be: Top, Right, Down, Left", this.name);
             return null;
        }
    }

    [Serializable]
   public struct BuildingSprite
   {
       public Sprite sprite;
       public BuildingDirection direction;
   }
}