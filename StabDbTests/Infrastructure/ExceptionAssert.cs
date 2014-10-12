using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StabDbTests.Infrastructure
{
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public static class ExceptionAssert
    {
        public static Exception Throws<T>(Action task) where T : Exception
        {
            try
            {
                task();
            }
            catch (Exception ex)
            {
                AssertExceptionType<T>(ex);
                return ex;
            }
            if (typeof (T) == new Exception().GetType())
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                    "Expected exception but no exception was thrown.");
            }
            else
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(
                    string.Format("Expected exception of type {0} but no exception was thrown.", (object) typeof (T)));
            }

            return null;
        }

        public static Exception Throws(Action task)
        {
            return Throws<Exception>(task);
        }

        private static void AssertExceptionType<T>(Exception ex)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType((object)ex, typeof(T), "Expected exception type failed.");
        }
    }
}
