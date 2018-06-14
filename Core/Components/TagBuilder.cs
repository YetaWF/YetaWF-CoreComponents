using System;
using System.Collections.Generic;
using System.Text;
#if MVC6
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public enum YTagRenderMode {
        Normal,
        StartTag,
        EndTag,
        SelfClosing
    }

    public class YTagBuilder {

        public string Tag { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; }

        public YTagBuilder(string tag) {
            Tag = tag;
            Attributes = new Dictionary<string, string>();
        }

        public void SetInnerText(string text) {
            InnerHtml = HttpUtility.HtmlEncode(text);
        }

        public string InnerHtml { get; set; }

        public void AddCssClass(string value) {
            string currentValue;
            if (Attributes.TryGetValue("class", out currentValue))
                Attributes["class"] = currentValue + " " + value;
            else
                Attributes["class"] = value;
        }

        public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting) {
            if (attributes != null) {
                foreach (var entry in attributes) {
                    string key = Convert.ToString(entry.Key);
                    string value = Convert.ToString(entry.Value);
                    MergeAttribute(key, value, replaceExisting);
                }
            }
        }
        public void MergeAttribute(string key, string value) {
            MergeAttribute(key, value, replaceExisting: false);
        }
        public void MergeAttribute(string key, string value, bool replaceExisting) {
            if (string.IsNullOrWhiteSpace(key))
                throw new InternalError($"Invalid attribute key");
            if (replaceExisting) {
                Attributes[key] = value;
            } else {
                if (!Attributes.ContainsKey(key))
                    Attributes[key] = value;
                else if (key == "class") // special case for class as it's cumulative
                    AddCssClass(value);
            }
        }
        public new string ToString() {
            return ToString(YTagRenderMode.Normal);
        }
        public string ToString(YTagRenderMode renderMode) {
            StringBuilder sb = new StringBuilder();
            switch (renderMode) {
                case YTagRenderMode.StartTag:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append('>');
                    break;
                case YTagRenderMode.EndTag:
                    sb.Append("</").Append(Tag).Append('>');
                    break;
                case YTagRenderMode.SelfClosing:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append(" />");
                    break;
                default:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append('>').Append(InnerHtml).Append("</").Append(Tag).Append('>');
                    break;
            }
            return sb.ToString();
        }
        public HtmlString ToHtmlString(YTagRenderMode renderMode) {
            return new HtmlString(ToString(renderMode));
        }
        public YHtmlString ToYHtmlString(YTagRenderMode renderMode) {
            return new YHtmlString(ToString(renderMode));
        }
        private void AppendAttributes(StringBuilder sb) {
            foreach (var attribute in Attributes) {
                string key = attribute.Key;
                if (String.Equals(key, "id", StringComparison.Ordinal /* case-sensitive */) && String.IsNullOrEmpty(attribute.Value))
                    continue;
                string value = HttpUtility.HtmlAttributeEncode(attribute.Value);
                sb.Append(' ').Append(key).Append("=\"").Append(value).Append('"');
            }
        }
    }
}
