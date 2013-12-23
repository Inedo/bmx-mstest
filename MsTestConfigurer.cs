using System.ComponentModel;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.MsTest.MsTestConfigurer))]

namespace Inedo.BuildMasterExtensions.MsTest
{
    public sealed class MsTestConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsTestConfigurer"/> class.
        /// </summary>
        public MsTestConfigurer()
        {
        }

        /// <summary>
        /// Gets or sets the path to vstest.console.exe.
        /// </summary>
        [Persistent]
        [DisplayName("VS Test Path")]
        [Description(@"The path to vstest.console.exe, typically: <br /><br />" 
            + @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions " 
            + @"\Microsoft\TestWindow\vstest.console.exe""")]
        public string VsTestPath { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
