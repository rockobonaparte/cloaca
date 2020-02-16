using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CloacaNative;
using CloacaNative.IO;
using LanguageImplementation.DataTypes;
using CloacaNative.IO.DataTypes;

namespace CloacaNativeTests
{
    [TestFixture]
    public class NativeResourceManagerTests
    {
        [Test]
        public void RegisterProvider_Available()
        {
            var resourceManager = new NativeResourceManager();
            var provider = new DefaultFileProvider();
            resourceManager.RegisterProvider<INativeFileProvider>(provider);

            var gotProvider = resourceManager.TryGetProvider<INativeFileProvider>(out INativeFileProvider result);

            Assert.IsTrue(gotProvider);
        }

        public void RegisterProvider_NotAvailable()
        {
            var resourceManager = new NativeResourceManager();
            var gotProvider = resourceManager.TryGetProvider<INativeFileProvider>(out INativeFileProvider result);

            Assert.IsFalse(gotProvider);
        }

        [Test]
        public void RegisterSameProvider_Exception()
        {
            var resourceManager = new NativeResourceManager();
            var provider = new DefaultFileProvider();
            resourceManager.RegisterProvider<INativeFileProvider>(provider);

            Assert.Catch(
                typeof(Exception),
                () =>
                {
                    resourceManager.RegisterProvider<INativeFileProvider>(provider);
                });
        }

        [Test]
        public void open_func_ProviderNotAvailable()
        {
            //var resourceManager = new NativeResourceManager();           
            //var result = resourceManager.open_func("test.dat", "r");

            //Assert.AreEqual(NoneType.Instance, result);
        }

        [Test]
        public void open_func_ProviderAvailable()
        {
            //CreateTestFile();

            //var resourceManager = new NativeResourceManager();
            //var provider = new DefaultFileProvider();
            //resourceManager.RegisterProvider<INativeFileProvider>(provider);
            //var result = resourceManager.open_func("test.dat", "r");

            //Assert.IsInstanceOf<PyTextIOWrapper>(result);
        }

        private void CreateTestFile()
        {
            using (var writer = new StreamWriter("test.dat"))
            {
                writer.WriteLine("Look at my waistcoat.");
                writer.WriteLine("It's made of roast beef!");
            }
        }
    }
}
