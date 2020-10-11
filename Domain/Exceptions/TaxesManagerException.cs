using System;

namespace Domain.Exceptions
{
    public class TaxesManagerException : Exception
    {
        public TaxesManagerException()
        { }

        public TaxesManagerException(string message)
            : base(message)
        { }

        public TaxesManagerException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
