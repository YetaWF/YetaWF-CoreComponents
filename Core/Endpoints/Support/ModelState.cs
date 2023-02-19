using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YetaWF.Core.Endpoints.Support {

    public class ModelState {

        public class PropertyState {
            public bool Valid { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        protected Dictionary<string, PropertyState> ModelStateDictionary = new Dictionary<string, PropertyState>();

        public bool IsValid {
            get {
                return !(from v in ModelStateDictionary.Values where !v.Valid select v).Any();
            }
        }

        public PropertyState? GetProperty(string name) {
            if (ModelStateDictionary.ContainsKey(name)) 
                return ModelStateDictionary[name];
            return null;
        }

        public void AddValid(string property) {
            ModelStateDictionary.Remove(property);
            ModelStateDictionary.Add(property, new PropertyState { Valid = true });
        }
        public void AddModelError(string property, string message) {
            ModelStateDictionary.Remove(property);
            ModelStateDictionary.Add(property, new PropertyState { Valid = false, Message = message });
        }
        public void RemoveModelState(string property) {
            ModelStateDictionary.Remove(property);
            // remove children
            string prefix = $"{property}.";
            foreach (string key in ModelStateDictionary.Keys) {
                if (key.StartsWith(prefix))
                    ModelStateDictionary.Remove(key);
            }
        }

        public void ValidateModel(object model) {
            EvaluateModel(model, string.Empty, true, true);
        }

        private void EvaluateModel(object? model, string prefix, bool hasTrim, bool hasCase) {
            if (model == null) return;
            Type modelType = model.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(modelType);
            foreach (var prop in props) {

                PropertyInfo pi = prop.PropInfo;
                if (!pi.CanRead || !pi.CanWrite) continue;

                bool valid = true;
                object? o = prop.GetPropertyValue<object?>(model);
                bool trim = false;
                if (hasTrim) {
                    trim = FixTrim(prop, ref o);
                    if (trim)
                        pi.SetValue(model, o);
                }
                bool @case = false;
                if (hasCase) {
                    @case = FixCase(prop, ref o);
                    if (@case)
                        pi.SetValue(model, o);
                }
                //$$$ FixDataAsync

                var caption = prop.GetCaption(model);
                ValidationContext context = new ValidationContext(model, null, null) {
                    DisplayName = string.IsNullOrEmpty(caption) ? prop.Name : caption,
                };

                object[] attrs = prop.PropInfo.GetCustomAttributes(true);
                // validate all fields
                foreach (object attr in attrs) {
                    ValidationAttribute? valAttr = attr as ValidationAttribute;
                    if (valAttr != null) {
                        valid = false;
                        ValidationResult? rs = valAttr.GetValidationResult(o, context);
                        if (rs != null) {
                            string msg = rs.ErrorMessage ?? valAttr.FormatErrorMessage(context.DisplayName);
                            AddModelError(prefix + prop.Name, msg);
                        }
                    }
                }
                if (valid) {
                    AddValid(prefix + prop.Name);
                }

                if (pi.PropertyType == typeof(string)) {
                    // nothing
                } else if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType)) {
                    // nothing
                } else if (pi.PropertyType.IsClass) {
                    EvaluateModel(o, $"{prefix}{prop.Name}.", hasTrim && trim, hasCase && @case);
                }
            }
            // remove fields that aren't processed (we validate all first because conditions can reference
            // other fields which must be completely validated first).
            foreach (var prop in props) {
                bool process = true;// overall whether we need to process this property
                bool found = false;// found an enabling attribute
                if (!found) {
                    if (ExprAttribute.IsRequired(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = true;
                    }
                }
                if (!found) {
                    if (ExprAttribute.IsSelectionRequired(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = true;
                    }
                }
                if (!found) {
                    if (ExprAttribute.IsSuppressed(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = false;
                    }
                }
                if (!process) {
                    // we don't process this property
                    RemoveModelState(prefix + prop.Name);
                }
            }
        }

        private static bool FixTrim(PropertyData prop, ref object? o) {
            TrimAttribute? trimAttr = prop.TryGetAttribute<TrimAttribute>();
            if (trimAttr == null) return false;
            if (o == null) return true;

            TrimAttribute.EnumStyle style = trimAttr.Value;
            if (style != TrimAttribute.EnumStyle.None) {
                PropertyInfo pi = prop.PropInfo;
                if (pi.PropertyType == typeof(MultiString)) {
                    MultiString ms = (MultiString)o;
                    ms.Trim();
                } else if (pi.PropertyType == typeof(string)) {
                    string val = (string)o;
                    switch (style) {
                        default:
                        case TrimAttribute.EnumStyle.None:
                            break;
                        case TrimAttribute.EnumStyle.Both:
                            o = val.Trim();
                            break;
                        case TrimAttribute.EnumStyle.Left:
                            o = val.TrimEnd();
                            break;
                        case TrimAttribute.EnumStyle.Right:
                            o = val.TrimStart();
                            break;
                    }
                }
            }
            return true;
        }

        private static bool FixCase(PropertyData prop, ref object? o) {
            CaseAttribute? caseAttr = prop.TryGetAttribute<CaseAttribute>();
            if (caseAttr == null) return false;
            if (o == null) return true;

            CaseAttribute.EnumStyle style = caseAttr.Value;
            PropertyInfo pi = prop.PropInfo;
            if (pi.PropertyType == typeof(MultiString)) {
                MultiString ms = (MultiString)o;
                ms.Case(style);
            } else if (pi.PropertyType == typeof(string)) {
                string val = (string)o;
                o = style switch {
                    CaseAttribute.EnumStyle.Lower => val.ToLower(),
                    _ => val.ToUpper(),
                };
            }
            return true;
        }
    }
}
