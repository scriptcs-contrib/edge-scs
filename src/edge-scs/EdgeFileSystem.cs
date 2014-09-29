using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptCs;

namespace EdgeScs
{
    public class EdgeFileSystem : FileSystem
    {
        public EdgeFileSystem()
        {
            _hostBin = Path.GetDirectoryName(typeof (EdgeCompiler).Assembly.Location);
        }

        private string _hostBin;

        public override string HostBin
        {
            get { return _hostBin; }
        }

        public override string ModulesFolder
        {
            get { return ""; }
        }
    }
}
