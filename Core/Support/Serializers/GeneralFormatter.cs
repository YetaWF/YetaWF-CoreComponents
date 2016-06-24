/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using YetaWF.Core.Localize;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Support.Serializers {
    /// <summary>
    /// General serializer/deserializer
    /// </summary>
    /// <remarks>It determines the format when deserializing and calls the appropriate deserializer (simple, text, binary).
    /// When serializing it uses the globally defined format. For debugging it's best to use "text", and in production "simple". "binary" is not supported.
    /// This means there can be a mix of formats in production which will be unified when saving data and when data is restored.</remarks>
    public class GeneralFormatter {

        public enum Style {
            Xml = 0,
            Simple = 1,
            Binary = 2,
        };

        public GeneralFormatter(Style format) {
            Format = format;
            FormatExplicit = true;
        }
        public GeneralFormatter() {
#if DEBUG
            Format = Style.Simple; // the preferred format - change for debugging if desired
#else
            Format = Style.Simple;
#endif
            FormatExplicit = false;
        }

        public Style Format { get; set; }
        public bool FormatExplicit { get; set; }// whether the format was explicitly set by caller (used for serialization, deserialization still tries to guess)

        public object Deserialize(byte[] btes) {
            if (btes == null || btes.Length <= 6) return null;
            IFormatter fmt;
            if (btes[0] == (char)239 && btes[1] == (char)187)
                fmt = new TextFormatter();
            else if (btes[2] == 0 && btes[3] == 0 && btes[4] == 'O' && btes[5] == 'b') // looking for object
                fmt = new SimpleFormatter();
            else
                fmt = new BinaryFormatter();// truly binary
            using (MemoryStream ms = new MemoryStream(btes)) {
                object data;
                try {
                    data = fmt.Deserialize(ms);
                } catch (Exception exc) {
                    throw new Error(this.__ResStr("errFormatChange", "{0} - A common cause for this error is a change in the internal format of the object."), exc.Message);
                }
                return data;
            }
        }
        /// <summary>
        /// Deserialize from FileStream
        /// </summary>
        /// <remarks>Does NOT guess the format</remarks>
        public object Deserialize(FileStream fs) {
            if (!FormatExplicit)
                throw new InternalError("The format for this stream was not explicitly specified");
            IFormatter fmt;
            if (Format == Style.Simple)
                fmt = new SimpleFormatter();
            else if (Format == Style.Xml)
                fmt = new TextFormatter();
            else
                fmt = new BinaryFormatter();// truly binary
            object data;
            try {
                data = fmt.Deserialize(fs);
            } catch (Exception exc) {
                throw new Error(this.__ResStr("errFormatChange", "{0} - A common cause for this error is a change in the internal format of the object."), exc.Message);
            }
            return data;
        }

        public void Serialize(FileStream fs, object obj) {
            IFormatter fmt = null;
            if (Format == Style.Xml) {
                fmt = new TextFormatter();
            } else if (Format == Style.Simple) {
                fmt = new SimpleFormatter();
            } else {
                fmt = new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };
            }
            fmt.Serialize(fs, obj);
        }
        public byte[] Serialize(object obj) {
            IFormatter fmt = null;
            bool simple = false;
            if (Format == Style.Xml) {
                fmt = new TextFormatter();
            } else if (Format == Style.Simple) {
                fmt = new SimpleFormatter();
                simple = true;
            } else {
                fmt = new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };
            }
            using (MemoryStream ms = new MemoryStream()) {
                fmt.Serialize(ms, obj);
                byte[] btes = ms.ToArray();
                if (simple) {
                    if (!(btes[2] == 0 && btes[3] == 0 && btes[4] == 'O' && btes[5] == 'b'))
                        throw new InternalError("SimpleFormatter preamble unexptected");
                }
                return btes;
            }
        }
    }
}