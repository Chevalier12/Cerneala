using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SDL3;

return ShaderCompilerProgram.Run(args);

internal static class ShaderCompilerProgram
{
    private const string ToolVersion = "2";
    private const string ShaderCrossVersion = "3.0.0";
    private const string ShaderCrossPackageVersion = "3.0.0.9";
    private const string ArtifactBytesWindowsOnlyReason =
        "SDL shader artifact bytes are compared only on Windows. The SDL_shadercross " +
        ShaderCrossPackageVersion + " DXC builds for Linux and macOS differ from the Windows " +
        "build: they write a different compiler identity into DXIL, and the macOS build is " +
        "another DXC commit that emits different SPIR-V.";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static int Run(string[] args)
    {
        try
        {
            (string manifestPath, bool verify) = ParseArguments(args);
            CompileManifest(Path.GetFullPath(manifestPath), verify);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static (string ManifestPath, bool Verify) ParseArguments(string[] args)
    {
        bool verify = false;
        string? manifest = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--verify":
                    verify = true;
                    break;
                case "--manifest" when i + 1 < args.Length:
                    manifest = args[++i];
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{args[i]}'.");
            }
        }

        return (manifest ?? Path.Combine(
            "Cerneala.Backends.SdlGpu",
            "Shaders",
            "manifest.json"), verify);
    }

