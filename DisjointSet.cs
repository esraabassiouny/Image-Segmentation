using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTemplate
{
    public class DisjointSet
    {
        private int[] parent;
        private int[] rank;
        private int[] size;

        public int Length => parent.Length;

        public DisjointSet(int count)
        {
            parent = new int[count];
            rank = new int[count];
            size = new int[count];

            for (int i = 0; i < count; i++)
            {
                parent[i] = i;
                rank[i] = 0;
                size[i] = 1;
            }
        }

        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        public void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return;

            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
                size[rootY] += size[rootX];
            }
            else
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
                if (rank[rootX] == rank[rootY])
                    rank[rootX]++;
            }
        }

        public int GetSize(int x) => size[Find(x)];
    }
}
