using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if MVC6
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public class YHtmlString : HtmlString { 
        public YHtmlString(string value) : base(value) { }
    }

}
