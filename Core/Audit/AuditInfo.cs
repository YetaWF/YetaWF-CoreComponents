/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Models;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Audit {

    /// <summary>
    /// This interface defines the core services for the AuditInfoDataProvider.
    /// </summary>
    public interface IAudit {
        /// <summary>
        /// Add an audit record to the audit log.
        /// </summary>
        /// <param name="info">Defines the information to be added to the audit log.</param>
        /// <returns>An AuditInfo object containing the information to record.</returns>
        Task AddAsync(AuditInfo info);
        /// <summary>
        /// Removes all audit log records.
        /// </summary>
        Task RemoveAllAsync();
        /// <summary>
        /// Returns whether a restart is pending to activate new settings.
        /// </summary>
        /// <returns>Returns whether a restart is pending to activate new settings.</returns>
        Task<bool> HasPendingRestartAsync();
    }

    /// <summary>
    /// This class defines one audit record describing the data stored for each audit record.
    /// Applications don't instantiate this class directly.
    /// Applications add audit records using the Auditing.AddAuditAsync method.
    /// </summary>
    public class AuditInfo {
        /// <summary>
        /// An application-specific string that describes the reason for this audit record.
        /// </summary>
        public string IdentifyString { get; set; }
        /// <summary>
        /// An application-specific Guid that describes the reason for this audit record. For example, operations on modules can save the module's Guid.
        /// </summary>
        public Guid IdentifyGuid { get; set; }
        /// <summary>
        /// The site identity that created this audit record. Can be 0 if no specific site is associated with this audit record.
        /// </summary>
        public int SiteIdentity { get; set; }
        /// <summary>
        /// The user ID that caused this audit record to be created. Can be 0 if no specific user is associated with this audit record.
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// An application-specific string that describes the reason for this audit record. Typically, the type and name of the method requesting the audit record to be written is used.
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// A description of the audit record summarizing the audit record, in user displayable format.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Defines whether the current action that caused the audit record to be created requires a restart. This can be set when modifying settings that don't take effect until after the site is restarted.
        /// </summary>
        public bool RequiresRestart { get; set; }
        /// <summary>
        /// Defines whether the current action that caused the audit record is considered an "expensive" operation. Typically, actions that
        /// affect shared cashing are considered expensive.
        /// </summary>
        public bool ExpensiveMultiInstance { get; set; }
        /// <summary>
        /// Lists the properties that were changed, analyzing DataBefore and DataAfter, if available.
        /// </summary>
        public string Changes { get; set; }
        /// <summary>
        /// The before image of a data object that was changed, which caused the current audit record to be created.
        /// May be null if the data object was added.
        /// </summary>
        public byte[] DataBefore { get; set; }
        /// <summary>
        /// The after image of a data object that was changed, which caused the current audit record to be created.
        /// May be null if the data object was removed.
        /// </summary>
        public byte[] DataAfter { get; set; }
    }

    /// <summary>
    /// This static class implements the public methods for applications to create audit records.
    /// Applications add audit records using the Auditing.AddAuditAsync method.
    /// </summary>
    public static class Auditing {

        /// <summary>
        /// Defines the data provider implementing the audit log.
        /// Applications do not use this data provider directly.
        /// The data provider is set by available data providers during application startup.
        /// </summary>
        public static IAudit AuditProvider { get; set; }
        /// <summary>
        /// Defines whether the audit log is available.
        /// </summary>
        public static bool Active { get { return AuditProvider != null; } }

        /// <summary>
        /// Add one record to the audit log.
        /// This method is used by applications to add audit records.
        /// </summary>
        /// <param name="action">An application-specific string that describes the reason for this audit record. Typically, the type and name of the method requesting the audit record to be written is used.</param>
        /// <param name="idString">An application-specific string that describes the reason for this audit record.</param>
        /// <param name="idGuid">An application-specific Guid that describes the reason for this audit record. For example, operations on modules can save the module's Guid.</param>
        /// <param name="description">A description of the audit record summarizing the audit record, in user displayable format.</param>
        /// <param name="dummy">Not used. Its purpose is to separate positional parameters from named parameters.</param>
        /// <param name="RequiresRestart">Defines whether the current action that caused the audit record to be created requires a restart. This can be set when modifying settings that don't take effect until after the site is restarted.</param>
        /// <param name="ExpensiveMultiInstance">Defines whether the current action that caused the audit record is considered an "expensive" operation. Typically, actions that affect shared cashing are considered expensive.</param>
        /// <param name="DataBefore">The before image of a data object that was changed, which caused the current audit record to be created. May be null if the data object is added.</param>
        /// <param name="DataAfter">The after image of a data object that was changed, which caused the current audit record to be created. May be null if the data object is removed.</param>
        /// <returns></returns>
        public static async Task AddAuditAsync(string action, string idString, Guid idGuid, string description, int dummy = 0,

            bool RequiresRestart = false, bool ExpensiveMultiInstance = false,
            object DataBefore = null, object DataAfter = null) {

            if (DataBefore != null && DataAfter != null) {
                if (!RequiresRestart) {
                    ObjectSupport.ModelDisposition modelDisp = ObjectSupport.EvaluateModelChanges(DataBefore, DataAfter);
                    switch (modelDisp) {
                        case ObjectSupport.ModelDisposition.SiteRestart:
                            RequiresRestart = true;
                            YetaWF.Core.Support.Startup.RestartPending = RequiresRestart;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (AuditProvider == null) return;

            string changes = null;
            if (DataBefore != null && DataAfter != null) {
                List<ObjectSupport.ChangedProperty> list = ObjectSupport.ModelChanges(DataBefore, DataAfter);
                changes = string.Join(",", (from l in list select $"{l.Name}={l.Value}"));
            }

            int siteIdentity = 0, userId = 0;
            if (YetaWFManager.HaveManager) {
                YetaWFManager manager = YetaWFManager.Manager;
                siteIdentity = YetaWFManager.Manager.HaveCurrentSite ? manager.CurrentSite.Identity : 0;
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
