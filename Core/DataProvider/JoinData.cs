/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.DataProvider {

    public class JoinData {

        public enum JoinTypeEnum {
            Inner = 0,
            Left = 1,
        };

        public DataProviderImpl MainDP { get; set; } = null!;
        public DataProviderImpl JoinDP { get; set; } = null!;
        public string MainColumn { get; set; } = null!;
        public string JoinColumn { get; set; } = null!;

        public bool UseSite{ get; set; }
        public JoinTypeEnum JoinType { get; set; }

        public JoinData() {
            UseSite = true;
            JoinType = JoinTypeEnum.Inner;
        }
    }
}
