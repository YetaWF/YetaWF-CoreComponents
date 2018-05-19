using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
#if MVC6
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFComponent<TYPE> {
        Task IncludeAsync();
        Task<YHtmlString> RenderAsync(TYPE model);
    }

    public abstract class YetaWFComponentBase {

        public const string ComponentSuffix = "Component";

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public enum ComponentType {
            Display = 0,
            Edit = 1,
        }

#if MVC6
        public void SetRenderInfo(IHtmlHelper htmlHelper, object container, string propertyName, PropertyData propData, string fieldName, object htmlAttributes, bool validation)
#else
        public void SetRenderInfo(HtmlHelper htmlHelper, object container, string propertyName, PropertyData propData, string fieldName, object htmlAttributes, bool validation)
#endif
        {
            HtmlHelper = htmlHelper;
            Container = container;
            PropertyName = propertyName;
            PropData = propData;
            FieldName = fieldName;
            HtmlAttributes = htmlAttributes;
            Validation = validation;
        }

#if MVC6
        public IHtmlHelper htmlHelper
#else
        public HtmlHelper HtmlHelper
#endif
        {
            get {
                if (_htmlHelper == null) throw new InternalError("No htmlHelper available");
                return _htmlHelper;
            }
            set {
                _htmlHelper = value;
            }
        }
#if MVC6
        private IHtmlHelper _htmlHelper;
#else
        private HtmlHelper _htmlHelper;
#endif
        protected object Container { get; private set; }
        protected string PropertyName { get; private set; }
        protected PropertyData PropData { get; private set; }
        protected string FieldName { get; private set; }
        protected object HtmlAttributes { get; private set; }
        protected bool Validation { get; private set; }

        public YetaWFComponentBase() {
            Package = GetPackage();
            ComponentName = GetTemplateName();
        }
        public readonly Package Package;
        protected readonly string ComponentName;

        protected string ControlId {
            get {
                if (string.IsNullOrEmpty(_controlId))
                    _controlId = Manager.UniqueId("ctrl");
                return _controlId;
            }
        }
        private string _controlId;

        protected string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = Manager.UniqueId("div");
                return _divId;
            }
        }
        private string _divId;

        protected string UniqueId(string name = "b") {
            return Manager.UniqueId(name);
        }

        public abstract Package GetPackage();
        public abstract string GetTemplateName();
        public abstract ComponentType GetComponentType();

        /// <summary>
        /// Include required JavaScript, Css files for all components in this package.
        /// </summary>
        public virtual Task IncludeStandardAsync() { return Task.CompletedTask; }
    }
}
