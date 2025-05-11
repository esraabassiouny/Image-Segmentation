using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTemplate
{

    //public class DisjointSet
    //{
    //    private int[] parent;
    //    private int[] size;

    //    public int Length => parent.Length;

    //    public DisjointSet(int capacity)
    //    {
    //        parent = new int[capacity];
    //        size = new int[capacity];
    //        for (int i = 0; i < capacity; i++)
    //        {
    //            parent[i] = i;
    //            size[i] = 1;  // Initialize size to 1
    //        }
    //    }

    //    public int Find(int x)
    //    {
    //        // Simple find without path compression
    //        return parent[x];

    //        // Or with path compression:
    //        // if (parent[x] != x)
    //        //     parent[x] = Find(parent[x]);
    //        // return parent[x];
    //    }

    //    public int Union(int x, int y)
    //    {
    //        int xRoot = Find(x);
    //        int yRoot = Find(y);

    //        if (xRoot == yRoot)
    //            return xRoot;

    //        // Always attach smaller tree to larger tree
    //        if (size[xRoot] < size[yRoot])
    //        {
    //            parent[xRoot] = yRoot;
    //            size[yRoot] += size[xRoot];
    //            return yRoot;
    //        }
    //        else
    //        {
    //            parent[yRoot] = xRoot;
    //            size[xRoot] += size[yRoot];
    //            return xRoot;
    //        }
    //    }

    //    public int GetSize(int x)
    //    {
    //        return size[Find(x)];
    //    }

    //    public void SetParentAndSize(int x, int newParent, int newSize)
    //    {
    //        parent[x] = newParent;
    //        size[x] = newSize;
    //    }
    //}

    public class DisjointSet
    {
        protected int[] parent;
        protected int[] size;
        public int Length { get; private set; }

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

        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]); // Path compression
            return parent[x];
        }

        public virtual void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return;

            // Union by size
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

        public void SetParentAndSize(int x, int p, int s)
        {
            parent[x] = p;
            size[x] = s;
        }
    }

    public class DisjointSetWithInternalDiff : DisjointSet
    {
        public int[] InternalDiff;

        public DisjointSetWithInternalDiff(int size) : base(size)
        {
            InternalDiff = new int[size]; // Initially 0 for all components
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
                InternalDiff[rootY] = Math.Max(edgeWeight,
                    Math.Max(InternalDiff[rootX], InternalDiff[rootY]));
            }
            else
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
                InternalDiff[rootX] = Math.Max(edgeWeight,
                    Math.Max(InternalDiff[rootX], InternalDiff[rootY]));
            }
        }

        public new void SetParentAndSize(int x, int p, int s)
        {
            base.SetParentAndSize(x, p, s);
        }
    }
}
