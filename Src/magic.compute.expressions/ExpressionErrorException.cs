using System;
using System.Collections.Generic;
using System.Text;

namespace magic.compute.expressions
{
    public class ExpressionErrorException : Exception
    {
        public ExpressionErrorException(int index, string word) : base($"has error near by \"{word}\", at {index}")
        {
        }
    }
}
