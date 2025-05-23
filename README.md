# Image-Segmentation-
Graph-Based Image Segmentation (C#) Efficient region merging using Minimum Spanning Trees (MST) and adaptive thresholding This project implements a graph-based image segmentation algorithm. It partitions images into coherent regions by: Treating pixels as graph nodes and edges as intensity differences. Merging regions hierarchically via Disjoint Set (Union-Find) with path compression. Applying parallel processing for RGB channels and adaptive thresholding (k-parameter) for boundary sensitivity.
Features ✅ MST-based segmentation for O(M log M) performance (M = edges) 
         ✅ 8-connected pixel neighborhoods with Gaussian-smoothed edge weights 
         ✅ Multi-channel support: Processes R/G/B independently and intersects results 
         ✅ Output: Segment counts, sorted region sizes, and color-coded visualization
