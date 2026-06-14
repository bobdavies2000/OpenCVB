// Open3D example: TriangleMesh::CreateFromPointCloudAlphaShape
// Based on Open3D/examples/python/geometry/triangle_mesh_from_point_cloud_alpha_shapes.py

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <Windows.h>

#include <iostream>
#include <string>

#include "../../Open3D_Native/Open3DPragmaLibs.h"
#include <open3d/Open3DConfig.h>
#include <open3d/data/Dataset.h>
#include <open3d/geometry/PointCloud.h>
#include <open3d/geometry/TriangleMesh.h>
#include <open3d/io/PointCloudIO.h>
#include <open3d/io/TriangleMeshIO.h>
#include <open3d/utility/Logging.h>

namespace {

std::string RepoBinPath() {
    char modulePath[MAX_PATH];
    const DWORD len = GetModuleFileNameA(nullptr, modulePath, MAX_PATH);
    if (len == 0 || len >= MAX_PATH) {
        return ".";
    }
    std::string path(modulePath, len);
    const auto slash = path.find_last_of("\\/");
    if (slash == std::string::npos) {
        return ".";
    }
    return path.substr(0, slash);
}

}  // namespace

int main(int argc, char* argv[]) {
    using namespace open3d;

    utility::SetVerbosityLevel(utility::VerbosityLevel::Info);

    double alpha = 0.03;
    size_t sampleCount = 750;
    if (argc > 1) {
        alpha = std::atof(argv[1]);
    }
    if (argc > 2) {
        sampleCount = static_cast<size_t>(std::atoi(argv[2]));
    }

    utility::LogInfo("Open3D {}", OPEN3D_VERSION);
    utility::LogInfo("Downloading BunnyMesh sample data (first run only)...");
    data::BunnyMesh bunny;

    const std::string bunnyPath = bunny.GetPath();
    auto mesh = io::CreateMeshFromFile(bunnyPath);
    if (mesh == nullptr) {
        utility::LogError("Failed to read {}", bunnyPath);
        return 1;
    }
    mesh->ComputeVertexNormals();

    utility::LogInfo("Sampling {} points from bunny mesh...", sampleCount);
    auto pcd = mesh->SamplePointsPoissonDisk(sampleCount);
    if (pcd == nullptr || pcd->points_.empty()) {
        utility::LogError("Point cloud sampling failed.");
        return 1;
    }

    utility::LogInfo("Running alpha-shape reconstruction (alpha={:.3f})...", alpha);
    auto alphaMesh =
            geometry::TriangleMesh::CreateFromPointCloudAlphaShape(*pcd, alpha);
    if (alphaMesh == nullptr) {
        utility::LogError("Alpha shape reconstruction failed.");
        return 1;
    }
    alphaMesh->ComputeTriangleNormals(true);

    const std::string outDir = RepoBinPath();
    const std::string inputPcd = outDir + "\\alpha_shape_input.pcd";
    const std::string outputPly = outDir + "\\alpha_shape_output.ply";

    io::WritePointCloud(inputPcd, *pcd);
    io::WriteTriangleMesh(outputPly, *alphaMesh, true, true);

    utility::LogInfo("Input point cloud:  {} ({} points)", inputPcd,
                     pcd->points_.size());
    utility::LogInfo("Alpha shape mesh:   {} ({} vertices, {} triangles)",
                     outputPly, alphaMesh->vertices_.size(),
                     alphaMesh->triangles_.size());
    utility::LogInfo(
            "Open the PLY in MeshLab or run the Python example for interactive "
            "visualization.");

    return 0;
}
