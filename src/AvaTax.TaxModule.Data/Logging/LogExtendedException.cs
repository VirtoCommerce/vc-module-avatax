﻿using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AvaTax.TaxModule.Data.Logging
{
    public class LogExtendedException : Exception
    {
        private readonly Exception _exception;
        public LogExtendedException(Exception error)
            : base(error.Message, error)
        {
            _exception = error;
        }

        public static LogExtendedException Create(Exception ex)
        {
            return new LogExtendedException(ex);
        }

        public override string ToString()
        {
            if (_exception == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                try
                {
                    JsonSerializer.Create().Serialize(sw, _exception);
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        string.Format(
                            "Failed to serialize exception type {0}: {1}",
                            _exception.GetType().Name,
                            ex));
                    sb.AppendLine("------------- original exception -------------");
                    sb.AppendLine(_exception.ToString());
                }
            }

            return sb.ToString();
        }
    }
}