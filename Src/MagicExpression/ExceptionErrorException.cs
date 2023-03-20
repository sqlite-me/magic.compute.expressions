using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression
{
    public class ExceptionErrorException : Exception
    {
        public ExceptionErrorException(int index, string word) : base($"has error near by \"{word}\", at {index}")
        {
        }
    }
}
