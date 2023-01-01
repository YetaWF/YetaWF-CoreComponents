/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;

namespace YetaWF.Core.Extensions {

    public static partial class Extensions {

        /// <summary>
        /// Removes the last comma from the buffer. The comma may be the last character or followed by \r\n.
        /// </summary>
        /// <param name="sb">The StringBuilder instance.</param>
        public static void RemoveLastComma(this StringBuilder sb) {

            int len = sb.Length;
            if (len > 0) {
                if (sb[len - 1] == ',') {
                    sb.Remove(len - 1, 1);
                    return;
                }
            }
            if (len > 2) {
                if (sb[len - 3] == ',' && sb[len - 2] == '\r' && sb[len - 1] == '\n') {
                    sb.Remove(len - 3, 3);
                    return;
                }
            }
        }
    }

}
