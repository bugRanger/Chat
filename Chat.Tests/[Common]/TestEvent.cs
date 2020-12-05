namespace Chat.Tests
{
    using System;
    using System.Collections;
    using System.Linq;

    public partial class CoreApiTests
    {
        public class TestEvent : IEquatable<TestEvent>
        {
            #region Properties

            public object[] Params { get; }

            #endregion Properties

            #region Constructors

            public TestEvent(params object[] param) 
            {
                Params = param;
            }

            #endregion Constructors

            #region Methods

            public bool Equals(TestEvent other)
            {
                if (other?.Params?.Length == 0 || Params?.Length != other.Params.Length)
                {
                    return false;
                }

                for (int i = 0; i < Params.Length; i++)
                {
                    if (Params[i] is IEnumerable paramArray && other.Params[i] is IEnumerable otherArray)
                    {
                        if (!paramArray.Cast<object>().SequenceEqual(otherArray.Cast<object>()))
                        {
                            return false;
                        }
                    }
                    else if (!Params[i].Equals(other.Params[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            #endregion Methods
        }
    }
}
