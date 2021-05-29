/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// <param name="modelState"></param>
        internal void Update(ModelStateDictionary modelState, IDictionary<string, object> actionArguments) {
            // TODO: Once we have a use case, support for actionArguments needs to be fixed
            foreach (var argEntry in actionArguments) {
                EvalArg(argEntry.Key, argEntry.Value, modelState);
            }
        }

        private void EvalArg(string key, object container, ModelStateDictionary modelState) {
            foreach (ErrorInstance error in Errors) {
                if (error.PropertyName == null && error.HardcodedMessage != null) {
                    foreach (string modelStateKey in modelState.Keys) {
                        ModelStateEntry modelEntry = modelState[modelStateKey]!;
                        if (modelEntry.Errors.FirstOrDefault()?.ErrorMessage == error.HardcodedMessage) {
                            error.PropertyName = modelStateKey;// save property name that matches this error
                            string? caption = modelStateKey;
                            if (container != null) {
                                PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), modelStateKey);
                                caption = propData.GetCaption(container);
                            }
                            switch (error.NetCoreErrorName) {
                                case "ValueMustNotBeNullAccessor":
                                    modelEntry.Errors.Clear();
                                    modelEntry.Errors.Add(this.__ResStr("nullVal", "The value '{0}' is not valid for field '{1}'", error.AttemptedValue, caption));
                                    break;
                            }
                        }
                    }
                } else if (error.PropertyName != null) {
                    ModelStateEntry? modelEntry = modelState[error.PropertyName];
                    if (modelEntry != null) {
                        switch (error.NetCoreErrorName) {
                            case "AttemptedValueIsInvalidAccessor":
                                string? caption = error.PropertyName;
                                if (container != null) {
                                    PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), error.PropertyName);
                                    caption = propData.GetCaption(container);
                                }
                                modelEntry.Errors.Clear();
                                modelEntry.Errors.Add(this.__ResStr("invVal", "The value '{0}' is not valid for field '{1}'", error.AttemptedValue, caption));
                                break;
                        }
                    }
                }
            }
        }

        public bool TryGetAttemptedValue(string propertyName, [MaybeNullWhen(false)] out string attemptedValue) {
            attemptedValue = null;
            ErrorInstance? error = Errors.Where((x) => x.PropertyName == propertyName).FirstOrDefault();
            if (error == null) return false;
            attemptedValue = error.AttemptedValue;
            return true;
        }
    }
}
