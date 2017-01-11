/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
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
        bool IsAvailable();
        /// <summary>
        /// Returns a ModuleAction to complete login by showing a form to enter two-step authorization info.
        /// </summary>
        /// <param name="userId">The userId of the user logging in.</param>
        /// <param name="userName">The user name of the user logging in.</param>
        /// <param name="email">The email address of the user logging in.</param>
        /// <returns>A ModuleAction which shows a form to enter two-step authorization info.</returns>
        ModuleAction GetLoginAction(int userId, string userName, string email);
        /// <summary>
        /// Returns a ModuleAction to set up two-step authorization for the current user.
        /// </summary>
        /// <returns>A ModuleAction which shows a form to set up two-step authorization info.</returns>
        ModuleAction GetSetupAction();
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

        public List<ITwoStepAuth> GetTwoStepAuthProcessors() {
            return (from r in RegisteredProcessors where r.IsAvailable() select r).ToList();
        }
        public ITwoStepAuth GetTwoStepAuthProcessorByName(string name) {
            return (from p in GetTwoStepAuthProcessors() where p.IsAvailable() && p.Name == name select p).FirstOrDefault();
        }
        public ModuleAction GetLoginAction(List<string> enabledTwoStepAuthentications, int userId, string userName, string email) {
            List<string> procs = (from p in GetTwoStepAuthProcessors() where p.IsAvailable() select p.Name).ToList();
            procs = procs.Intersect(enabledTwoStepAuthentications).ToList();
            if (procs.Count == 0)
                return null;
            if (procs.Count > 1) {
                // show select desired two-step method
                return Resource.ResourceAccess.GetSelectTwoStepAction(userId, userName, email);
            } else {
                // call two-step method
                string procName = procs.First();
                ITwoStepAuth auth = GetTwoStepAuthProcessorByName(procs.First());
                if (auth == null)
                    throw new InternalError("TwoStepAuthorization provider {0} not found", procName);
                return auth.GetLoginAction(userId, userName, email);
            }
        }
        public bool VerifyTwoStepAutheticationDone(int userId) {
            List<ITwoStepAuth> procs = (from p in GetTwoStepAuthProcessors() where p.IsAvailable() select p).ToList();
            foreach (ITwoStepAuth auth in procs) {
                if (auth.CheckVerifiedUser(userId))
                    return true;
            }
            return false;
        }
        public bool ClearTwoStepAuthetication(int userId) {
            List<ITwoStepAuth> procs = (from p in GetTwoStepAuthProcessors() where p.IsAvailable() select p).ToList();
            foreach (ITwoStepAuth auth in procs)
                auth.ClearVerifiedUser(userId);
            return false;
        }
    }
}
