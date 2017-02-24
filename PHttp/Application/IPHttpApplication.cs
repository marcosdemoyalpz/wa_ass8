using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{

    public delegate void PreApplicationStartMethod(Type type, string method);
    public delegate void ApplicationStartMethod(Type type, string method);
    public interface IPHttpApplication
    {
        #region Methods
        void Start();
        void ExecuteAction();
        event PreApplicationStartMethod PreApplicationStart;
        event ApplicationStartMethod ApplicationStart;
        #endregion

        #region Properties
        string Name { get; set; }
        #endregion
    }
}
