using System.Runtime.InteropServices;

var values = new[] { 1.0, 2.0, 3.0, 4.0 };
Console.WriteLine($"abi={Native.helios_native_abi_version()}");
Console.WriteLine($"sum={Native.helios_vector_sum(values, (nuint)values.Length)}");
Console.WriteLine($"mean={Native.helios_vector_mean(values, (nuint)values.Length)}");

internal static partial class Native
{
    private const string LibraryName = "HELIOSNativePerformance";

    [LibraryImport(LibraryName)]
    internal static partial int helios_native_abi_version();

    [LibraryImport(LibraryName)]
    internal static partial double helios_vector_sum(double[] values, nuint length);

    [LibraryImport(LibraryName)]
    internal static partial double helios_vector_mean(double[] values, nuint length);
}
