/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using FluffySpoon.AspNet.LetsEncrypt;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.LetsEncrypt {

	public class LetsEncryptLifecycleHook : ICertificateRenewalLifecycleHook {

		// when the renewal background job has started.
		public Task OnStartAsync() {
			Logging.AddLog($"{nameof(LetsEncryptLifecycleHook)}: Starting renewal process");
			return Task.CompletedTask;
		}

		//when the renewal background job (or the application) has stopped.
		//this is not guaranteed to fire in critical application crash scenarios.
		public Task OnStopAsync() {
			Logging.AddLog($"{nameof(LetsEncryptLifecycleHook)}: Stopping");
			return Task.CompletedTask;
		}

		//when the renewal has completed.
		public Task OnRenewalSucceededAsync() {
			Logging.AddLog($"{nameof(LetsEncryptLifecycleHook)}: Renewal succeeded");
			return Task.CompletedTask;
		}

		public Task OnExceptionAsync(Exception exc) {
			Logging.AddLog($"{nameof(LetsEncryptLifecycleHook)}: {ErrorHandling.FormatExceptionMessage(exc)}");
			return Task.CompletedTask;
		}
	}
}

#endif