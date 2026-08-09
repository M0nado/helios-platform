namespace Helios.Preview;

public static class WinUIProbe
{
    public static string RuntimeType => typeof(Microsoft.UI.Xaml.Application).AssemblyQualifiedName!;
}
