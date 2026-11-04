namespace WaterAndTarPools.Core;

/// <summary>Marks this build as the KydlDev spec layout (MIGRATION_DECISIONS A1); ArchitectureRulesTests looks for the tag in the built DLL.</summary>
internal static class Architecture
{
    internal static readonly string Tag = "WaterAndTarPools-KYDL-SPEC-V1";
}