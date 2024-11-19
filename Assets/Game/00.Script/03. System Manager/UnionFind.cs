namespace Game._00.Script._05._Manager
{
using System;
using System.Collections.Generic;

    public class UnionFind
    {
        private Dictionary<int, int> parent;
        private Dictionary<int, int> rank;

        public UnionFind()
        {
            parent = new Dictionary<int, int>();
            rank = new Dictionary<int, int>();
        }

        /// <summary>
        /// Call recursively to find the deepest root, compress => 1->2->3->4 store in 1->4
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public int Find(int building)
        {
            if (parent[building] != building)
            {
                parent[building] = Find(parent[building]); // Path compression
            }
            return parent[building];
        }

        
        /// <summary>
        /// If two segments has connection, merge it to tree
        /// Ex: 1: {2}, 2:{1} call Union(1,2) merge it 1-2
        /// </summary>
        /// <param name="building1"></param>
        /// <param name="building2"></param>
        public void Union(int building1, int building2)
        {
            int root1 = Find(building1);
            int root2 = Find(building2);

            if (root1 != root2)
            {
                // Union by rank
                if (rank[root1] > rank[root2])
                {
                    parent[root2] = root1;
                }
                else if (rank[root1] < rank[root2])
                {
                    parent[root1] = root2;
                }
                else
                {
                    parent[root2] = root1;
                    rank[root1]++; // Increase rank if the roots were equal
                }
            }
        }
        
         /// <summary>
         /// Set up building, start in 0, parent = itself
         /// </summary>
         /// <param name="building"></param>
        public void AddBuilding(int building)
        {
            if (!parent.ContainsKey(building))
            {
                parent[building] = building;
                rank[building] = 0;
            }
        }
    }
}