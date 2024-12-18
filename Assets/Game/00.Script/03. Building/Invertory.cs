using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public enum SpecificRoadType 
{
   //Red = input (good blood), blue = output (bad blood)
    RedNormal,
    RedExtended,
   
    BlueNormal,
    BlueExtended,
    
    FoodNormal,
    FoodExtended,
    
    Bridge,
    Overpass,
    UnderPass
   
}

public class Invertory : MonoBehaviour
{
   private Dictionary <SpecificRoadType, int> _inventory = new Dictionary<SpecificRoadType, int>();
   public Dictionary <SpecificRoadType, int> Inventory { get { return _inventory; } }
      

   private void Start()
   {
      Testing();
   }
   
   private void Update()
   {
      
   }

   private void UpdateInventory(SpecificRoadType roadType, int amount)
   {
      if (_inventory.ContainsKey(roadType))
      {
         _inventory[roadType] += amount;
      }
      else
      {
         _inventory.Add(roadType, amount);
      }
      
   }

   public int GetNumbRoadByType(SpecificRoadType specificRoadType)
   {
      return _inventory[specificRoadType];
   }
   
   //Real road can bill to creat connection
   public int GetPossitiveNumbRoad()
   {
      int sum = 0;
      foreach (var road in _inventory)
      {
         if (road.Key == SpecificRoadType.RedNormal || road.Key == SpecificRoadType.RedExtended || road.Key == SpecificRoadType.Overpass|| road.Key == SpecificRoadType.Bridge || road.Key == SpecificRoadType.UnderPass)
         {
            sum += road.Value;
         }
      }
      return sum;
   }

   public int GetNumbAllRoad()
   {
      return _inventory.Values.Count();
   }
   private void Testing()
   {      
      UpdateInventory(SpecificRoadType.RedNormal, 100);
      UpdateInventory(SpecificRoadType.BlueNormal, 100);
   }
}
