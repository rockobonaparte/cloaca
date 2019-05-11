using System;
using System.Diagnostics;
using System.Reflection;

namespace Piksel.LibREPL
{
    public class SoftwareInfo
    {
        private Version _version;
        public Version Version {
            get {
                if (_version == null)
                    GetInfoFromExecutingAssembly();
                return _version;
            }
            set {
                _version = value;
            }
        }

        private string _copyright;
        public string Copyright
        {
            get
            {
                if (_copyright == null)
                    GetInfoFromExecutingAssembly();
                return _copyright;
            }
            set
            {
                _copyright = value;
            }
        }

        private string _company;
        public string Company
        {
            get
            {
                if (_company == null)
                    GetInfoFromExecutingAssembly();
                return _company;
            }
            set
            {
                _company = value;
            }
        }

        public string License { get; set; }

        public string Message { get; set; }

        private string _name;
        public string Name
        {
            get
            {
                if (_name == null)
                    GetInfoFromExecutingAssembly();
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public void GetInfoFromExecutingAssembly()
        {
            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            _version = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart, fvi.ProductPrivatePart);
            _name = fvi.ProductName;
            _copyright = fvi.LegalCopyright;
            _company = fvi.CompanyName;
        }
    }
}