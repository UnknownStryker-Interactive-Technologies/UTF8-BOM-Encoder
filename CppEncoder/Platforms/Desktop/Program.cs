using Uno.UI.Hosting;
/*
Copyright © from 2022 to present, UNKNOWN STRYKER. All Rights Reserved.
Licensed under the AGPLv3 License. You may not use this file except in compliance with the License.
*/




namespace CppEncoder;
internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseWin32()
            .Build();

        host.Run();
    }
}
