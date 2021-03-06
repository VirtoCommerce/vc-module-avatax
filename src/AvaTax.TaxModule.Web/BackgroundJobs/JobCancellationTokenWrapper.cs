﻿using Hangfire;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    /// <summary>
    /// Wrapper for Hangfire's IJobCancellationToken
    /// </summary>
    public class JobCancellationTokenWrapper : ICancellationToken
    {
        public IJobCancellationToken JobCancellationToken { get; }

        public JobCancellationTokenWrapper(IJobCancellationToken jobCancellationToken)
        {
            JobCancellationToken = jobCancellationToken;
        }

        #region Implementation of ICancellationToken

        public void ThrowIfCancellationRequested()
        {
            JobCancellationToken.ThrowIfCancellationRequested();
        }

        #endregion
    }
}