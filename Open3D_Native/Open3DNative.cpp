#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <Windows.h>
#include <objbase.h>
#include <algorithm>
#include <cstring>
#include <string>
#include <vector>
#include "Open3DPragmaLibs.h"
#include <open3d/Open3DConfig.h>
#include <open3d/geometry/PointCloud.h>
#include <open3d/geometry/TriangleMesh.h>

extern "C" __declspec(dllexport)
char* Open3D_PrintVersion()
{
    const std::string version = std::string("Open3D ") + OPEN3D_VERSION;
    const size_t byteCount = version.size() + 1;
    char* buffer = static_cast<char*>(CoTaskMemAlloc(byteCount));
    if (buffer != nullptr) {
        std::memcpy(buffer, version.c_str(), byteCount);
    }
    return buffer;
}

extern "C" __declspec(dllexport)
int Open3D_AlphaShape(const float* points,
                      int pointCount,
                      double alpha,
                      float* outVertices,
                      int* outVertexCount,
                      int maxVertices,
                      int* outTriangles,
                      int* outTriangleCount,
                      int maxTriangles)
{
    if (outVertexCount != nullptr) {
        *outVertexCount = 0;
    }
    if (outTriangleCount != nullptr) {
        *outTriangleCount = 0;
    }
    if (points == nullptr || pointCount < 4 || alpha <= 0.0) {
        return 0;
    }

    try {
        open3d::geometry::PointCloud pcd;
        pcd.points_.resize(static_cast<size_t>(pointCount));
        for (int i = 0; i < pointCount; ++i) {
            const int base = i * 3;
            pcd.points_[static_cast<size_t>(i)] = Eigen::Vector3d(
                    points[base], points[base + 1], points[base + 2]);
        }

        const auto mesh =
                open3d::geometry::TriangleMesh::CreateFromPointCloudAlphaShape(
                        pcd, alpha);
        if (mesh == nullptr) {
            return 0;
        }

        const int vertexCount = static_cast<int>(mesh->vertices_.size());
        const int triangleCount = static_cast<int>(mesh->triangles_.size());
        if (outVertexCount != nullptr) {
            *outVertexCount = vertexCount;
        }
        if (outTriangleCount != nullptr) {
            *outTriangleCount = triangleCount;
        }

        if (outVertices != nullptr && maxVertices > 0) {
            const int copyVertices = (std::min)(vertexCount, maxVertices);
            for (int i = 0; i < copyVertices; ++i) {
                const int base = i * 3;
                outVertices[base] = static_cast<float>(mesh->vertices_[i].x());
                outVertices[base + 1] = static_cast<float>(mesh->vertices_[i].y());
                outVertices[base + 2] = static_cast<float>(mesh->vertices_[i].z());
            }
        }

        if (outTriangles != nullptr && maxTriangles > 0) {
            const int copyTriangles = (std::min)(triangleCount, maxTriangles);
            for (int i = 0; i < copyTriangles; ++i) {
                const int base = i * 3;
                outTriangles[base] =
                        static_cast<int>(mesh->triangles_[i](0));
                outTriangles[base + 1] =
                        static_cast<int>(mesh->triangles_[i](1));
                outTriangles[base + 2] =
                        static_cast<int>(mesh->triangles_[i](2));
            }
        }

        return 1;
    } catch (...) {
        if (outVertexCount != nullptr) {
            *outVertexCount = 0;
        }
        if (outTriangleCount != nullptr) {
            *outTriangleCount = 0;
        }
        return 0;
    }
}
