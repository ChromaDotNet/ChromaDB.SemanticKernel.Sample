// Copyright (c) Microsoft. All rights reserved.

using Docker.DotNet;
using Docker.DotNet.Models;

namespace Memory.VectorStoreFixtures;

/// <summary>
/// Helper class that creates and deletes containers for the vector store examples.
/// </summary>
internal static class VectorStoreInfra
{
    /// <summary>
    /// Setup the chroma container by pulling the image and running it.
    /// </summary>
    /// <param name="client">The docker client to create the container with.</param>
    /// <returns>The id of the container.</returns>
    public static async Task<string> SetupChromaContainerAsync(DockerClient client)
    {
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "chromadb/chroma",
                Tag = "1.5.9",
            },
            null,
            new Progress<JSONMessage>());

        var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters()
        {
            Image = "chromadb/chroma:1.5.9",
            HostConfig = new HostConfig()
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {"8000", new List<PortBinding> {new() {HostPort = "8000" } }},
                },
                PublishAllPorts = true
            },
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "8000", default },
            },
        });

        await client.Containers.StartContainerAsync(
            container.ID,
            new ContainerStartParameters());

        return container.ID;
    }

    /// <summary>
    /// Stop and delete the container with the specified id.
    /// </summary>
    /// <param name="client">The docker client to delete the container in.</param>
    /// <param name="containerId">The id of the container to delete.</param>
    /// <returns>An async task.</returns>
    public static async Task DeleteContainerAsync(DockerClient client, string containerId)
    {
        await client.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
        await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters());
    }
}
