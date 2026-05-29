// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "AgentPet",
    platforms: [.macOS(.v13)],
    targets: [
        .target(
            name: "AgentPetCore",
            path: "Sources/AgentPetCore",
            resources: [.copy("Pets")]
        ),
        .executableTarget(
            name: "agentpet",
            dependencies: ["AgentPetCore"],
            path: "Sources/App"
        ),
        .testTarget(
            name: "AgentPetCoreTests",
            dependencies: ["AgentPetCore"],
            path: "Tests/AgentPetCoreTests"
        ),
    ]
)
