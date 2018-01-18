/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.DataProvider {
    public class JoinData {

        public enum JoinTypeEnum {
            Inner = 0,
            Left = 1,
        };

        public DataProviderImpl MainDP { get; set; }
        public DataProviderImpl JoinDP { get; set; }
        public string MainColumn { get; set; }
        public string JoinColumn { get; set; }

        public bool UseSite{ get; set; }
        public JoinTypeEnum JoinType { get; set; }

        public JoinData() {
            UseSite = true;
            JoinType = JoinTypeEnum.Inner;
        }
    }
}
