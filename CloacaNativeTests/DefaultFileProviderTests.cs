using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CloacaNative.IO;

namespace CloacaNativeTests
{
    [TestFixture]
    public class DefaultFileProviderTests
    {
        [Test]
        public void Open_FileExists()
        {
            var provider = new DefaultFileProvider();
        }
    }
}
