/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Extensions;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Support.Serializers {

    //PERFORMANCE: serializing/deserializing is a significant hit on performance. RESEARCH: Alternatives
    // Turns out JSON is slower than Simple (used in session state, cached main menu is a bit of a problem)

    public interface IYetaWFFormatter {
        object? Deserialize(Stream serializationStream);
        void Serialize(Stream serializationStream, object? graph);
    }

    /// <summary>
    /// General serializer/deserializer
    /// </summary>
    /// <remarks>It determines the format when deserializing and calls the appropriate deserializer (simple, text, binary).
    /// When serializing it uses the globally defined format. For debugging it's best to use "text", and in production "simple". "binary" is not supported.
    /// This means there can be a mix of formats in production which will be unified when saving data and when data is restored.</remarks>
    public class GeneralFormatter {

        public enum Style {
            /// <summary>
            /// Serialization as XML data.
            /// </summary>
            Xml = 0,
            /// <summary>
            /// Serialization in an internal format. Can be used for large data files, streams.
            /// </summary>
            Simple = 1,
            /// <summary>
            /// Used for small amounts of data that is in memory (usually cache and small data files).
            /// </summary>
            Simple2 = 3,
            /// <summary>
            /// JSON serialization.
            /// </summary>
            JSON = 4,
            /// <summary>
            /// JSON serialization with type information.
            /// </summary>
            JSONTyped = 6,
        };

        public GeneralFormatter(Style format) {
            Format = format;
            FormatExplicit = true;
        }
        public GeneralFormatter() {
#if DEBUG
            Format = Style.Simple2; // the preferred format - change for debugging if desired
#else
            Format = Style.Simple2;
#endif
            FormatExplicit = false;
        }

        public Style Format { get; set; }
        public bool FormatExplicit { get; set; }// whether the format was explicitly set by caller (used for serialization, deserialization still tries to guess)

        public TObj? Deserialize<TObj>(byte[] btes) {
            if (btes == null || btes.Length <= 6) return default;
            IYetaWFFormatter fmt;
            if (btes[0] == (char)239 && btes[1] == (char)187)
                fmt = new TextFormatter();
            else if (btes[0] == Simple2Formatter.MARKER1 && btes[1] == Simple2Formatter.MARKER2) {
                Simple2Formatter simpleFmt = new Simple2Formatter();
                try {
                    object? data = simpleFmt.Deserialize(btes);
                    if (data != null) {
                        Type dataType = data.GetType();
                        while (true) {
                            if (dataType == typeof(TObj))
                                break;
                            if (dataType.BaseType == null)
                                return default(TObj); // type mismatch
                            dataType = dataType.BaseType;
                        }
                    }
                    return (TObj?)data;
                } catch (Exception exc) {
                    throw new InternalError("{0} - A common cause for this error is a change in the internal format of the object", ErrorHandling.FormatExceptionMessage(exc));
                }
            } else if (btes[2] == 0 && btes[3] == 0 && btes[4] == 'O' && btes[5] == 'b') {// looking for object
                fmt = new SimpleFormatter();
            } else if (btes[0] == '{') {
                return new JSONFormatter().Deserialize<TObj>(btes);
            } else
                throw new InternalError("An unknown format was encountered and cannot be deserialized");
            using (MemoryStream ms = new MemoryStream(btes)) {
                object? data;
                try {
                    data = fmt.Deserialize(ms);
                } catch (Exception exc) {
                    throw new InternalError("{0} - A common cause for this error is a change in the internal format of the object", ErrorHandling.FormatExceptionMessage(exc));
                }
                return (TObj?)data;
            }
        }
        /// <summary>
        /// Deserialize from FileStream
        /// </summary>
        /// <remarks>Does NOT guess the format</remarks>
        public TObj Deserialize<TObj>(FileStream fs) where TObj: notnull {
            if (!FormatExplicit)
                throw new InternalError("The format for this stream was not explicitly specified");

            object? data;

            switch (Format) {
                case Style.Xml:
                    try {
                        data = new TextFormatter().Deserialize(fs);
                    } catch (Exception exc) {
                        throw new InternalError($"{ErrorHandling.FormatExceptionMessage(exc)} - A common cause for this error is a change in the internal format of the object");
                    }
                    break;
                case Style.Simple:
                    try {
                        data = new SimpleFormatter().Deserialize(fs);
                    } catch (Exception exc) {
                        throw new InternalError($"{ErrorHandling.FormatExceptionMessage(exc)} - A common cause for this error is a change in the internal format of the object");
                    }
                    break;
                case Style.Simple2:
                    throw new InternalError("This format is not supported for file streams");
                case Style.JSON:
                    try {
                        data = new JSONFormatter().Deserialize<TObj>(fs);
                    } catch (Exception exc) {
                        throw new InternalError($"{ErrorHandling.FormatExceptionMessage(exc)} - A common cause for this error is a change in the internal format of the object");
                    }
                    break;
                case Style.JSONTyped:
                    try {
                        data = new JSONFormatter().Deserialize<TObj>(fs, true);
                    } catch (Exception exc) {
                        throw new InternalError($"{ErrorHandling.FormatExceptionMessage(exc)} - A common cause for this error is a change in the internal format of the object");
                    }
                    break;
                default:
                    throw new InternalError($"Invalid format {Format}");
            }
            if (data == null)
                throw new InternalError($"null data received in {nameof(Deserialize)}");

            return (TObj)data;
        }

        public void Serialize<TObj>(FileStream fs, TObj obj) {
            switch (Format) {
                case Style.Xml:
                    new TextFormatter().Serialize(fs, obj);
                    break;
                case Style.Simple:
                    new SimpleFormatter().Serialize(fs, obj);
                    break;
                case Style.Simple2:
                    throw new InternalError("This format is not supported for file streams");
                case Style.JSON:
                    new JSONFormatter().Serialize(fs, obj, false);
                    return;
                case Style.JSONTyped:
                    new JSONFormatter().Serialize(fs, obj, true);
                    return;
                default:
                    throw new InternalError($"Invalid format {Format}");
            }
        }

        public byte[] Serialize<TObj>(TObj? obj) {
            if (obj == null) return new byte[] { };
            IYetaWFFormatter fmt;

            switch (Format) {
                case Style.Xml:
                    fmt = new TextFormatter();
                    break;
                case Style.Simple:
                    fmt = new SimpleFormatter();
                    break;
                case Style.Simple2:
                    Simple2Formatter simpleFmt = new Simple2Formatter();
                    return simpleFmt.Serialize(obj);
                case Style.JSON:
                    return new JSONFormatter().Serialize(obj, false);
                case Style.JSONTyped:
                    return new JSONFormatter().Serialize(obj, true);
                default:
                    throw new InternalError($"Invalid format {Format}");
            }
            using (MemoryStream ms = new MemoryStream()) {
                fmt.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        // Remove some type information that is not needed and normalize it to MVC6
        internal static string UpdateTypeForSerialization(string typeName) {
            int index;
            for ( ; (index = typeName.IndexOf(", Version=", StringComparison.Ordinal)) >= 0 ; ) {
                typeName = typeName.RemoveUpTo(index, index + ", Version=".Length, EndChars);
            }
            for ( ; (index = typeName.IndexOf(", Culture=", StringComparison.Ordinal)) >= 0 ; ) {
                typeName = typeName.RemoveUpTo(index, index + ", Culture=".Length, EndChars);
            }
            for ( ; (index = typeName.IndexOf(", PublicKeyToken=", StringComparison.Ordinal)) >= 0 ; ) {
                typeName = typeName.RemoveUpTo(index, index + ", PublicKeyToken=".Length, EndChars);
            }
            typeName = typeName.Replace(", mscorlib", ", System.Private.CoreLib"); // (MVC5) used for system.string, replace with MVC6 equivalent (standard is MVC6)
            return typeName;
        }
        static List<char> EndChars = new List<char> { ',', ']' };

        internal static void UpdateTypeForDeserialization(ref string typeName, ref string assembly) {
            // Types that were changed in 4.0
            if (typeName == "YetaWF.Core.Menus.MenuList")
                typeName = "YetaWF.Core.Components.MenuList";
            else if (typeName == "YetaWF.Core.Views.Shared.RecaptchaV2Config")
                typeName = "YetaWF.Core.Components.RecaptchaV2Config";
            else {
                string tp = typeName;
                string asm = assembly;
                TypeConversion? conv = (from t in TypeConversions where tp == t.OldType && asm == t.OldAssembly select t).FirstOrDefault();
                if (conv != null) {
                    typeName = conv.NewType;
                    assembly = conv.NewAssembly;
                }
            }
        }

        public static void RegisterTypeConversion(string oldType, string oldAssembly, string newType, string newAssembly) {
            TypeConversions.Add(new TypeConversion {
                OldType = oldType, OldAssembly = oldAssembly,
                NewType = newType, NewAssembly = newAssembly,
            });
        }
        private static List<TypeConversion> TypeConversions = new List<TypeConversion>();

        public class TypeConversion {
            public string OldType { get; set; } = null!;
            public string OldAssembly { get; set; } = null!;
            public string NewType { get; set; } = null!;
            public string NewAssembly { get; set; } = null!;
        }
    }
}