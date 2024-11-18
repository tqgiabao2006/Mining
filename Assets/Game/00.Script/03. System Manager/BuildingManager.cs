using System;
using System.Linq;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game._00.Script._05._Manager
{
    
    public enum BuildingType
    {
        Heart,
        Lung,
        Stomach,
        
        NormalCell
    }

    
    public class BuildingManager:MonoBehaviour
    {
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _inputOutputMap = new Dictionary<BuildingType, List<BuildingType>>();
        private Dictionary<BuildingType, List<Vector3>> _ouputPositionMap = new Dictionary<BuildingType, List<Vector3>>();
        private List<Building> _currentBuildings = new List<Building>();
        [SerializeField] public float BuildingRadius = 0.5f;

        public List<Building> CurrentBuildings
        {
            get { return _currentBuildings; }
        }
        private void Start()
        {
          
            InitialInputOutputMap();

        }

        private void InitialInputOutputMap()
        {
            _inputOutputMap.Add(BuildingType.Lung, new List<BuildingType>() { BuildingType.NormalCell, BuildingType.Heart });
            _inputOutputMap.Add(BuildingType.Heart, new List<BuildingType>() { BuildingType.Lung, BuildingType.NormalCell });
            _inputOutputMap.Add(BuildingType.NormalCell, new List<BuildingType>() { BuildingType.Heart });
        }
        
        public void RegisterBuilding(Building building)
        {
            _currentBuildings.Add(building);
            AddOutputBuildings(building.BuildingType);
        }
        
        /// <summary>
        /// Get the building input list, add this building to inputBuilding output lit
        /// </summary>
        /// <param name="buildingType"></param>
        private void AddOutputBuildings(BuildingType buildingType)
        {
            foreach (Building build in _currentBuildings)
            {
                if (build.BuildingType == buildingType)
                {
                    if (_ouputPositionMap.ContainsKey(build.BuildingType))
                    {
                        _ouputPositionMap[build.BuildingType].Add(build.WorldPosition);
                    }
                    else
                    {
                        _ouputPositionMap.Add(build.BuildingType, new List<Vector3>());
                    }
                }
            }
        }

        /// <summary>
        /// Find the nearest output object, find if can find path is successful
        /// </summary>
        /// <param name="building"></param>
        /// <exception cref="Exception"></exception>
        private void GetShortestPath(Building building)
        {
            if (!_inputOutputMap.ContainsKey(building.BuildingType))
            {
                throw new Exception("Building type not registered");
            }
            List<Vector3> ouputs = _ouputPositionMap[building.BuildingType];
            
            
        }

        private void QuickSort(List<Vector3> positions, Vector3 startPos)
        {
            //Create list distance:
            List<float> distances = new List<float>();
            foreach (Vector3 p in positions)
            {
                
            }
            
        }
        
    }
}