using System.ComponentModel;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.Serialization;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.MsTest.MsTestConfigurer))]

namespace Inedo.BuildMasterExtensions.MsTest
{
    public sealed class MsTestConfigurer : ExtensionConfigurerBase
    {
        [Persistent]
        [DisplayName("VS Test Path")]
        [Description(@"The path to vstest.console.exe, typically: <br /><br />" 
            + @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions " 
            + @"\Microsoft\TestWindow\vstest.console.exe""")]
        public string VsTestPath { get; set; }
    }
}
