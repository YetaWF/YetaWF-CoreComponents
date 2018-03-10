/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;

namespace YetaWF.Core.Support.TwoStepAuthorization {

    /// <summary>
    /// Defines all features an TwoStepAuthorization provider provides to support two-step authorization.
    /// </summary>
    public interface ITwoStepAuth {
        /// <summary>
        /// The name of the TwoStepAuthorization provider.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Returns whether the TwoStepAuthorization provider is available.
        /// </summary>
        /// <returns>True if it is available, false otherwise.</returns>
        Task<bool> IsAvailableAsync();
        /// <summary>
        /// Returns a ModuleAction to complete login by showing a form to enter two-step authorization info.
        /// </summary>
        /// <param name="userId">The userId of the user logging in.</param>
        /// <param name="userName">The user name of the user logging in.</param>
        /// <param name="email">The email address of the user logging in.</param>
        /// <returns>A ModuleAction which shows a form to enter two-step authorization info.</returns>
        Task<ModuleAction> GetLoginActionAsync(int userId, string userName, string email);
        /// <summary>
        /// Returns a ModuleAction to set up two-step authorization for the current user.
        /// </summary>
        /// <returns>A ModuleAction which shows a form to set up two-step authorization info.</returns>
        Task<ModuleAction> GetSetupActionAsync();
        /// <summary>
        /// Verifies that the specified user has just been verified by the TwoStepAuthorization provider.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True if verified, false otherwise</returns>
        bool CheckVerifiedUser(int userId);
        /// <summary>
        /// Removes info about the specified user from the TwoStepAuthorization provider.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True if verified, false otherwise</returns>
        void ClearVerifiedUser(int userId);
        /// <summary>
        /// Returns a summary description of the TwoStepAuthorization provider.
        /// </summary>
        /// <returns>Summary description.</returns>
        string GetDescription();
    }

    public class TwoStepAuth {

        private static List<ITwoStepAuth> RegisteredProcessors = new List<ITwoStepAuth>();

        /// <summary>
        /// Registers a TwoStepAuthorization provider.
        /// </summary>
        public static void Register(ITwoStepAuth iproc) {
            Logging.AddLog("Registering TwoStepAuthorization provider named {0}", iproc.Name);
            RegisteredProcessors.Add(iproc);
        }

        public async Task<List<ITwoStepAuth>> GetTwoStepAuthProcessorsAsync() {
            List<ITwoStepAuth> list = new List<ITwoStepAuth>();
            foreach (ITwoStepAuth r in RegisteredProcessors) {
                if (await r.IsAvailableAsync())
                    list.Add(r);
            }
            return list;
        }
        public async Task<ITwoStepAuth> GetTwoStepAuthProcessorByNameAsync(string name) {
            List<ITwoStepAuth> list = await GetTwoStepAuthProcessorsAsync();
            foreach (ITwoStepAuth r in RegisteredProcessors) {
                if (await r.IsAvailableAsync() && r.Name == name)
                    return r;
            }
            return null;
        }
        public async Task<ModuleAction> GetLoginActionAsync(List<string> enabledTwoStepAuthentications, int userId, string userName, string email) {
            List<ITwoStepAuth> list = await GetTwoStepAuthProcessorsAsync();
            List<string> procs = (from p in list select p.Name).ToList();
            procs = procs.Intersect(enabledTwoStepAuthentications).ToList();
            if (procs.Count == 0)
                return null;
            if (procs.Count > 1) {
                // show select desired two-step method
                return Resource.ResourceAccess.GetSelectTwoStepAction(userId, userName, email);
            } else {
                // call two-step method
                string procName = procs.First();
                ITwoStepAuth auth = await GetTwoStepAuthProcessorByNameAsync(procs.First());
                if (auth == null)
                    throw new InternalError("TwoStepAuthorization provider {0} not found", procName);
                return await auth.GetLoginActionAsync(userId, userName, email);
            }
        }
        public async Task<bool> VerifyTwoStepAutheticationDoneAsync(int userId) {
            List<ITwoStepAuth> procs = await GetTwoStepAuthProcessorsAsync();
            foreach (ITwoStepAuth auth in procs) {
                if (auth.CheckVerifiedUser(userId))
                    return true;
            }
            return false;
        }
        public async Task<bool> ClearTwoStepAutheticationAsync(int userId) {
            List<ITwoStepAuth> procs = await GetTwoStepAuthProcessorsAsync();
            foreach (ITwoStepAuth auth in procs)
                auth.ClearVerifiedUser(userId);
            return false;
        }
    }
}