    private static void CompileManifest(string manifestPath, bool verify)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("SDL shader manifest was not found.", manifestPath);
        }

        byte[] manifestBytes = File.ReadAllBytes(manifestPath);
        ShaderManifest manifest = JsonSerializer.Deserialize<ShaderManifest>(
            manifestBytes,
            JsonOptions) ?? throw new InvalidDataException("SDL shader manifest is empty.");
        ValidateManifest(manifest);
        string baseDirectory = Path.GetDirectoryName(manifestPath)!;
        if (!OperatingSystem.IsWindows())
        {
            if (!verify)
            {
                throw new PlatformNotSupportedException(
                    $"{ArtifactBytesWindowsOnlyReason} Generate the SDL shader artifacts on Windows.");
            }
            Console.WriteLine(
                $"{ArtifactBytesWindowsOnlyReason} This host compiles and reflects the shaders " +
                $"and checks the committed artifacts against '{manifest.Metadata}'.");
        }

        if (!ShaderCross.Init())
        {
            throw new InvalidOperationException(
                $"SDL_shadercross initialization failed: {SDL.GetError()}");
        }

        try
        {
            List<CompiledShaderMetadata> compiled = [];
            foreach (ShaderDefinition shader in manifest.Shaders)
            {
                compiled.Add(CompileShader(baseDirectory, shader, verify));
            }

            ShaderArtifactMetadata metadata = new(
                ToolVersion,
                ShaderCrossVersion,
                ShaderCrossPackageVersion,
                HashCanonicalText(manifestBytes),
                ComputeSourceHash(baseDirectory, manifest),
                compiled);
            byte[] metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, JsonOptions);
            string metadataPath = Resolve(baseDirectory, manifest.Metadata);
            VerifyOrWrite(metadataPath, AppendNewline(metadataBytes), verify, textArtifact: true);
            Console.WriteLine(
                $"{(verify ? "Verified" : "Compiled")} {compiled.Count} SDL shader artifacts from '{manifestPath}'.");
        }
        finally
        {
            ShaderCross.Quit();
        }
    }

    private static CompiledShaderMetadata CompileShader(
        string baseDirectory,
        ShaderDefinition shader,
        bool verify)
    {
        string sourcePath = Resolve(baseDirectory, shader.Source);
        string source = File.ReadAllText(sourcePath);
        ShaderCross.ShaderStage stage = shader.Stage switch
        {
            ShaderStage.Vertex => ShaderCross.ShaderStage.Vertex,
            ShaderStage.Fragment => ShaderCross.ShaderStage.Fragment,
            _ => throw new InvalidDataException($"Unsupported shader stage '{shader.Stage}'.")
        };
        ShaderCross.HLSLInfo hlsl = new()
        {
            ManagedSource = source,
            ManagedEntrypoint = shader.EntryPoint,
            ManagedIncludeDir = Path.GetDirectoryName(sourcePath),
            ShaderStage = stage
        };

        byte[] spirv = CopyOwnedBytes(
            ShaderCross.CompileSPIRVFromHLSL(ref hlsl, out nuint spirvSize),
            spirvSize,
            shader,
            "SPIR-V");
        byte[] msl = CompileMsl(spirv, shader, stage);
        ValidateBindingContract(spirv, shader, Reflect(spirv, shader));
        ValidatePortableLimits(shader, shader.Bindings);

        string spirvPath = Resolve(baseDirectory, shader.Outputs.Spirv);
        string dxilPath = Resolve(baseDirectory, shader.Outputs.Dxil);
        string mslPath = Resolve(baseDirectory, shader.Outputs.Msl);
        OutputHashes hashes;
        if (OperatingSystem.IsWindows())
        {
            byte[] dxil = CopyOwnedBytes(
                ShaderCross.CompileDXILFromHLSL(in hlsl, out nuint dxilSize),
                dxilSize,
                shader,
                "DXIL");
            VerifyOrWrite(spirvPath, spirv, verify);
            VerifyOrWrite(dxilPath, dxil, verify);
            VerifyOrWrite(mslPath, msl, verify, textArtifact: true);
            hashes = new OutputHashes(Hash(spirv), Hash(dxil), Hash(msl));
        }
        else
        {
            // The committed artifacts must still be the ones artifacts.json describes.
            hashes = new OutputHashes(
                Hash(ReadCommittedArtifact(spirvPath)),
                Hash(ReadCommittedArtifact(dxilPath)),
                HashCanonicalText(ReadCommittedArtifact(mslPath)));
        }
        return new CompiledShaderMetadata(
            shader.LogicalName,
            shader.Stage,
            shader.EntryPoint,
            shader.Variants,
            shader.Bindings,
            shader.Layout,
            hashes);
    }

    private static byte[] CompileMsl(
        byte[] spirv,
        ShaderDefinition shader,
        ShaderCross.ShaderStage stage)
    {
        GCHandle pinned = GCHandle.Alloc(spirv, GCHandleType.Pinned);
        try
        {
            ShaderCross.SPIRVInfo info = new()
            {
                ByteCode = pinned.AddrOfPinnedObject(),
                ByteCodeSize = (nuint)spirv.Length,
                ManagedEntrypoint = shader.EntryPoint,
                ShaderStage = stage
            };
            nint pointer = ShaderCross.TranspileMSLFromSPIRV(in info);
            if (pointer == 0)
            {
                throw CompileFailure(shader, "MSL");
            }
            try
            {
                string text = Marshal.PtrToStringUTF8(pointer) ??
                    throw new InvalidDataException(
                        $"Shader '{shader.LogicalName}' returned empty MSL source.");
                return Encoding.UTF8.GetBytes(text.TrimEnd('\r', '\n') + "\n");
            }
            finally
            {
                SDL.Free(pointer);
            }
        }
        finally
        {
            pinned.Free();
        }
    }

    private static ReflectedBindings Reflect(byte[] spirv, ShaderDefinition shader)
    {
        GCHandle pinned = GCHandle.Alloc(spirv, GCHandleType.Pinned);
        try
        {
            nint pointer = ShaderCross.ReflectGraphicsSPIRV(
                pinned.AddrOfPinnedObject(),
                (nuint)spirv.Length,
                0);
            if (pointer == 0)
            {
                throw CompileFailure(shader, "SPIR-V reflection");
            }
            try
            {
                ShaderCross.GraphicsShaderMetadata metadata =
                    Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(pointer);
                return new ReflectedBindings(
                    metadata.ResourceInfo.NumSamplers,
                    metadata.ResourceInfo.NumStorageTextures,
                    metadata.ResourceInfo.NumStorageBuffers,
                    metadata.ResourceInfo.NumUniformBuffers,
                    metadata.NumInputs,
                    metadata.NumOutputs);
            }
            finally
            {
                SDL.Free(pointer);
            }
        }
        finally
        {
            pinned.Free();
        }
    }

    // SDL_GPU binds resources by slot, so the create info must count every slot the
    // shader declares, including slots its entry point never reads. SDL_shadercross
    // reflection counts only the resources the entry point reads; the declared SPIR-V
    // bindings are the slot contract. A texture the entry point only loads, without its
    // sampler, is reflected as a storage texture, so reflection is compared with the
    // declared slot total rather than per resource kind.
    private static void ValidateBindingContract(
        byte[] spirv,
        ShaderDefinition shader,
        ReflectedBindings reflected)
    {
        ReflectedBindings declared = shader.Bindings;
        (uint resourceSet, uint uniformSet) = shader.Stage == ShaderStage.Vertex
            ? (0u, 1u)
            : (2u, 3u);
        uint declaredResources =
            declared.Samplers + declared.StorageTextures + declared.StorageBuffers;
        Dictionary<uint, SortedSet<uint>> bindings = ReadDescriptorBindings(spirv, shader);
        if (reflected.Samplers + reflected.StorageTextures + reflected.StorageBuffers >
                declaredResources ||
            reflected.UniformBuffers > declared.UniformBuffers ||
            reflected.Inputs != declared.Inputs ||
            reflected.Outputs != declared.Outputs ||
            bindings.Keys.Any(set => set != resourceSet && set != uniformSet) ||
            !IsContiguous(bindings, resourceSet, declaredResources) ||
            !IsContiguous(bindings, uniformSet, declared.UniformBuffers))
        {
            throw new InvalidDataException(
                $"Shader '{shader.LogicalName}' reflection mismatch. " +
                $"Manifest={declared}; reflected={reflected}; SPIR-V bindings: " +
                string.Join("; ", bindings.Select(static pair =>
                    $"set {pair.Key}=[{string.Join(",", pair.Value)}]")) + ".");
        }
    }

    private static Dictionary<uint, SortedSet<uint>> ReadDescriptorBindings(
        byte[] spirv,
        ShaderDefinition shader)
    {
        const uint magic = 0x07230203;
        const uint opDecorate = 71;
        const uint decorationBinding = 33;
        const uint decorationDescriptorSet = 34;
        ReadOnlySpan<uint> words = MemoryMarshal.Cast<byte, uint>(spirv);
        if (spirv.Length % sizeof(uint) != 0 || words.Length < 5 || words[0] != magic)
        {
            throw new InvalidDataException($"Shader '{shader.LogicalName}' produced invalid SPIR-V.");
        }

        Dictionary<uint, uint> sets = [];
        Dictionary<uint, uint> slots = [];
        for (int index = 5; index < words.Length;)
        {
            int wordCount = (int)(words[index] >> 16);
            if (wordCount == 0 || index + wordCount > words.Length)
            {
                throw new InvalidDataException($"Shader '{shader.LogicalName}' produced invalid SPIR-V.");
            }
            if ((words[index] & 0xFFFF) == opDecorate && wordCount == 4)
            {
                if (words[index + 2] == decorationDescriptorSet)
                {
                    sets[words[index + 1]] = words[index + 3];
                }
                else if (words[index + 2] == decorationBinding)
                {
                    slots[words[index + 1]] = words[index + 3];
                }
            }
            index += wordCount;
        }

        Dictionary<uint, SortedSet<uint>> bindings = [];
        foreach ((uint variable, uint set) in sets)
        {
            if (!slots.TryGetValue(variable, out uint slot))
            {
                throw new InvalidDataException(
                    $"Shader '{shader.LogicalName}' declares a descriptor set without a binding.");
            }
            if (!bindings.TryGetValue(set, out SortedSet<uint>? setSlots))
            {
                setSlots = [];
                bindings.Add(set, setSlots);
            }
            setSlots.Add(slot);
        }
        return bindings;
    }

    private static bool IsContiguous(
        Dictionary<uint, SortedSet<uint>> bindings,
        uint set,
        uint count) =>
        bindings.TryGetValue(set, out SortedSet<uint>? slots)
            ? slots.Count == count && slots.Min == 0 && slots.Max == count - 1
            : count == 0;

    private static byte[] CopyOwnedBytes(
        nint pointer,
        nuint size,
        ShaderDefinition shader,
        string format)
    {
        if (pointer == 0 || size == 0 || size > int.MaxValue)
        {
            if (pointer != 0)
            {
                SDL.Free(pointer);
            }
            throw CompileFailure(shader, format);
        }

        try
        {
            byte[] bytes = new byte[(int)size];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            SDL.Free(pointer);
        }
    }

    private static InvalidOperationException CompileFailure(
        ShaderDefinition shader,
        string format) => new(
            $"Shader '{shader.LogicalName}' failed to compile/reflect as {format}: {SDL.GetError()}");

    private static void VerifyOrWrite(
        string path,
        byte[] expected,
        bool verify,
        bool textArtifact = false)
    {
        byte[] canonicalExpected = textArtifact ? CanonicalizeText(expected) : expected;
        if (verify)
        {
            if (!File.Exists(path) ||
                !ArtifactMatches(File.ReadAllBytes(path), canonicalExpected, textArtifact))
            {
                throw MissingOrStaleArtifact(path);
            }
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) ||
            !ArtifactMatches(File.ReadAllBytes(path), canonicalExpected, textArtifact))
        {
            File.WriteAllBytes(path, canonicalExpected);
        }
    }

    private static byte[] ReadCommittedArtifact(string path) =>
        File.Exists(path) ? File.ReadAllBytes(path) : throw MissingOrStaleArtifact(path);

    private static InvalidDataException MissingOrStaleArtifact(string path) =>
        new($"SDL shader artifact '{path}' is missing or stale.");

    private static bool ArtifactMatches(
        byte[] actual,
        byte[] canonicalExpected,
        bool textArtifact)
    {
        ReadOnlySpan<byte> canonicalActual = textArtifact
            ? CanonicalizeText(actual)
            : actual;
        return canonicalActual.SequenceEqual(canonicalExpected);
    }

    private static string ComputeSourceHash(string baseDirectory, ShaderManifest manifest)
    {
        SortedSet<string> files = new(StringComparer.Ordinal);
        foreach (ShaderDefinition shader in manifest.Shaders)
        {
            files.Add(Resolve(baseDirectory, shader.Source));
        }
        foreach (string root in manifest.CommonSourceRoots)
        {
            string fullRoot = Resolve(baseDirectory, root);
            foreach (string file in Directory.EnumerateFiles(
                fullRoot,
                "*.hlsl",
                SearchOption.AllDirectories))
            {
                files.Add(file);
            }
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(
                Path.GetRelativePath(baseDirectory, file).Replace('\\', '/')));
            hash.AppendData([0]);
            hash.AppendData(CanonicalizeText(File.ReadAllBytes(file)));
            hash.AppendData([0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void ValidateManifest(ShaderManifest manifest)
    {
        if (manifest.ToolVersion != ToolVersion ||
            manifest.ShaderCrossVersion != ShaderCrossVersion ||
            manifest.ShaderCrossPackageVersion != ShaderCrossPackageVersion)
        {
            throw new InvalidDataException(
                $"Manifest requires tool {manifest.ToolVersion}/ShaderCross " +
                $"{manifest.ShaderCrossVersion} package {manifest.ShaderCrossPackageVersion}; " +
                $"this compiler is {ToolVersion}/{ShaderCrossVersion} package " +
                $"{ShaderCrossPackageVersion}.");
        }
        if (string.IsNullOrWhiteSpace(manifest.Metadata) || manifest.Shaders.Count == 0)
        {
            throw new InvalidDataException("SDL shader manifest must declare metadata and shaders.");
        }
        string[] requiredFormats = ["spirv", "dxil", "msl"];
        if (!manifest.OutputFormats.SequenceEqual(requiredFormats, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                "SDL shader manifest outputFormats must be exactly: spirv, dxil, msl.");
        }
        string? duplicate = manifest.Shaders
            .GroupBy(static shader => shader.LogicalName, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1)?.Key;
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate SDL shader logical name '{duplicate}'.");
        }
        foreach (ShaderDefinition shader in manifest.Shaders)
        {
            if (string.IsNullOrWhiteSpace(shader.LogicalName) ||
                string.IsNullOrWhiteSpace(shader.Source) ||
                string.IsNullOrWhiteSpace(shader.EntryPoint) ||
                shader.Variants.Count == 0 ||
                shader.Variants.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException(
                    "Every SDL shader must declare a logical name, source, entry point, and variants.");
            }

            ValidateInterfaceLayout(shader);
        }
    }

    private static void ValidateInterfaceLayout(ShaderDefinition shader)
    {
        ValidateBindings(
            shader,
            "uniform buffers",
            shader.Layout.UniformBuffers,
            shader.Bindings.UniformBuffers);
        ValidateBindings(
            shader,
            "samplers",
            shader.Layout.Samplers,
            shader.Bindings.Samplers);
        ValidateBindings(
            shader,
            "storage textures",
            shader.Layout.StorageTextures,
            shader.Bindings.StorageTextures);
        ValidateBindings(
            shader,
            "storage buffers",
            shader.Layout.StorageBuffers,
            shader.Bindings.StorageBuffers);

        uint expectedVertexInputs = shader.Stage == ShaderStage.Vertex
            ? shader.Bindings.Inputs
            : 0;
        if (shader.Layout.VertexInputs.Count != expectedVertexInputs)
        {
            throw new InvalidDataException(
                $"Shader '{shader.LogicalName}' declares {shader.Layout.VertexInputs.Count} " +
                $"vertex inputs but its stage/reflection contract requires {expectedVertexInputs}.");
        }

        ValidateUniqueNames(
            shader,
            "vertex inputs",
            shader.Layout.VertexInputs.Select(static input => input.Semantic));
        for (int i = 0; i < shader.Layout.VertexInputs.Count; i++)
        {
            VertexInputBinding input = shader.Layout.VertexInputs[i];
            if (input.Location != (uint)i || string.IsNullOrWhiteSpace(input.Format))
            {
                throw new InvalidDataException(
                    $"Shader '{shader.LogicalName}' vertex inputs must have contiguous zero-based " +
                    "locations and a format.");
            }
        }
    }

    private static void ValidateBindings(
        ShaderDefinition shader,
        string kind,
        IReadOnlyList<NamedBinding> bindings,
        uint reflectedCount)
    {
        if (bindings.Count != reflectedCount)
        {
            throw new InvalidDataException(
                $"Shader '{shader.LogicalName}' declares {bindings.Count} {kind} but reflection " +
                $"reports {reflectedCount}.");
        }

        ValidateUniqueNames(shader, kind, bindings.Select(static binding => binding.Name));
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].Slot != (uint)i)
            {
                throw new InvalidDataException(
                    $"Shader '{shader.LogicalName}' {kind} must use contiguous zero-based slots.");
            }
        }
    }

    private static void ValidateUniqueNames(
        ShaderDefinition shader,
        string kind,
        IEnumerable<string> names)
    {
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || !unique.Add(name))
            {
                throw new InvalidDataException(
                    $"Shader '{shader.LogicalName}' {kind} must have non-empty, unique names.");
            }
        }
    }

    private static void ValidatePortableLimits(
        ShaderDefinition shader,
        ReflectedBindings bindings)
    {
        const uint maxSamplers = 16;
        const uint maxStorageTextures = 4;
        const uint maxStorageBuffers = 4;
        const uint maxUniformBuffers = 4;
        const uint maxInputs = 32;
        const uint maxOutputs = 32;
        if (bindings.Samplers > maxSamplers ||
            bindings.StorageTextures > maxStorageTextures ||
            bindings.StorageBuffers > maxStorageBuffers ||
            bindings.UniformBuffers > maxUniformBuffers ||
            bindings.Inputs > maxInputs ||
            bindings.Outputs > maxOutputs)
        {
            throw new InvalidDataException(
                $"Shader '{shader.LogicalName}' exceeds Cerneala's portable SDL_GPU binding limits: " +
                $"{bindings}.");
        }
    }

    private static byte[] AppendNewline(byte[] value)
    {
        byte[] result = new byte[value.Length + 1];
        value.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }

    private static string Resolve(string baseDirectory, string path) =>
        Path.GetFullPath(path, baseDirectory);

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string HashCanonicalText(ReadOnlySpan<byte> bytes) =>
        Hash(CanonicalizeText(bytes));

    private static byte[] CanonicalizeText(ReadOnlySpan<byte> bytes) =>
        Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(bytes).ReplaceLineEndings("\n"));
}

