using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Models;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Audit {

    public interface IAudit {
        Task AddAsync(AuditInfo info);
        Task RemoveAllAsync();
        Task<bool> HasItemsAsync();
    }

    public class AuditInfo {

        public string IdentifyString { get; set; }
        public Guid IdentifyGuid { get; set; }
        public int SiteIdentity { get; set; }
        public int UserId { get; set; }

        public string Action { get; set; }
        public string Description { get; set; }
        public bool RequiresRestart { get; set; }
        public bool ExpensiveMultiInstance { get; set; }

        public string Changes { get; set; }
        public byte[] DataBefore { get; set; }
        public byte[] DataAfter { get; set; }
    }

    public static class Auditing {

        // Dataprovider set by available data providers during application startup
        public static IAudit AuditProvider { get; set; }
        public static bool Active { get { return AuditProvider != null; } }

        public static async Task AddAuditAsync(string action, string idString, Guid idGuid, string description, int dummy = 0,
            bool RequiresRestart = false, bool ExpensiveMultiInstance = false,
            object DataBefore = null, object DataAfter = null) {

            string changes = null;
            if (DataBefore != null && DataAfter != null) {
                List<ObjectSupport.ChangedProperty> list = ObjectSupport.ModelChanges(DataBefore, DataAfter);                    
                changes = string.Join(",", (from l in list select $"{l.Name}={l.Value}"));
            }

            if (AuditProvider == null) return;

            int siteIdentity = 0, userId = 0;
            if (YetaWFManager.HaveManager) {
                YetaWFManager manager = YetaWFManager.Manager;
                siteIdentity = manager.CurrentSite != null ? manager.CurrentSite.Identity : 0;
                userId = manager.UserId;
            }

            AuditInfo info = new AuditInfo {
                Action = action,
                Changes = changes,
                Description = description,
                DataAfter = DataAfter != null ? new GeneralFormatter().Serialize(DataAfter) : null,
                DataBefore = DataBefore != null ? new GeneralFormatter().Serialize(DataBefore) : null,
                IdentifyString = idString,
                IdentifyGuid = idGuid,
                ExpensiveMultiInstance = ExpensiveMultiInstance,
                RequiresRestart = RequiresRestart,
                SiteIdentity = siteIdentity,
                UserId = userId,
            };
            await AuditProvider.AddAsync(info);
        }
    }
}
