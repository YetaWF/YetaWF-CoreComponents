#if MVC6
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public class YHtmlString : HtmlString {
        public YHtmlString(string value) : base(value) { }
        public YHtmlString() : base("") { }
    }
}
