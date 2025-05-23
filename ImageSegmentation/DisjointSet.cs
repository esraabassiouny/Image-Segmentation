using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTemplate
{
    public class DisjointSet
    {
        protected int[] parent;
        protected int[] size;
        public int Length { get; private set; }

        //O(M)
        public DisjointSet(int n)
        {
            parent = new int[n];
            size = new int[n];
            Length = n;

            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
                size[i] = 1;
            }
        }

        //O(log M)
        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }
        //O(log M)
        public virtual void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return;

            //union by size >> make tree almost balanced >> maximum depth of tree almost O(log M)
            if (size[rootX] < size[rootY])
            {
                parent[rootX] = rootY;
                size[rootY] += size[rootX];
            }
            else
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
            }
        }

        public int GetSize(int x) => size[Find(x)];

    }

    public class DisjointSetWithInternalDifference : DisjointSet
    {
        public int[] InternalDifference;

        public DisjointSetWithInternalDifference(int size) : base(size)
        {
            InternalDifference = new int[size];
        }

        public void Union(int x, int y, int edgeWeight)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return;

            if (size[rootX] < size[rootY])
            {
                parent[rootX] = rootY;
                size[rootY] += size[rootX];
                InternalDifference[rootY] = Math.Max(edgeWeight,
                    Math.Max(InternalDifference[rootX], InternalDifference[rootY]));
            }
            else
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
                InternalDifference[rootX] = Math.Max(edgeWeight,
                    Math.Max(InternalDifference[rootX], InternalDifference[rootY]));
            }
        }
    }
}