internal sealed record ShaderManifest(
    string ToolVersion,
    string ShaderCrossVersion,
    string ShaderCrossPackageVersion,
    string Metadata,
    IReadOnlyList<string> OutputFormats,
    IReadOnlyList<string> CommonSourceRoots,
    IReadOnlyList<ShaderDefinition> Shaders);

internal sealed record ShaderDefinition(
    string LogicalName,
    string Source,
    ShaderStage Stage,
    string EntryPoint,
    IReadOnlyList<string> Variants,
    ReflectedBindings Bindings,
    ShaderInterfaceLayout Layout,
    ShaderOutputs Outputs);

internal enum ShaderStage
{
    Vertex,
    Fragment
}

internal sealed record ShaderOutputs(string Spirv, string Dxil, string Msl);

internal sealed record ReflectedBindings(
    uint Samplers,
    uint StorageTextures,
    uint StorageBuffers,
    uint UniformBuffers,
    uint Inputs,
    uint Outputs);

internal sealed record ShaderInterfaceLayout(
    IReadOnlyList<NamedBinding> UniformBuffers,
    IReadOnlyList<NamedBinding> Samplers,
    IReadOnlyList<NamedBinding> StorageTextures,
    IReadOnlyList<NamedBinding> StorageBuffers,
    IReadOnlyList<VertexInputBinding> VertexInputs);

internal sealed record NamedBinding(string Name, uint Slot);

internal sealed record VertexInputBinding(string Semantic, uint Location, string Format);

internal sealed record ShaderArtifactMetadata(
    string ToolVersion,
    string ShaderCrossVersion,
    string ShaderCrossPackageVersion,
    string ManifestHash,
    string SourceHash,
    IReadOnlyList<CompiledShaderMetadata> Shaders);

internal sealed record CompiledShaderMetadata(
    string LogicalName,
    ShaderStage Stage,
    string EntryPoint,
    IReadOnlyList<string> Variants,
    ReflectedBindings Bindings,
    ShaderInterfaceLayout Layout,
    OutputHashes Outputs);

internal sealed record OutputHashes(string Spirv, string Dxil, string Msl);
