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
    //    private int[] rank;
    //    private int[] size;

    //    public int Length { get { return parent.Length; } }

    //    public DisjointSet(int capacity)
    //    {
    //        parent = new int[capacity];
    //        rank = new int[capacity];
    //        size = new int[capacity];

    //        // Initialize each element as its own parent
    //        for (int i = 0; i < capacity; i++)
    //        {
    //            parent[i] = i;
    //            rank[i] = 0;
    //            size[i] = 1;
    //        }
    //    }

    //    // Makes a set containing only the element x
    //    public void MakeSet(int x)
    //    {
    //        if (x < 0 || x >= parent.Length)
    //            throw new ArgumentOutOfRangeException();

    //        parent[x] = x;
    //        rank[x] = 0;
    //        size[x] = 1;
    //    }

    //    // Finds the root of the set containing x (with path compression)
    //    public int Find(int x)
    //    {
    //        if (x < 0 || x >= parent.Length)
    //            throw new ArgumentOutOfRangeException();

    //        if (parent[x] != x)
    //        {
    //            // Path compression - make parent point directly to root
    //            parent[x] = Find(parent[x]);
    //        }
    //        return parent[x];
    //    }

    //    // Unions the sets containing x and y
    //    public int Union(int x, int y)
    //    {
    //        int xRoot = Find(x);
    //        int yRoot = Find(y);

    //        if (xRoot == yRoot) 
    //            return xRoot; // Already in same set

    //        // Union by rank - attach smaller rank tree under root of higher rank tree
    //        if (rank[xRoot] < rank[yRoot])
    //        {
    //            parent[xRoot] = yRoot;
    //            size[yRoot] += size[xRoot];
    //            return yRoot;
    //        }
    //        else if (rank[xRoot] > rank[yRoot])
    //        {
    //            parent[yRoot] = xRoot;
    //            size[xRoot] += size[yRoot];
    //            return xRoot;
    //        }
    //        else
    //        {
    //            // If ranks are same, arbitrarily make one root and increment its rank
    //            parent[yRoot] = xRoot;
    //            size[xRoot] += size[yRoot];
    //            rank[xRoot]++;
    //            return xRoot;
    //        }
    //    }

    //    // Gets the size of the set containing x
    //    public int GetSize(int x)
    //    {
    //        int root = Find(x);
    //        return size[root];
    //    }

    //    // For debugging - gets the parent of x (without path compression)
    //    public int GetParent(int x)
    //    {
    //        return parent[x];
    //    }

    //    // For debugging - gets the rank of x's root
    //    public int GetRank(int x)
    //    {
    //        return rank[Find(x)];
    //    }

    //    // Sets parent directly (used in the intersection step)
    //    public void SetParent(int x, int newParent)
    //    {
    //        parent[x] = newParent;
    //        // Note: When using this, you should manage rank and size manually
    //    }
    //}

    //public class DisjointSet
    //{
    //    private int[] parent;
    //    private int[] rank;
    //    private int[] size;

    //    public int Length => parent.Length;

    //    public DisjointSet(int capacity)
    //    {
    //        parent = new int[capacity];
    //        rank = new int[capacity];
    //        size = new int[capacity];
    //        for (int i = 0; i < capacity; i++)
    //        {
    //            parent[i] = i;
    //            size[i] = 1;  // Initialize size to 1
    //            rank[i] = 0;   // Initialize rank to 0
    //        }
    //    }

    //    public int Find(int x)
    //    {
    //        //if (parent[x] != x)
    //        //{
    //        //    parent[x] = Find(parent[x]);  // Path compression
    //        //}
    //        return parent[x];
    //    }

    //    public int Union(int x, int y)
    //    {
    //        int xRoot = Find(x);
    //        int yRoot = Find(y);

    //        if (xRoot == yRoot)
    //            return xRoot;

    //        // Union by rank (without unnecessary rank increments)
    //        if (rank[xRoot] < rank[yRoot])
    //        {
    //            parent[xRoot] = yRoot;
    //            size[yRoot] += size[xRoot];  // Update size
    //            return yRoot;
    //        }
    //        else if (rank[yRoot] < rank[xRoot])
    //        {
    //            parent[yRoot] = xRoot;
    //            size[xRoot] += size[yRoot];  // Update size
    //            return xRoot;
    //        }
    //        else
    //        {
    //            parent[yRoot] = xRoot;
    //            size[xRoot] += size[yRoot];  // Update size
    //            rank[xRoot] = rank[xRoot] + 1;  // Only increment if ranks equal
    //            return xRoot;
    //        }
    //    }

    //    public int GetSize(int x)
    //    {
    //        return size[Find(x)];  // Now returns correct size
    //    }

    //    // Modified SetParent to handle size/rank for combined regions
    //    public void SetParentAndSize(int x, int newParent, int newSize)
    //    {
    //        parent[x] = newParent;
    //        size[x] = newSize;  // Manually set size
    //        //rank[x] = 0;          // Reset rank (or set based on your needs)
    //    }
    //}

    public class DisjointSet
    {
        private int[] parent;
        private int[] size;

        public int Length => parent.Length;

        public DisjointSet(int capacity)
        {
            parent = new int[capacity];
            size = new int[capacity];
            for (int i = 0; i < capacity; i++)
            {
                parent[i] = i;
                size[i] = 1;  // Initialize size to 1
            }
        }

        public int Find(int x)
        {
            // Simple find without path compression
            return parent[x];

            // Or with path compression:
            // if (parent[x] != x)
            //     parent[x] = Find(parent[x]);
            // return parent[x];
        }

        public int Union(int x, int y)
        {
            int xRoot = Find(x);
            int yRoot = Find(y);

            if (xRoot == yRoot)
                return xRoot;

            // Always attach smaller tree to larger tree
            if (size[xRoot] < size[yRoot])
            {
                parent[xRoot] = yRoot;
                size[yRoot] += size[xRoot];
                return yRoot;
            }
            else
            {
                parent[yRoot] = xRoot;
                size[xRoot] += size[yRoot];
                return xRoot;
            }
        }

        public int GetSize(int x)
        {
            return size[Find(x)];
        }

        public void SetParentAndSize(int x, int newParent, int newSize)
        {
            parent[x] = newParent;
            size[x] = newSize;
        }
    }
}
