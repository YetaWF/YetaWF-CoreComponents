/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace YetaWF2.Support {

    // This was copied from .net core 3.1/5.0 .\src\Mvc\Mvc.Core\src\ModelBinding\Binders\SimpleTypeModelBinder.cs and
    // adapted for string types only.

    public class YetaWFStringModelBinderProvider : IModelBinderProvider {

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.Metadata.IsComplexType) {
                Type modelType = context.Metadata.UnderlyingOrModelType;
                if (modelType == typeof(string))
                {
                    var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                    return new YetaWFSimpleTypeModelBinder(context.Metadata.ModelType, loggerFactory);
                }
            }
            return null;
        }
    }

    // Copyright (c) .NET Foundation. All rights reserved.
    // Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

    /// <summary>
    /// An <see cref="IModelBinder"/> for simple types.
    /// </summary>
    public class YetaWFSimpleTypeModelBinder : IModelBinder {
        private readonly TypeConverter _typeConverter;
        //private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="YetaWFSimpleTypeModelBinder"/>.
        /// </summary>
        /// <param name="type">The type to create binder for.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public YetaWFSimpleTypeModelBinder(Type type, ILoggerFactory loggerFactory) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (loggerFactory == null) {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _typeConverter = TypeDescriptor.GetConverter(type);
            //_logger = loggerFactory.CreateLogger<YetaWFSimpleTypeModelBinder>();
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext) {
            if (bindingContext == null) {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None) {
                //_logger.FoundNoValueInRequest(bindingContext);

                // no entry
                //_logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            //_logger.AttemptingToBindModel(bindingContext);

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            try {
                var value = valueProviderResult.FirstValue;

                object? model;
                if (bindingContext.ModelType == typeof(string)) {
                    // Already have a string. No further conversion required but handle ConvertEmptyStringToNull.
                    // if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && string.IsNullOrWhiteSpace(value)) {
                    if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && string.IsNullOrEmpty(value)) { // THIS is the only reason we did all this (don't check for white space) #WhitespaceMatters
                        model = null;
                    } else {
                        model = value;
                    }
                } else {
                    throw new NotSupportedException();
                }

                CheckModel(bindingContext, valueProviderResult, model);

                //_logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            } catch (Exception exception) {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null) {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we capture the inner exception.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);

                // Were able to find a converter for the type but conversion failed.
                return Task.CompletedTask;
            }
        }

        protected virtual void CheckModel(
            ModelBindingContext bindingContext,
            ValueProviderResult valueProviderResult,
            object? model) {
            // When converting newModel a null value may indicate a failed conversion for an otherwise required
            // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
            // current bindingContext. If not, an error is logged.
            if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType) {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                        valueProviderResult.ToString()));
            } else {
                bindingContext.Result = ModelBindingResult.Success(model);
            }
        }
    }
}
