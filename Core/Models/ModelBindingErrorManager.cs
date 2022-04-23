/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models {

    /// <summary>
    /// An instance of this class keeps track of model binding errors for the current request.
    /// </summary>
    /// <remarks>Model binding errors are not easily localizable. They are intercepted in Startup Configure (services.AddMvc()) and added to the error list in the ModelBindingErrorManager instance.
    /// The base controller implementation has access to the error information and can localize the error message.
    /// Components have access to the error information and can use the original "raw" value for rendering if desired. As an example, the DateEdit component uses the attempted value to render an invalid value.</remarks>
    public class ModelBindingErrorManager {

        /// <summary>
        /// Hardcoded message for SetAttemptedValueIsInvalidAccessor, which is used to later localize the message, including reference to the caption if available.
        /// </summary>
        public const string AttemptedValueIsInvalidMessage = "The value '' is not valid";

        internal class ErrorInstance {
            public string NetCoreErrorName { get; set; } = null!;
            public string? PropertyName { get; set; }
            public string? HardcodedMessage { get; set; }
            public string AttemptedValue { get; set; } = null!;
        }

        private readonly List<ErrorInstance> Errors = new List<ErrorInstance>();
        private readonly List<ModelStateEntry> Handled = new List<ModelStateEntry>();

        public void AddError(string netCoreErrorName, string? propertyName, string? hardcodedMessage, string attemptedValue) {
            Errors.Add(new ErrorInstance {
                NetCoreErrorName = netCoreErrorName,
                PropertyName = propertyName,
                HardcodedMessage = hardcodedMessage,
                AttemptedValue = attemptedValue,
            });
        }

        /// <summary>
        /// Replace any standard .NET Core messages with their localized equivalent.
        /// </summary>
        internal void Update(ModelStateDictionary modelState, IDictionary<string, object> actionArguments) {
            // TODO: Once we have a use case, support for actionArguments needs to be fixed
            foreach (var argEntry in actionArguments) {
                UpdateModelState(modelState, argEntry.Value);
            }
        }

        internal void UpdateModelState(ModelStateDictionary modelState, object container) {
            foreach (ErrorInstance error in Errors) {
                if (error.PropertyName == null && error.HardcodedMessage != null) {
                    foreach (string modelStateKey in modelState.Keys) {
                        string? key = FindModelStateKey(modelState, modelStateKey);
                        if (key != null) {
                            ModelStateEntry? modelEntry = modelState[key];
                            if (modelEntry != null && modelEntry.Errors.FirstOrDefault()?.ErrorMessage == error.HardcodedMessage) {
                                error.PropertyName = modelStateKey;// save property name that matches this error
                                switch (error.NetCoreErrorName) {
                                    case "ValueMustNotBeNullAccessor":
                                        string? caption = GetCaption(container, modelStateKey);
                                        modelEntry.Errors.Clear();
                                        modelEntry.Errors.Add(this.__ResStr("nullVal", "The value '{0}' is not valid for field '{1}'", error.AttemptedValue, caption));
                                        Handled.Add(modelEntry);
                                        break;
                                }
                            }
                        }
                    }
                } else if (error.PropertyName != null) {
                    string? key = FindModelStateKey(modelState, error.PropertyName);
                    if (key != null) {
                        ModelStateEntry? modelEntry = modelState[key];
                        if (modelEntry != null) { 
                            switch (error.NetCoreErrorName) {
                                case "AttemptedValueIsInvalidAccessor":
                                    string? caption = GetCaption(container, key);
                                    modelEntry.Errors.Clear();
                                    modelEntry.Errors.Add(this.__ResStr("invVal", "The value '{0}' is not valid for field '{1}'", error.AttemptedValue, caption));
                                    Handled.Add(modelEntry);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private string? FindModelStateKey(ModelStateDictionary modelState, string propertyName) {
            string? key = modelState.Keys.FirstOrDefault((x) => x == propertyName && !Handled.Contains(modelState[x]!));
            if (key == null)
                key = modelState.Keys.FirstOrDefault((x) => x.EndsWith($".{propertyName}") && !Handled.Contains(modelState[x]!));
            if (key == null)
                return null;
            return key;
        }
        private string? GetCaption(object? container, string propertyName) {
            if (container == null) return null;
            string[] keys = propertyName.Split(new char[] { '.' }, 2);
            if (keys.Length == 1) {
                PropertyData? propData = ObjectSupport.TryGetPropertyData(container.GetType(), keys[0]);
                if (propData != null)
                    return propData.GetCaption(container);
            } else {// 2 keys
                string key = keys[0];
                string subProp = keys[1];
                if (key.IndexOf('[') >= 0 && subProp.Length > 0) {
                    // enumerable property (List, SerializableList, etc.)
                    string prop = key.RemoveStartingAt('[');
                    PropertyInfo? pi = ObjectSupport.TryGetProperty(container.GetType(), prop);
                    if (pi != null) {
                        if (ObjectSupport.TryGetPropertyValue<object?>(container, prop, out object? parm)) { 
                            if (parm != null && parm is IEnumerable<object?> ienum) {
                                IEnumerator<object?> ienumerator = ienum.GetEnumerator();
                                if (ienumerator.MoveNext()) {
                                    object? val = ienumerator.Current;
                                    return GetCaption(val, subProp);
                                }
                            }
                        }
                    }
                } else {
                    if (ObjectSupport.TryGetPropertyValue<object?>(container, key, out object? value))
                        return GetCaption(value, key);
                }
            }
            return null;
        }


        public bool TryGetAttemptedValue(string propertyName, [MaybeNullWhen(false)] out string attemptedValue) {
            attemptedValue = null;
            ErrorInstance? error = Errors.FirstOrDefault((x) => x.PropertyName == propertyName);
            if (error == null) return false;
            attemptedValue = error.AttemptedValue;
            return true;
        }
    }
}
