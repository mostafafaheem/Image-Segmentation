An efficient image segmentation pipeline inspired by the Felzenszwalb-Huttenlocher algorithm, leveraging graph-based methods and Minimum Spanning Tree (MST) construction via Kruskal’s algorithm.

Represented each pixel as a point in 5D space (x, y, r, g, b) to combine spatial and color similarity.

Constructed a K-nearest neighbor graph using a custom KDB tree for fast neighbor searches in high-dimensional space.

Applied Kruskal’s algorithm to build the MST, then segmented the image by selectively removing edges based on a dynamic threshold function.

Built modular components: graph construction, MST-based segmentation, segment visualization, and an interactive “Click-to-Merge” interface for manual refinement.

Experimented with nearest-neighbor graph variants to analyze segmentation quality and computational trade-offs.

Focused on scalable performance for high-resolution images with visually meaningful boundaries.
