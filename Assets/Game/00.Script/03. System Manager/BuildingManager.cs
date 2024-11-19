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
        
        NormalCell,
        None
    }

    
    public class BuildingManager: MonoBehaviour
    {
        //Directed graph => adjacent list => building type + its output
        private Dictionary<BuildingType, List<BuildingType>> _inputOutputMap = new Dictionary<BuildingType, List<BuildingType>>();
        private List<Building> _currentBuildings = new List<Building>();

        public List<Building> CurrentBuildings
        {
            get { return _currentBuildings; }
        }

        private void Awake()
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
            //Assign index
            _currentBuildings.Add(building);
            // AddOutputBuildings(building.BuildingType);
        }
        
        
        //
        // /// <summary>
        // /// Get the building input list, add this building to inputBuilding output lit
        // /// </summary>
        // /// <param name="buildingType"></param>
        // private void AddOutputBuildings(BuildingType buildingType)
        // {
        //     foreach (Building build in _currentBuildings)
        //     {
        //         if (build.BuildingType == buildingType)
        //         {
        //             if (_ouputPositionMap.ContainsKey(build.BuildingType))
        //             {
        //                 _ouputPositionMap[build.BuildingType].Add(build.WorldPosition);
        //             }
        //             else
        //             {
        //                 _ouputPositionMap.Add(build.BuildingType, new List<Vector3>());
        //             }
        //         }
        //     }
        // }
        //
        // /// <summary>
        // /// Find the nearest output object, find if can find path is successful
        // /// </summary>
        // /// <param name="building"></param>
        // /// <exception cref="Exception"></exception>
        // private void GetShortestPath(Building building)
        // {
        //     if (!_inputOutputMap.ContainsKey(building.BuildingType))
        //     {
        //         throw new Exception("Building type not registered");
        //     }
        //     List<Vector3> ouputs = _ouputPositionMap[building.BuildingType];
        // }
        
    }
}