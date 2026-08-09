namespace Helios.Preview;

public static class PreviewProbe
{
    public static string Runtime => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
}
